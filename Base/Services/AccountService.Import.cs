using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.IO.Compression;
using Base.Entities;
using Base.Exceptions;
using Base.Models.Requests;
using Base.Models.Responses;
using ClosedXML.Excel;

namespace Base.Services;

public partial class AccountService
{
    public const long MaxImportFileSize = 5 * 1024 * 1024;
    public static readonly string[] ImportHeaders =
        ["Username", "Password", "FullName", "Email", "PhoneNumber", "DateOfBirth", "Gender", "Address"];

    public static byte[] CreateImportTemplate()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Accounts");
        for (var i = 0; i < ImportHeaders.Length; i++)
        {
            sheet.Cell(1, i + 1).Value = ImportHeaders[i];
            sheet.Column(i + 1).Style.NumberFormat.Format = "@";
            sheet.Column(i + 1).Width = 24;
        }
        sheet.Row(1).Style.Font.Bold = true;
        sheet.SheetView.FreezeRows(1);
        using var output = new MemoryStream();
        workbook.SaveAs(output);
        return output.ToArray();
    }

    public static string? NormalizeImportPhone(string? phone) =>
        string.IsNullOrWhiteSpace(phone) ? null : phone.Trim().Replace(" ", "").Replace("-", "")
            .Replace(".", "").Replace("(", "").Replace(")", "").Replace("+", "");

    public static byte[] CreateDuplicateReport(IReadOnlyList<AccountImportDuplicate> duplicates)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Duplicates");
        string[] headers = ["Row", "Username", "FullName", "Email", "PhoneNumber", "Reason"];
        for (var col = 0; col < headers.Length; col++)
        {
            sheet.Cell(1, col + 1).Value = headers[col];
            sheet.Column(col + 1).Width = col == 5 ? 65 : 24;
            sheet.Column(col + 1).Style.NumberFormat.Format = "@";
        }
        sheet.Row(1).Style.Font.Bold = true;
        for (var i = 0; i < duplicates.Count; i++)
        {
            var item = duplicates[i];
            var row = i + 2;
            sheet.Cell(row, 1).Value = item.Row;
            sheet.Cell(row, 2).Value = item.Username;
            sheet.Cell(row, 3).Value = item.FullName;
            sheet.Cell(row, 4).Value = item.Email;
            sheet.Cell(row, 5).Value = item.PhoneNumber ?? "";
            sheet.Cell(row, 6).Value = item.Reason;
        }
        sheet.SheetView.FreezeRows(1);
        using var output = new MemoryStream();
        workbook.SaveAs(output);
        return output.ToArray();
    }

    public async Task<AccountImportResponse> ImportAsync(IFormFile file, CancellationToken cancellationToken)
    {
        if (file.Length == 0 || file.Length > MaxImportFileSize ||
            !string.Equals(Path.GetExtension(file.FileName), ".xlsx", StringComparison.OrdinalIgnoreCase))
            throw new ApiException(400, "Vui lòng chọn file .xlsx có dung lượng từ 1 byte đến 5 MB.");

        using var buffer = new MemoryStream();
        await file.CopyToAsync(buffer, cancellationToken);
        XLWorkbook workbook;
        try
        {
            buffer.Position = 0;
            using (var archive = new ZipArchive(buffer, ZipArchiveMode.Read, leaveOpen: true))
            {
                long expandedSize = 0;
                foreach (var entry in archive.Entries)
                {
                    expandedSize += entry.Length;
                    if (expandedSize > 50 * 1024 * 1024)
                        throw new ApiException(400, "Nội dung Excel giải nén vượt quá 50 MB.");
                }
            }
            buffer.Position = 0;
            workbook = new XLWorkbook(buffer);
        }
        catch (ApiException) { throw; }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new ApiException(400, "File Excel không hợp lệ hoặc không đọc được.");
        }

        using (workbook)
        {
            var sheet = workbook.Worksheets.FirstOrDefault()
                ?? throw new ApiException(400, "File Excel phải có ít nhất một sheet.");
            var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 0;
            if (lastRow < 2)
                throw new ApiException(400, "File Excel chưa có dữ liệu tài khoản.");
            if (lastRow > 1001)
                throw new ApiException(400, "Mỗi lần nhập tối đa 1.000 dòng.");
            var headers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var cell in sheet.Row(1).CellsUsed())
            {
                var name = cell.GetString().Trim();
                if (!headers.TryAdd(name, cell.Address.ColumnNumber))
                    throw new ApiException(400, $"Cột bị trùng: {name}.");
            }
            foreach (var name in ImportHeaders.Take(4))
                if (!headers.ContainsKey(name))
                    throw new ApiException(400, $"Thiếu cột bắt buộc: {name}.");

            var result = new AccountImportResponse();
            var accounts = new List<Account>();
            var usernames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var emails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var phones = new HashSet<string>(StringComparer.Ordinal);
            for (var row = 2; row <= lastRow; row++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var excelRow = sheet.Row(row);
                if (excelRow.IsEmpty()) continue;
                result.TotalRows++;
                string Value(string name) => headers.TryGetValue(name, out var col)
                    ? excelRow.Cell(col).GetString().Trim() : "";
                string? Optional(string name) => string.IsNullOrWhiteSpace(Value(name)) ? null : Value(name);
                if (excelRow.CellsUsed().Any(cell => cell.HasFormula))
                {
                    result.Errors.Add(new(row, "Dòng nhập không được chứa công thức Excel."));
                    continue;
                }

                DateOnly? birth = null;
                var date = Value("DateOfBirth");
                if (date.Length > 0)
                {
                    var cell = excelRow.Cell(headers["DateOfBirth"]);
                    if (cell.DataType == XLDataType.DateTime)
                        birth = DateOnly.FromDateTime(cell.GetDateTime());
                    else if (DateOnly.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                        DateTimeStyles.None, out var parsed)) birth = parsed;
                    else
                    {
                        result.Errors.Add(new(row, "DateOfBirth phải là ngày Excel hoặc yyyy-MM-dd."));
                        continue;
                    }
                }
                var request = new RegisterRequest
                {
                    Username = Value("Username").ToLowerInvariant(),
                    Password = excelRow.Cell(headers["Password"]).GetString(),
                    FullName = Value("FullName"),
                    Email = Value("Email").ToLowerInvariant(),
                    PhoneNumber = Optional("PhoneNumber"),
                    DateOfBirth = birth,
                    Gender = Optional("Gender"),
                    Address = Optional("Address")
                };
                var validation = new List<ValidationResult>();
                if (!Validator.TryValidateObject(request, new ValidationContext(request), validation, true))
                {
                    result.Errors.Add(new(row, string.Join(" ", validation.Select(error => error.ErrorMessage))));
                    continue;
                }
                var phone = NormalizeImportPhone(request.PhoneNumber);
                var duplicateInFile = usernames.Contains(request.Username) || emails.Contains(request.Email) ||
                    (phone != null && phones.Contains(phone));
                var duplicateInDatabase = await repository.ExistsForImportAsync(
                    request.Username, request.Email, phone, cancellationToken);
                if (duplicateInFile || duplicateInDatabase)
                {
                    result.Duplicates.Add(new(row, request.Username, request.FullName, request.Email,
                        request.PhoneNumber, duplicateInDatabase
                            ? "Tài khoản đã có trong database (trùng username, số điện thoại hoặc email); không thêm."
                            : "Tài khoản trùng dòng hợp lệ trước đó trong file (username, số điện thoại hoặc email); không thêm."));
                    continue;
                }
                var account = new Account { Username = request.Username };
                ApplyProfile(account, request);
                account.PasswordHash = passwordHasher.HashPassword(account, request.Password);
                accounts.Add(account);
                usernames.Add(request.Username);
                emails.Add(request.Email);
                if (phone != null) phones.Add(phone);
            }

            foreach (var account in accounts)
                await repository.AddAsync(account, cancellationToken);
            if (accounts.Count > 0)
                await repository.SaveAsync(cancellationToken);
            result.ImportedCount = accounts.Count;
            audit.Begin("Import", "User", Guid.Empty);
            audit.Complete(new { result.TotalRows, result.ImportedCount, result.DuplicateCount, result.InvalidCount });
            return result;
        }
    }
}
