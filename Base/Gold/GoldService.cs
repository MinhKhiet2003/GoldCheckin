using Base.Common;
using Base.Entities;
using Base.Exceptions;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Globalization;
using System.IO.Compression;
using System.Text.Json;
using ClosedXML.Excel;

namespace Base.Gold;

[DebuggerNonUserCode]
public class GoldService(IGoldRepository repo, IFaceVerifier face, AuditContext audit, TimeProvider clock)
{
    private DateTime UtcNow => clock.GetUtcNow().UtcDateTime;
    private DateOnly Today => DateOnly.FromDateTime(GoldRules.LocalNow(UtcNow));
    private static ApiException Error(string message, int code = 422) => new(code, message);
    public async Task<Employee> Actor(Guid id, bool admin, CancellationToken ct)
    {
        var e = await repo.Query<Employee>().SingleOrDefaultAsync(x => x.Id == id, ct);
        if (e is null && !admin) throw Error("Tài khoản chưa được gắn hồ sơ nhân viên. Liên hệ HCNS.", 403);
        return e ?? new Employee { Id = id, GoldRole = "hr" };
    }
    public async Task<Employee> RequireHr(Guid id, bool admin, CancellationToken ct)
    {
        var e = await Actor(id, admin, ct);
        if (!admin && e.GoldRole != "hr") throw Error("Chỉ HCNS hoặc quản trị viên được thực hiện.", 403);
        return e;
    }
    private IQueryable<Employee> Scope(Employee actor, bool admin)
    {
        var q = repo.Query<Employee>();
        return admin || actor.GoldRole == "hr" ? q : actor.GoldRole == "manager"
            ? q.Where(e => e.DepartmentId == actor.DepartmentId) : q.Where(e => e.Id == actor.Id);
    }
    public async Task<object> Bootstrap(Guid id, bool admin, CancellationToken ct)
    {
        var actor = await Actor(id, admin, ct);
        var employees = await (from e in Scope(actor, admin)
                               join a in repo.Query<Account>() on e.Id equals a.Id
                               join d in repo.Query<Department>() on e.DepartmentId equals d.Id
                               select new EmployeeResponse(e.Id, a.FullName, e.Code, e.DepartmentId, d.Name, e.Position, e.GoldRole, e.AnnualLeaveBalance, a.DateOfBirth, a.IsActive, e.EmploymentStartDate, a.AvatarUrl != null, a.AvatarUrl, e.FaceEnrollmentId != null && e.FaceEnrollmentId != "", e.FaceRegisteredAt, a.Username)).ToListAsync(ct);
        var name = await repo.Query<Account>().Where(a => a.Id == id).Select(a => a.FullName).SingleAsync(ct);
        return new
        {
            me = new { id, name, role = admin ? "admin" : actor.GoldRole, actor.DepartmentId, actor.AnnualLeaveBalance },
            employees,
            birthdays = await (from e in repo.Query<Employee>()
                               join a in repo.Query<Account>() on e.Id equals a.Id
                               where a.IsActive && a.DateOfBirth != null
                               select new { e.Id, name = a.FullName, dateOfBirth = a.DateOfBirth }).ToListAsync(ct),
            colleagues = await (from e in repo.Query<Employee>().Where(e => e.DepartmentId == actor.DepartmentId)
                                join a in repo.Query<Account>() on e.Id equals a.Id
                                where a.IsActive
                                select new { e.Id, name = a.FullName, e.Position }).ToListAsync(ct),
            departments = await (admin || actor.GoldRole == "hr" ? repo.Query<Department>() : repo.Query<Department>().Where(d => d.Id == actor.DepartmentId)).OrderBy(d => d.Id).ToListAsync(ct),
            locations = await repo.Query<WorkLocation>().ToListAsync(ct),
            shifts = await repo.Query<WorkShift>().ToListAsync(ct),
            policy = await repo.Query<AttendancePolicy>().SingleAsync(ct),
            staffingRules = await repo.Query<StaffingRule>().ToListAsync(ct),
            peakDays = await repo.Query<PeakDay>().ToListAsync(ct),
            today = Today,
            scheduleLocks = await repo.Query<ScheduleLock>().Select(l => l.Id).ToListAsync(ct),
            canRegisterFace = admin,
            faceReady = !string.IsNullOrWhiteSpace(actor.FaceEnrollmentId)
        };
    }
    public async Task<Guid?> GetEmployeeAvatarFileId(Guid actorId, bool admin, Guid employeeId, CancellationToken ct)
    {
        var actor = await Actor(actorId, admin, ct);
        if (!await Scope(actor, admin).AnyAsync(e => e.Id == employeeId, ct))
            throw Error("Bạn không có quyền xem ảnh đại diện nhân viên này.", 403);

        var avatarUrl = await repo.Query<Account>().Where(a => a.Id == employeeId)
            .Select(a => a.AvatarUrl).SingleOrDefaultAsync(ct);
        var fileIdText = avatarUrl?.Split('/', StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
        return Guid.TryParse(fileIdText, out var fileId) ? fileId : null;
    }
    public static readonly string[] EmployeeImportHeaders =
        ["Username", "Password", "FullName", "Email", "PhoneNumber", "DateOfBirth", "Gender", "Address", "Code", "Position", "Department", "GoldRole", "AnnualLeaveBalance", "EmploymentStartDate", "IsActive"];

    public static byte[] CreateEmployeeImportTemplate()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("NhanVien");
        for (var i = 0; i < EmployeeImportHeaders.Length; i++)
        {
            sheet.Cell(1, i + 1).Value = EmployeeImportHeaders[i];
            sheet.Column(i + 1).Width = 22;
            sheet.Column(i + 1).Style.NumberFormat.Format = "@";
        }
        sheet.Row(1).Style.Font.Bold = true;
        sheet.SheetView.FreezeRows(1);
        sheet.Cell(2, 1).Value = "nguyenvana"; sheet.Cell(2, 2).Value = "MatKhauDauTien123";
        sheet.Cell(2, 3).Value = "Nguyễn Văn A"; sheet.Cell(2, 4).Value = "a@example.com";
        sheet.Cell(2, 5).Value = "0912345678"; sheet.Cell(2, 6).Value = "1995-05-20";
        sheet.Cell(2, 7).Value = "Male"; sheet.Cell(2, 8).Value = "Địa chỉ";
        sheet.Cell(2, 9).Value = "NV001"; sheet.Cell(2, 10).Value = "Nhân viên";
        sheet.Cell(2, 11).Value = "Tên phòng ban chính xác"; sheet.Cell(2, 12).Value = "employee";
        sheet.Cell(2, 13).Value = "12"; sheet.Cell(2, 14).Value = "2026-01-01";
        sheet.Cell(2, 15).Value = "true";
        sheet.Row(2).Style.Font.FontColor = XLColor.Gray;
        using var output = new MemoryStream(); workbook.SaveAs(output); return output.ToArray();
    }

    public async Task<byte[]> ExportEmployees(Guid actorId, bool admin, CancellationToken ct)
    {
        var actor = await RequireHr(actorId, admin, ct);
        var rows = await (from e in Scope(actor, admin)
                          join a in repo.Query<Account>() on e.Id equals a.Id
                          join d in repo.Query<Department>() on e.DepartmentId equals d.Id
                          orderby d.Name, a.FullName
                          select new { a.Username, a.FullName, a.Email, a.PhoneNumber, a.DateOfBirth, a.Gender, a.Address,
                              e.Code, e.Position, Department = d.Name, e.GoldRole, e.AnnualLeaveBalance,
                              e.EmploymentStartDate, a.IsActive }).ToListAsync(ct);
        using var workbook = new XLWorkbook(); var sheet = workbook.AddWorksheet("Danh sách nhân viên");
        string[] headers = ["Tên đăng nhập", "Họ và tên", "Thư điện tử", "Số điện thoại", "Ngày sinh", "Giới tính", "Địa chỉ", "Mã nhân viên", "Chức danh", "Phòng ban", "Vai trò", "Phép năm còn lại (ngày)", "Ngày bắt đầu làm việc", "Trạng thái tài khoản"];
        for (var col = 0; col < headers.Length; col++) { sheet.Cell(1, col + 1).Value = headers[col]; sheet.Column(col + 1).Width = col switch { 0 or 2 => 25, 1 or 8 or 9 => 28, 3 or 7 => 18, 5 or 10 or 13 => 21, 11 => 25, _ => 22 }; }
        for (var i = 0; i < rows.Count; i++)
        {
            var row = rows[i]; var n = i + 2;
            sheet.Cell(n, 1).Value = row.Username; sheet.Cell(n, 2).Value = row.FullName; sheet.Cell(n, 3).Value = row.Email;
            sheet.Cell(n, 4).Value = row.PhoneNumber ?? "";
            if (row.DateOfBirth is { } birth) sheet.Cell(n, 5).Value = birth.ToDateTime(TimeOnly.MinValue);
            sheet.Cell(n, 6).Value = row.Gender switch { "Male" => "Nam", "Female" => "Nữ", "Other" => "Khác", "PreferNotToSay" => "Không muốn tiết lộ", null or "" => "", _ => "Chưa xác định" };
            sheet.Cell(n, 7).Value = row.Address ?? ""; sheet.Cell(n, 8).Value = row.Code;
            sheet.Cell(n, 9).Value = row.Position; sheet.Cell(n, 10).Value = row.Department;
            sheet.Cell(n, 11).Value = row.GoldRole switch { "employee" => "Nhân viên", "manager" => "Trưởng phòng", "hr" => "Hành chính nhân sự", _ => "Quản trị viên" };
            sheet.Cell(n, 12).Value = row.AnnualLeaveBalance;
            if (row.EmploymentStartDate is { } startDate) sheet.Cell(n, 13).Value = startDate.ToDateTime(TimeOnly.MinValue);
            sheet.Cell(n, 14).Value = row.IsActive ? "Đang hoạt động" : "Đã khóa";
        }
        sheet.Row(1).Style.Font.Bold = true; sheet.Row(1).Style.Fill.BackgroundColor = XLColor.FromHtml("#087c50");
        sheet.Row(1).Style.Font.FontColor = XLColor.White; sheet.SheetView.FreezeRows(1);
        if (rows.Count > 0) sheet.Range(1, 1, rows.Count + 1, headers.Length).SetAutoFilter();
        sheet.Column(5).Style.DateFormat.Format = "dd/MM/yyyy";
        sheet.Column(13).Style.DateFormat.Format = "dd/MM/yyyy";
        sheet.Column(12).Style.NumberFormat.Format = "0.##";
        using var output = new MemoryStream(); workbook.SaveAs(output); return output.ToArray();
    }

    public async Task<EmployeeImportResult> ImportEmployees(Guid actorId, bool admin, IFormFile file,
        Base.Services.IServices.IAccountService accounts, CancellationToken ct)
    {
        await RequireHr(actorId, admin, ct);
        const long maxFileSize = 5 * 1024 * 1024;
        if (file.Length == 0 || file.Length > maxFileSize || !string.Equals(Path.GetExtension(file.FileName), ".xlsx", StringComparison.OrdinalIgnoreCase))
            throw new ApiException(400, "Vui lòng chọn file .xlsx có dung lượng từ 1 byte đến 5 MB.");
        using var buffer = new MemoryStream(); await file.CopyToAsync(buffer, ct);
        XLWorkbook workbook;
        try
        {
            buffer.Position = 0;
            using (var archive = new ZipArchive(buffer, ZipArchiveMode.Read, leaveOpen: true))
            {
                long expandedSize = 0;
                foreach (var entry in archive.Entries) { expandedSize += entry.Length; if (expandedSize > 50 * 1024 * 1024) throw new ApiException(400, "Nội dung Excel giải nén vượt quá 50 MB."); }
            }
            buffer.Position = 0; workbook = new XLWorkbook(buffer);
        }
        catch (ApiException) { throw; }
        catch (Exception ex) when (ex is not OperationCanceledException) { throw new ApiException(400, "File Excel không hợp lệ hoặc không đọc được."); }
        using (workbook)
        {
            var sheet = workbook.Worksheets.FirstOrDefault() ?? throw new ApiException(400, "File Excel phải có ít nhất một sheet.");
            var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 0;
            if (lastRow < 2) throw new ApiException(400, "File Excel chưa có dữ liệu nhân viên.");
            if (lastRow > 1001) throw new ApiException(400, "Mỗi lần nhập tối đa 1.000 dòng.");
            var headers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var cell in sheet.Row(1).CellsUsed()) if (!headers.TryAdd(cell.GetString().Trim(), cell.Address.ColumnNumber)) throw new ApiException(400, $"Cột bị trùng: {cell.GetString().Trim()}.");
            foreach (var name in EmployeeImportHeaders.Take(4).Concat(["Code", "Position", "Department", "EmploymentStartDate"]))
                if (!headers.ContainsKey(name)) throw new ApiException(400, $"Thiếu cột bắt buộc: {name}.");

            var result = new EmployeeImportResult();
            var departments = await repo.Query<Department>().ToListAsync(ct);
            var usernames = new HashSet<string>(StringComparer.OrdinalIgnoreCase); var emails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var codes = new HashSet<string>(StringComparer.OrdinalIgnoreCase); var phones = new HashSet<string>(StringComparer.Ordinal);
            var existingUsernames = (await repo.Query<Account>().Select(a => a.Username).ToListAsync(ct)).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var existingEmails = (await repo.Query<Account>().Select(a => a.Email).ToListAsync(ct)).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var existingPhones = (await repo.Query<Account>().Where(a => a.PhoneNumber != null).Select(a => a.PhoneNumber!).ToListAsync(ct))
                .Select(Base.Services.AccountService.NormalizeImportPhone).Where(phone => phone is not null).ToHashSet(StringComparer.Ordinal);
            var existingCodes = (await repo.Query<Employee>().Select(e => e.Code).ToListAsync(ct)).ToHashSet(StringComparer.OrdinalIgnoreCase);
            for (var rowNumber = 2; rowNumber <= lastRow; rowNumber++)
            {
                ct.ThrowIfCancellationRequested(); var row = sheet.Row(rowNumber); if (row.IsEmpty()) continue; result.TotalRows++;
                string Value(string name) => headers.TryGetValue(name, out var col) ? row.Cell(col).GetString().Trim() : "";
                if (row.CellsUsed().Any(cell => cell.HasFormula)) { result.Errors.Add(new(rowNumber, "Dòng nhập không được chứa công thức Excel.")); continue; }
                DateOnly? ParseDate(string name)
                {
                    if (!headers.TryGetValue(name, out var col) || string.IsNullOrWhiteSpace(Value(name))) return null;
                    var cell = row.Cell(col); if (cell.DataType == XLDataType.DateTime) return DateOnly.FromDateTime(cell.GetDateTime());
                    return DateOnly.TryParseExact(Value(name), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) ? date : throw new FormatException($"{name} phải là yyyy-MM-dd hoặc ngày Excel.");
                }
                try
                {
                    var username = Value("Username").ToLowerInvariant(); var email = Value("Email").ToLowerInvariant();
                    var code = Value("Code"); var phone = Base.Services.AccountService.NormalizeImportPhone(Value("PhoneNumber"));
                    var department = departments.SingleOrDefault(d => d.Name.Equals(Value("Department"), StringComparison.OrdinalIgnoreCase));
                    if (department is null) throw new FormatException("Department phải khớp chính xác tên một phòng ban hiện có.");
                    var role = string.IsNullOrWhiteSpace(Value("GoldRole")) ? "employee" : Value("GoldRole").ToLowerInvariant();
                    if (role is not ("employee" or "manager" or "hr")) throw new FormatException("GoldRole chỉ nhận employee, manager hoặc hr.");
                    if (!admin && role == "hr") throw new FormatException("Chỉ quản trị viên được cấp quyền HCNS.");
                    var birth = ParseDate("DateOfBirth"); var employmentStart = ParseDate("EmploymentStartDate") ?? throw new FormatException("EmploymentStartDate là bắt buộc.");
                    var balance = string.IsNullOrWhiteSpace(Value("AnnualLeaveBalance")) ? 0 : decimal.Parse(Value("AnnualLeaveBalance"), CultureInfo.InvariantCulture);
                    var isActive = string.IsNullOrWhiteSpace(Value("IsActive")) || bool.Parse(Value("IsActive"));
                    var request = new EmployeeCreateInput
                    {
                        Username = username, Password = Value("Password"), FullName = Value("FullName"), Email = email,
                        PhoneNumber = string.IsNullOrEmpty(phone) ? null : Value("PhoneNumber"), DateOfBirth = birth,
                        Gender = string.IsNullOrWhiteSpace(Value("Gender")) ? null : Value("Gender"),
                        Address = string.IsNullOrWhiteSpace(Value("Address")) ? null : Value("Address"),
                        Code = code, Position = Value("Position"), DepartmentId = department.Id, GoldRole = role,
                        AnnualLeaveBalance = balance, IsActive = isActive, EmploymentStartDate = employmentStart
                    };
                    var validation = new List<ValidationResult>();
                    if (!Validator.TryValidateObject(request, new ValidationContext(request), validation, true)) { result.Errors.Add(new(rowNumber, string.Join(" ", validation.Select(error => error.ErrorMessage)))); continue; }
                    var duplicateInFile = usernames.Contains(username) || emails.Contains(email) || codes.Contains(code) || (phone != null && phones.Contains(phone));
                    var duplicateInDatabase = existingUsernames.Contains(username) || existingEmails.Contains(email)
                        || (phone != null && existingPhones.Contains(phone)) || existingCodes.Contains(code);
                    if (duplicateInFile || duplicateInDatabase) { result.Duplicates.Add(new(rowNumber, duplicateInDatabase ? "Trùng tên đăng nhập, email, số điện thoại hoặc mã nhân viên với dữ liệu hiện có." : "Trùng tên đăng nhập, email, số điện thoại hoặc mã nhân viên với dòng hợp lệ trước đó trong file.")); continue; }
                    await CreateEmployee(actorId, admin, request, accounts, ct);
                    usernames.Add(username); emails.Add(email); codes.Add(code); if (phone != null) phones.Add(phone);
                    existingUsernames.Add(username); existingEmails.Add(email); existingCodes.Add(code); if (phone != null) existingPhones.Add(phone);
                    result.ImportedCount++;
                }
                catch (FormatException ex) { result.Errors.Add(new(rowNumber, ex.Message)); }
                catch (ApiException ex) when (ex.StatusCode < 500) { result.Errors.Add(new(rowNumber, ex.Message)); }
            }
            audit.Begin("Import", "GoldEmployee", Guid.Empty);
            audit.Complete(new { result.TotalRows, result.ImportedCount, result.DuplicateCount, result.InvalidCount });
            return result;
        }
    }
    public async Task<object> List(Guid id, bool admin, string kind, DateOnly from, DateOnly to, CancellationToken ct)
    {
        _ = GoldRules.Dates(from, to).Count();
        var actor = await Actor(id, admin, ct);
        var scoped = Scope(actor, admin).Select(e => e.Id);
        var attendanceScope = admin || actor.GoldRole == "manager"
            ? scoped
            : repo.Query<Employee>().Where(e => e.Id == id).Select(e => e.Id);
        if (kind == "schedule") return await (from s in repo.Query<WorkSchedule>().Where(s => attendanceScope.Contains(s.EmployeeId) && s.Date >= @from && s.Date <= to)
                                              join a in repo.Query<Account>() on s.EmployeeId equals a.Id
                                              join sh in repo.Query<WorkShift>() on s.ShiftId equals sh.Id into shifts
                                              from sh in shifts.DefaultIfEmpty()
                                              join l in repo.Query<WorkLocation>() on s.LocationId equals l.Id into locations
                                              from l in locations.DefaultIfEmpty()
                                              select new ScheduleResponse(s.Id, s.EmployeeId, a.FullName, s.Date, s.ShiftId, sh == null ? null : sh.Name,
                                                  sh == null ? null : sh.Start.ToString(), sh == null ? null : sh.End.ToString(), s.LocationId, l == null ? null : l.Name, s.Kind, s.Locked)).ToListAsync(ct);
        if (kind == "attendance") return await (from r in repo.Query<AttendanceRecord>().Where(r => attendanceScope.Contains(r.EmployeeId) && r.Date >= @from && r.Date <= to)
                                                join e in repo.Query<Employee>() on r.EmployeeId equals e.Id
                                                join a in repo.Query<Account>() on e.Id equals a.Id
                                                join d in repo.Query<Department>() on e.DepartmentId equals d.Id
                                                orderby r.RecordedAt descending
                                                select new AttendanceResponse(r.Id, e.Id, a.FullName, d.Name, r.Date, r.RecordedAt, r.Type, r.Status,
                                                    r.DeviationMinutes, r.DistanceMeters, r.GpsAccuracy, r.Reason)).ToListAsync(ct);
        return await (from r in repo.Query<GoldRequest>().Where(r => (scoped.Contains(r.EmployeeId) || r.PartnerId == id) && r.From <= to && r.To >= @from && (admin || r.EmployeeId == id || actor.GoldRole == "manager" || r.Type != "supplement"))
                      join e in repo.Query<Employee>() on r.EmployeeId equals e.Id
                      join a in repo.Query<Account>() on e.Id equals a.Id
                      join d in repo.Query<Department>() on e.DepartmentId equals d.Id
                      join p in repo.Query<Account>() on r.PartnerId equals p.Id into partners
                      from p in partners.DefaultIfEmpty()
                      orderby r.CreatedAt descending
                       select new RequestResponse(r.Id, e.Id, a.FullName, d.Name, r.Type, r.LeaveKind, r.From, r.To, r.Reason, r.Handover, r.Status, r.PartnerId, p == null ? null : p.FullName, r.ReviewNote, r.CreatedAt, r.ReplacesRequestId, r.IsCancellation, e.DepartmentId, r.LeaveSession, r.LeaveStart.ToString(), r.LeaveEnd.ToString(), r.PaidLeaveDays, r.UnpaidLeaveDays)).ToListAsync(ct);
    }
    public async Task<object> AssignEmployee(Guid actorId, bool admin, Guid id, EmployeeInput input, CancellationToken ct)
    {
        await RequireHr(actorId, admin, ct);
        // HR can assign employees/managers; only base admin can grant the global HR role.
        if (!admin && input.GoldRole == "hr") throw Error("Chỉ quản trị viên được cấp quyền HCNS.", 403);
        var account = await repo.Query<Account>().SingleOrDefaultAsync(a => a.Id == id, ct);
        if (account is null || !await repo.Query<Department>().AnyAsync(d => d.Id == input.DepartmentId, ct))
            throw Error("Tài khoản hoặc phòng ban không tồn tại.");
        if (!admin && account.Role == "admin") throw Error("Chỉ quản trị viên được chỉnh sửa trạng thái tài khoản admin.", 403);
        if (id == actorId && input.IsActive == false) throw Error("Bạn không thể khóa chính tài khoản đang đăng nhập.", 422);
        var e = await repo.Query<Employee>().SingleOrDefaultAsync(e => e.Id == id, ct);
        if (!admin && e?.GoldRole == "hr") throw Error("Chỉ quản trị viên được thay đổi hồ sơ HCNS.", 403);
        audit.Begin("Update", "GoldEmployee", id, e is null ? null : new { e.Code, e.DepartmentId, e.Position, e.GoldRole, e.AnnualLeaveBalance, e.EmploymentStartDate, account.IsActive, account.DateOfBirth });
        if (e is null) { e = new Employee { Id = id }; repo.Add(e); }
        e.Code = input.Code.Trim(); e.DepartmentId = input.DepartmentId; e.Position = input.Position.Trim(); e.GoldRole = input.GoldRole;
        e.EmploymentStartDate = input.EmploymentStartDate;
        if (input.AnnualLeaveBalance is decimal balance) e.AnnualLeaveBalance = balance;
        if (input.IsActive is bool isActive) account.IsActive = isActive;
        if (input.DateOfBirth is DateOnly dateOfBirth) account.DateOfBirth = dateOfBirth;
        await repo.Save(ct); audit.Complete(new { e.Code, e.DepartmentId, e.Position, e.GoldRole, e.AnnualLeaveBalance, e.EmploymentStartDate, account.IsActive, account.DateOfBirth }); return new { e.Id };
    }
    public async Task<object> CreateEmployee(Guid actorId, bool admin, EmployeeCreateInput input, Base.Services.IServices.IAccountService accounts, CancellationToken ct)
    {
        await RequireHr(actorId, admin, ct);
        if (!admin && input.GoldRole == "hr") throw Error("Chỉ quản trị viên được cấp quyền HCNS.", 403);
        await using var tx = await repo.Transaction(ct);
        var account = await accounts.RegisterAsync(input, ct);
        var result = await AssignEmployee(actorId, admin, account.Id, new(input.Code, input.DepartmentId, input.Position, input.GoldRole, input.AnnualLeaveBalance, input.IsActive, input.DateOfBirth, input.EmploymentStartDate), ct);
        await tx.CommitAsync(ct); return result;
    }
    public async Task<object> RegisterFace(Guid actorId, bool admin, Guid employeeId, FaceEnrollmentInput input, IFaceEnrollment enrollment, CancellationToken ct)
    {
        if (!admin && actorId != employeeId) throw Error("Bạn chỉ được đăng ký khuôn mặt cho tài khoản của mình.", 403);
        var employee = await (from e in repo.Query<Employee>()
                              join a in repo.Query<Account>() on e.Id equals a.Id
                              where e.Id == employeeId && a.IsActive
                              select e).SingleOrDefaultAsync(ct) ?? throw Error("Không tìm thấy nhân viên đang hoạt động.", 404);
        var firstRegistration = string.IsNullOrWhiteSpace(employee.FaceEnrollmentId);
        if (!admin && !firstRegistration) throw Error("Khuôn mặt đã được đăng ký. Chỉ quản trị viên được đăng ký lại.", 403);
        audit.Begin(firstRegistration ? "Create" : "Update", "GoldEmployeeFace", employeeId);
        var result = await enrollment.Register(actorId, employeeId, input.ImageBase64, ct);
        employee.FaceEnrollmentId = result.EnrollmentId;
        employee.FaceRegisteredAt = result.VerifiedAt;
        employee.FaceRegisteredBy = actorId;
        await repo.Save(ct);
        audit.Complete(new { employeeId, registeredBy = actorId, result.EnrollmentId, result.VerifiedAt });
        return new { employeeId, registeredBy = actorId, registeredAt = result.VerifiedAt };
    }
    private async Task<GoldRequest> ReadRequest(Guid id, bool admin, Guid requestId, CancellationToken ct)
    {
        var actor = await Actor(id, admin, ct);
        var r = await repo.Query<GoldRequest>().SingleOrDefaultAsync(r => r.Id == requestId, ct) ?? throw Error("Không tìm thấy đơn.", 404);
        if (!admin && r.EmployeeId != id && r.Type == "supplement" && actor.GoldRole != "manager") throw Error("Chỉ quản trị viên hoặc trưởng phòng trong phạm vi quản lý được xem yêu cầu bổ sung chấm công.", 403);
        if (r.EmployeeId != id && r.PartnerId != id && !await Scope(actor, admin).AnyAsync(e => e.Id == r.EmployeeId, ct)) throw Error("Không có quyền xem đơn.", 403);
        return r;
    }
    public async Task<object> RequestDetail(Guid id, bool admin, Guid requestId, CancellationToken ct)
    {
        var r = await ReadRequest(id, admin, requestId, ct);
        var attachmentName = r.AttachmentId is null ? null : await repo.Query<UploadedFile>().Where(f => f.Id == r.AttachmentId).Select(f => f.OriginalName).SingleOrDefaultAsync(ct);
        var handoverName = r.HandoverEmployeeId is null ? null : await repo.Query<Account>().Where(a => a.Id == r.HandoverEmployeeId).Select(a => a.FullName).SingleOrDefaultAsync(ct);
        var linkedAttendance = r.AttendanceId is null ? null : await repo.Query<AttendanceRecord>().SingleOrDefaultAsync(a => a.Id == r.AttendanceId, ct);
        return new { r.Id, r.Type, r.LeaveKind, r.From, r.To, r.Reason, r.Handover, r.Status, r.Version, r.ReplacesRequestId, r.IsCancellation, r.LeaveSession, leaveStart = r.LeaveStart.ToString(), leaveEnd = r.LeaveEnd.ToString(), r.PaidLeaveDays, r.UnpaidLeaveDays, r.AttachmentId, attachmentName, r.HandoverEmployeeId, handoverName, r.AttendanceId, r.ActualTime, supplementPunches = GetSupplementPunches(r), systemRecordedDate = r.SystemRecordedDate ?? linkedAttendance?.Date, systemRecordedAt = r.SystemRecordedAt ?? linkedAttendance?.RecordedAt, attendanceType = linkedAttendance?.Type ?? r.AttendanceType, attendanceStatus = linkedAttendance?.Status, attendanceDeviationMinutes = linkedAttendance?.DeviationMinutes, r.PartnerDate, canEdit = r.Type == "regular" ? await CanEditRegular(id, r, ct) : r.Type == "leave" && await CanEditLeave(id, r, ct), canCancel = r.EmployeeId == id && r.Type == "leave" && r.Status == "approved" && await CanApproveChange(r, ct) };
    }
    public async Task<UploadedFile> Attachment(Guid id, bool admin, Guid requestId, CancellationToken ct)
    {
        var r = await ReadRequest(id, admin, requestId, ct);
        if (r.AttachmentId is null) throw Error("Đơn không có file đính kèm.", 404);
        return await repo.Query<UploadedFile>().SingleOrDefaultAsync(f => f.Id == r.AttachmentId, ct) ?? throw Error("Không tìm thấy file.", 404);
    }
    public async Task<object> SaveLocation(Guid id, bool admin, Guid? locationId, LocationInput x, CancellationToken ct)
    {
        await RequireHr(id, admin, ct);
        var l = locationId is null ? new WorkLocation() : await repo.Query<WorkLocation>().SingleOrDefaultAsync(l => l.Id == locationId, ct) ?? throw Error("Không có địa điểm.", 404);
        audit.Begin("Update", "GoldLocation", l.Id, new { l.Name, l.Address, l.Latitude, l.Longitude, l.RadiusMeters, l.Active });
        if (locationId is null) repo.Add(l);
        l.Name = x.Name; l.Address = x.Address; l.Latitude = x.Latitude; l.Longitude = x.Longitude; l.RadiusMeters = x.RadiusMeters; l.Active = x.Active;
        await repo.Save(ct); audit.Complete(l); return l;
    }
    public async Task<object> SaveShift(Guid id, bool admin, Guid? shiftId, ShiftInput x, CancellationToken ct)
    {
        await RequireHr(id, admin, ct);
        if (x.End <= x.Start) throw Error("Ca phải kết thúc sau giờ bắt đầu; phiên bản này dùng ca trong ngày.");
        var s = shiftId is null ? new WorkShift() : await repo.Query<WorkShift>().SingleOrDefaultAsync(s => s.Id == shiftId, ct) ?? throw Error("Không có ca.", 404);
        if (shiftId is not null && (s.Start != x.Start || s.End != x.End) && await repo.Query<WorkSchedule>().AnyAsync(w => w.ShiftId == shiftId && w.Date <= Today, ct))
            throw Error("Ca đã được dùng trong lịch hiện tại hoặc quá khứ. Hãy tạo ca mới để giữ dữ liệu đối soát.", 409);
        audit.Begin("Update", "GoldShift", s.Id, new { s.Name, s.Start, s.End }); if (shiftId is null) repo.Add(s);
        s.Name = x.Name; s.Start = x.Start; s.End = x.End; await repo.Save(ct); audit.Complete(s); return s;
    }
    public async Task SavePolicy(Guid id, bool admin, PolicyInput x, CancellationToken ct)
    {
        await RequireHr(id, admin, ct); var p = await repo.Query<AttendancePolicy>().SingleAsync(ct);
        audit.Begin("Update", "GoldPolicy", Guid.Empty, new { p.RegularDays, p.AnnualDays, p.LateMinutes, p.EarlyLeaveMinutes, p.EarlyCheckinMinutes, p.DeadlineDay, p.AllowOffsite, p.WeekendRestricted, p.MaxGpsAccuracyMeters, p.OvertimeAfterMinutes });
        p.RegularDays = x.RegularDays; p.AnnualDays = x.AnnualDays; p.LateMinutes = x.LateMinutes; p.EarlyLeaveMinutes = x.EarlyLeaveMinutes;
        p.EarlyCheckinMinutes = x.EarlyCheckinMinutes; p.DeadlineDay = x.DeadlineDay; p.AllowOffsite = x.AllowOffsite;
        p.WeekendRestricted = x.WeekendRestricted; p.MaxGpsAccuracyMeters = x.MaxGpsAccuracyMeters; p.OvertimeAfterMinutes = x.OvertimeAfterMinutes;
        await repo.Save(ct); audit.Complete(p);
    }
    public async Task<object> Schedule(Guid id, bool admin, ScheduleInput x, CancellationToken ct)
    {
        var actor = await Actor(id, admin, ct);
        if (!admin && actor.GoldRole is not ("manager" or "hr")) throw Error("Chỉ trưởng phòng, HCNS hoặc quản trị viên được xếp lịch.", 403);
        if (!await Scope(actor, admin).AnyAsync(e => e.Id == x.EmployeeId, ct)) throw Error("Nhân viên không thuộc phạm vi quản lý.", 403);
        if (x.Kind == "work" && (x.ShiftId is null || x.LocationId is null
            || !await repo.Query<WorkShift>().AnyAsync(s => s.Id == x.ShiftId, ct)
            || !await repo.Query<WorkLocation>().AnyAsync(l => l.Id == x.LocationId && l.Active, ct))) throw Error("Ca làm hoặc địa điểm không hợp lệ.");
        await using var tx = await repo.Transaction(ct);
        var s = await repo.Query<WorkSchedule>().SingleOrDefaultAsync(s => s.EmployeeId == x.EmployeeId && s.Date == x.Date, ct);
        if (s?.Locked == true || await repo.Query<ScheduleLock>().AnyAsync(l => l.Id == x.Date.Year * 100 + x.Date.Month, ct)
            || await repo.Query<PayrollLock>().AnyAsync(l => l.Id == x.Date.Year * 100 + x.Date.Month, ct)) throw Error("Lịch hoặc bảng công đã khóa. Vui lòng tạo yêu cầu đổi lịch.", 409);
        if (await repo.Query<GoldRequest>().AnyAsync(r => (r.EmployeeId == x.EmployeeId || r.PartnerId == x.EmployeeId)
            && r.Status != "cancelled" && r.Status != "rejected" && ((r.From <= x.Date && r.To >= x.Date) || r.PartnerDate == x.Date), ct))
            throw Error("Ngày này có đơn đang xử lý hoặc đã duyệt. Không thể ghi đè lịch.", 409);
        if (await repo.Query<AttendanceRecord>().AnyAsync(r => r.EmployeeId == x.EmployeeId && r.Date == x.Date, ct)) throw Error("Ngày đã chấm công, không thể thay đổi lịch.", 409);
        audit.Begin("Update", "GoldSchedule", s?.Id ?? Guid.Empty, s is null ? null : new { s.Date, s.Kind, s.ShiftId, s.LocationId });
        if (s is null) { s = new WorkSchedule { EmployeeId = x.EmployeeId, Date = x.Date }; repo.Add(s); }
        s.Kind = x.Kind; s.ShiftId = x.Kind == "work" ? x.ShiftId : null; s.LocationId = x.Kind == "work" ? x.LocationId : null; s.Version++;
        await repo.Save(ct); await tx.CommitAsync(ct); audit.Complete(new { s.Id, s.Date, s.Kind, s.ShiftId, s.LocationId }); return new { s.Id };
    }
    public async Task Lock(Guid id, bool admin, int year, int month, string kind, CancellationToken ct)
    {
        await RequireHr(id, admin, ct); var from = new DateOnly(year, month, 1); var to = from.AddMonths(1).AddDays(-1);
        await using var tx = await repo.Transaction(ct);
        if (kind == "attendance" && to >= Today) throw Error("Chỉ khóa bảng công tháng đã kết thúc.");
        var q = await repo.Query<WorkSchedule>().Where(s => s.Date >= from && s.Date <= to).ToListAsync(ct);
        if (await repo.Query<GoldRequest>().AnyAsync(r => ((r.From <= to && r.To >= from) || (r.PartnerDate >= from && r.PartnerDate <= to)) && r.Status != "approved" && r.Status != "rejected" && r.Status != "cancelled", ct))
            throw Error("Còn đơn chưa hoàn tất trong tháng. Hãy xử lý trước khi khóa.");
        audit.Begin("Lock", "GoldSchedule", Guid.Empty, new { year, month, kind });
        if (kind == "attendance")
        {
            var period = year * 100 + month;
            if (!await repo.Query<PayrollLock>().AnyAsync(l => l.Id == period, ct)) repo.Add(new PayrollLock { Id = period, LockedAt = UtcNow, LockedBy = id });
        }
        else
        {
            var period = year * 100 + month;
            if (!await repo.Query<ScheduleLock>().AnyAsync(l => l.Id == period, ct)) repo.Add(new ScheduleLock { Id = period, LockedAt = UtcNow, LockedBy = id });
            foreach (var s in q) { s.Locked = true; s.Version++; }
        }
        await repo.Save(ct); await tx.CommitAsync(ct); audit.Complete(new { year, month, count = q.Count });
    }
    public async Task ScheduleBatch(Guid id, bool admin, ScheduleBatchInput x, CancellationToken ct)
    {
        var actor = await Actor(id, admin, ct);
        if (!admin && actor.GoldRole is not ("manager" or "hr")) throw Error("Chỉ trưởng phòng, HCNS hoặc quản trị viên được xếp lịch.", 403);
        if (!await Scope(actor, admin).AnyAsync(e => e.Id == x.EmployeeId, ct)) throw Error("Nhân viên không thuộc phạm vi quản lý.", 403);
        var dates = GoldRules.Dates(x.From, x.To).ToArray();
        if (dates.Length > 31 || x.WorkingDays.Any(d => (int)d < 0 || (int)d > 6)) throw Error("Chỉ xếp tối đa 31 ngày với các thứ hợp lệ.");
        if (!await repo.Query<WorkShift>().AnyAsync(s => s.Id == x.ShiftId, ct) || !await repo.Query<WorkLocation>().AnyAsync(l => l.Id == x.LocationId && l.Active, ct)) throw Error("Ca hoặc địa điểm không hợp lệ.");
        await using var tx = await repo.Transaction(ct);
        var periods = dates.Select(d => d.Year * 100 + d.Month).Distinct().ToArray();
        if (await repo.Query<ScheduleLock>().AnyAsync(l => periods.Contains(l.Id), ct) || await repo.Query<PayrollLock>().AnyAsync(l => periods.Contains(l.Id), ct)) throw Error("Có tháng đã khóa trong khoảng xếp lịch.", 409);
        if (await repo.Query<GoldRequest>().AnyAsync(r => (r.EmployeeId == x.EmployeeId || r.PartnerId == x.EmployeeId) && r.Status != "rejected" && r.Status != "cancelled"
            && ((r.From <= x.To && r.To >= x.From) || (r.PartnerDate >= x.From && r.PartnerDate <= x.To)), ct)) throw Error("Có đơn từ trong khoảng xếp lịch. Không thể ghi đè.", 409);
        if (await repo.Query<AttendanceRecord>().AnyAsync(r => r.EmployeeId == x.EmployeeId && r.Date >= x.From && r.Date <= x.To, ct)) throw Error("Có ngày đã chấm công. Không thể ghi đè.", 409);
        var existing = await repo.Query<WorkSchedule>().Where(s => s.EmployeeId == x.EmployeeId && s.Date >= x.From && s.Date <= x.To).ToListAsync(ct);
        if (existing.Any(s => s.Locked)) throw Error("Có lịch đã khóa.", 409);
        audit.Begin("BatchSchedule", "GoldSchedule", x.EmployeeId, existing.Select(s => new { s.Date, s.Kind, s.ShiftId, s.LocationId }));
        foreach (var date in dates)
        {
            var s = existing.SingleOrDefault(s => s.Date == date);
            if (s is null) { s = new WorkSchedule { EmployeeId = x.EmployeeId, Date = date }; repo.Add(s); }
            var work = x.WorkingDays.Contains(date.DayOfWeek); s.Kind = work ? "work" : "off";
            s.ShiftId = work ? x.ShiftId : null; s.LocationId = work ? x.LocationId : null; s.Version++;
        }
        await repo.Save(ct); await tx.CommitAsync(ct); audit.Complete(new { x.EmployeeId, x.From, x.To, x.ShiftId, x.LocationId, x.WorkingDays });
    }
    public async Task<object> Punch(Guid id, bool admin, PunchInput x, string ip, CancellationToken ct)
    {
        var actor = await Actor(id, admin, ct); var now = UtcNow; var date = DateOnly.FromDateTime(GoldRules.LocalNow(now));
        if (string.IsNullOrWhiteSpace(actor.FaceEnrollmentId)) throw Error("Tài khoản chưa được admin đăng ký khuôn mặt.", 403);
        if (actor.EmploymentStartDate is { } startDate && date < startDate) throw Error("Chưa đến ngày bắt đầu làm việc của bạn.", 403);
        await using var tx = await repo.Transaction(ct);
        var schedule = await repo.Query<WorkSchedule>().SingleOrDefaultAsync(s => s.EmployeeId == id && s.Date == date, ct);
        if (schedule is not null && schedule.Kind != "work") throw Error("Hôm nay là ngày nghỉ theo lịch, không thể chấm công.");
        var policy = await repo.Query<AttendancePolicy>().SingleAsync(ct);
        if (!double.IsFinite(x.Latitude) || !double.IsFinite(x.Longitude) || !double.IsFinite(x.GpsAccuracy) || x.GpsAccuracy > policy.MaxGpsAccuracyMeters)
            throw Error("GPS chưa đủ chính xác. Vui lòng thử lại ở nơi thoáng.");
        var shift = schedule?.ShiftId is { } shiftId
            ? await repo.Query<WorkShift>().SingleAsync(s => s.Id == shiftId, ct)
            : await repo.Query<WorkShift>().OrderByDescending(s => s.Name.ToLower().Contains("hành chính")).ThenBy(s => s.Start).FirstOrDefaultAsync(ct)
                ?? throw Error("Chưa cấu hình ca làm mặc định. Hãy liên hệ HCNS.", 503);
        var location = schedule?.LocationId is { } locationId
            ? await repo.Query<WorkLocation>().SingleOrDefaultAsync(l => l.Id == locationId, ct)
                ?? throw Error("Địa điểm trong lịch làm không còn tồn tại. Hãy liên hệ quản lý.", 409)
            : (await repo.Query<WorkLocation>().Where(l => l.Active).ToListAsync(ct))
                .OrderBy(l => GoldRules.Distance(x.Latitude, x.Longitude, l.Latitude, l.Longitude)).FirstOrDefault()
                ?? throw Error("Chưa có địa điểm chấm công đang hoạt động. Hãy liên hệ HCNS.", 503);
        if (!location.Active) throw Error("Địa điểm đã ngừng nhận chấm công.");
        var distance = GoldRules.Distance(x.Latitude, x.Longitude, location.Latitude, location.Longitude);
        var offsite = distance > location.RadiusMeters;
        if (offsite && !policy.AllowOffsite) throw Error($"Bạn đang cách cơ sở {distance:F0} m, ngoài bán kính {location.RadiusMeters} m.");
        var (timeStatus, minutes) = GoldRules.Classify(x.Type, GoldRules.LocalNow(now), shift, policy);
        var status = GoldRules.CombineLocationStatus(timeStatus, offsite);
        if (status != "normal" && x.Reason.Trim().Length < 5) throw Error("Chấm công bất thường cần lý do giải trình ít nhất 5 ký tự.");
        if (await repo.Query<AttendanceRecord>().AnyAsync(r => r.EmployeeId == id && r.Date == date && r.Type == x.Type, ct)) throw Error("Đã chấm công loại này hôm nay.", 409);
        if (x.Type == "out" && !await repo.Query<AttendanceRecord>().AnyAsync(r => r.EmployeeId == id && r.Date == date && r.Type == "in", ct)) throw Error("Chưa có giờ vào. Hãy gửi đơn bổ sung chấm công.");
        var proof = await face.Verify(id, x.ImageBase64, ct);
        if (await repo.Query<AttendanceRecord>().AnyAsync(r => r.VerificationId == proof.VerificationId, ct)) throw Error("Phiên xác thực đã sử dụng.", 409);
        var r = new AttendanceRecord
        {
            EmployeeId = id,
            Date = date,
            ShiftId = shift.Id,
            LocationId = location.Id,
            RecordedAt = now,
            Type = x.Type,
            Latitude = x.Latitude,
            Longitude = x.Longitude,
            DistanceMeters = distance,
            GpsAccuracy = x.GpsAccuracy,
            FaceConfidence = proof.Confidence,
            FacePhoto = FaceEnrollment.DecodeImage(x.ImageBase64),
            VerificationId = proof.VerificationId,
            Device = x.Device,
            Ip = ip,
            Status = status,
            DeviationMinutes = minutes,
            Reason = x.Reason
        };
        repo.Add(r);
        if (status != "normal") repo.Add(new GoldRequest { EmployeeId = id, Type = "explanation", From = date, To = date, AttendanceId = r.Id, ActualTime = TimeOnly.FromDateTime(r.RecordedAt.AddHours(7)), SystemRecordedAt = r.RecordedAt, SystemRecordedDate = date, Reason = x.Reason });
        audit.Begin("Create", "GoldAttendance", r.Id); await repo.Save(ct); await tx.CommitAsync(ct);
        audit.Complete(new { r.Id, r.Date, r.Type, r.Status, r.DeviationMinutes, r.DistanceMeters });
        return new { r.Id, r.RecordedAt, r.Type, r.Status, r.DeviationMinutes, r.DistanceMeters };
    }
    private async Task ValidateRequest(Employee e, GoldRequest r, CancellationToken ct, bool registering = false, IReadOnlySet<DateOnly>? releasing = null)
    {
        if (r.Type is not ("regular" or "supplement") && r.Reason.Trim().Length < 5) throw Error("Vui lòng nhập lý do ít nhất 5 ký tự.");
        if (r.Type == "supplement" && r.Reason.Trim().Length < 5 && r.AttachmentId is null) throw Error("Đơn bổ sung cần có giải trình ít nhất 5 ký tự hoặc file đính kèm.");
        var dates = GoldRules.Dates(r.From, r.To).ToArray();
        var policy = await repo.Query<AttendancePolicy>().SingleAsync(ct);
        if (r.Type is "leave" or "regular")
        {
            if (r.From < Today) throw Error("Không đăng ký nghỉ cho ngày đã qua.");
            if (await repo.Query<GoldRequest>().AnyAsync(q => q.Id != r.Id && q.Id != r.ReplacesRequestId && !q.IsCancellation && q.EmployeeId == e.Id && (q.Type == "leave" || q.Type == "regular")
                && q.Status != "rejected" && q.Status != "cancelled" && q.From <= r.To && q.To >= r.From, ct)) throw Error("Trùng ngày với đơn nghỉ khác.");
            foreach (var month in dates.GroupBy(d => new { d.Year, d.Month }))
            {
                if (registering)
                {
                    var total = (await RegularBooked(e.Id, new(month.Key.Year, month.Key.Month, 1), ct, r.Id, releasing, r.ReplacesRequestId)).Count + month.Count();
                    await ValidateRegularExplanation(e, month, total, r.Reason, r.HandoverEmployeeId, ct);
                }
                if (r.Type != "regular") continue;
                var begin = new DateOnly(month.Key.Year, month.Key.Month, 1); var end = begin.AddMonths(1).AddDays(-1);
                var existing = await repo.Query<GoldRequest>().Where(q => q.Id != r.Id && q.EmployeeId == e.Id && q.Type == r.Type
                    && (r.Type == "regular" || q.LeaveKind == "annual") && q.Status != "rejected" && q.Status != "cancelled" && q.From <= end && q.To >= begin).ToListAsync(ct);
                var booked = existing.SelectMany(q => GoldRules.Dates(q.From, q.To)).Where(d => d >= begin && d <= end).ToHashSet();
                if (r.Type == "regular") booked = await RegularBooked(e.Id, begin, ct, r.Id);
                var count = booked.Count;
                _ = count; // Regular leave exceptions are governed by explanation and handover rules above.
            }
            if (await repo.Query<PeakDay>().AnyAsync(p => p.Date >= r.From && p.Date <= r.To && p.BlockLeave, ct)) throw Error("Có ngày cao điểm không được đăng ký nghỉ.");
            if (r.Type != "regular" && policy.WeekendRestricted && dates.Any(GoldRules.Weekend) && (r.HandoverEmployeeId is null || r.Handover.Trim().Length < 5))
                throw Error("Nghỉ cuối tuần cần người thay thế và nội dung bàn giao.");
            var schedules = await repo.Query<WorkSchedule>().Where(s => s.EmployeeId == e.Id && s.Date >= r.From && s.Date <= r.To).ToListAsync(ct);
            if ((r.Type != "regular" && schedules.Count != dates.Length) || schedules.Any(s => (s.Kind != "work" && !(s.Kind == "off" && releasing?.Contains(s.Date) == true)) || (r.Type == "regular" && s.Locked))) throw Error("Ngày đã có lịch nghỉ hoặc lịch đã khóa; nghỉ phép cần lịch làm hợp lệ.");
            foreach (var month in dates.Select(d => d.Year * 100 + d.Month).Distinct())
            {
                if (r.Type == "regular" && await repo.Query<ScheduleLock>().AnyAsync(l => l.Id == month, ct)) throw Error("Lịch tháng đã khóa.");
                if (await repo.Query<PayrollLock>().AnyAsync(l => l.Id == month, ct)) throw Error("Bảng công tháng đã khóa.");
            }
            if (r.Type == "regular" && await repo.Query<AttendanceRecord>().AnyAsync(a => a.EmployeeId == e.Id && a.Date >= r.From && a.Date <= r.To, ct)) throw Error("Ngày đã chấm công không thể đăng ký nghỉ.");
        }
        if (r.HandoverEmployeeId is Guid handover && !await (from p in repo.Query<Employee>()
                                                             join a in repo.Query<Account>() on p.Id equals a.Id
                                                             where p.Id == handover && p.Id != e.Id && p.DepartmentId == e.DepartmentId && a.IsActive
                                                             select p.Id).AnyAsync(ct))
            throw Error("Người bàn giao phải là nhân viên khác cùng phòng ban.");
        if (r.AttachmentId is Guid file && !await repo.Query<UploadedFile>().AnyAsync(f => f.Id == file && f.UploadedBy == e.Id, ct)) throw Error("File đính kèm không thuộc tài khoản.");
        if (r.Type == "swap")
        {
            if (r.PartnerId is null || r.PartnerDate is null || r.From != r.To || r.From < Today || r.PartnerDate < Today || r.PartnerDate == r.From) throw Error("Thông tin đổi lịch không hợp lệ.");
            var partner = await repo.Query<Employee>().SingleOrDefaultAsync(p => p.Id == r.PartnerId, ct);
            if (partner is null || partner.Id == e.Id || partner.DepartmentId != e.DepartmentId || partner.Position != e.Position) throw Error("Đổi lịch cần hai nhân viên cùng phòng và cùng chuyên môn.");
            var ownOff = await repo.Query<WorkSchedule>().SingleOrDefaultAsync(s => s.EmployeeId == e.Id && s.Date == r.From, ct);
            var partnerOff = await repo.Query<WorkSchedule>().SingleOrDefaultAsync(s => s.EmployeeId == partner.Id && s.Date == r.PartnerDate, ct);
            var ownWork = await repo.Query<WorkSchedule>().SingleOrDefaultAsync(s => s.EmployeeId == e.Id && s.Date == r.PartnerDate, ct);
            var partnerWork = await repo.Query<WorkSchedule>().SingleOrDefaultAsync(s => s.EmployeeId == partner.Id && s.Date == r.From, ct);
            if (ownOff?.Kind != "off" || partnerOff?.Kind != "off" || ownWork?.Kind != "work" || partnerWork?.Kind != "work") throw Error("Hai ngày cần có lịch nghỉ và ca làm đối ứng.");
            if (await repo.Query<PayrollLock>().AnyAsync(l => l.Id == r.From.Year * 100 + r.From.Month || l.Id == r.PartnerDate.Value.Year * 100 + r.PartnerDate.Value.Month, ct)) throw Error("Bảng công đã khóa, không thể đổi lịch.");
            if (await repo.Query<AttendanceRecord>().AnyAsync(a => (a.EmployeeId == e.Id || a.EmployeeId == partner.Id) && (a.Date == r.From || a.Date == r.PartnerDate), ct)) throw Error("Ngày đã chấm công, không thể đổi lịch.");
            if (await repo.Query<GoldRequest>().AnyAsync(q => q.Id != r.Id && q.Type == "swap" && q.Status != "rejected" && q.Status != "cancelled" && q.Status != "approved"
                && (q.EmployeeId == e.Id || q.EmployeeId == partner.Id || q.PartnerId == e.Id || q.PartnerId == partner.Id)
                && (q.From == r.From || q.From == r.PartnerDate || q.PartnerDate == r.From || q.PartnerDate == r.PartnerDate), ct)) throw Error("Có yêu cầu đổi lịch khác đang chờ xử lý.");
        }
        if (r.Type == "explanation")
        {
            var attendance = r.AttendanceId is null ? null : await repo.Query<AttendanceRecord>().SingleOrDefaultAsync(a => a.Id == r.AttendanceId && a.EmployeeId == e.Id, ct);
            if (r.AttendanceId is not null && attendance is null) throw Error("Không tìm thấy lượt chấm công của chính nhân viên.");
            if (attendance is not null && attendance.Status is not ("late" or "early-leave" or "early-checkin" or "offsite" or "late-offsite" or "early-leave-offsite" or "early-checkin-offsite"))
                throw Error("Chỉ dùng giải trình cho lượt chấm công bất thường. Trường hợp quên chấm vào/ra, hãy gửi đơn bổ sung chấm công.");
            if (attendance is not null)
            {
                if (r.From != r.To || r.From > Today || r.ActualTime is null) throw Error("Khi liên kết lượt chấm công hệ thống, cần chọn ngày và giờ thực tế hợp lệ.");
                var actualAt = DateTime.SpecifyKind(r.From.ToDateTime(r.ActualTime.Value).AddHours(-7), DateTimeKind.Utc);
                if (actualAt > UtcNow) throw Error("Giờ chấm công thực tế không thể ở tương lai.");
                var lockedMonths = new[] { r.From.Year * 100 + r.From.Month, attendance.Date.Year * 100 + attendance.Date.Month }.Distinct().ToArray();
                if (await repo.Query<PayrollLock>().AnyAsync(l => lockedMonths.Contains(l.Id), ct)) throw Error("Bảng công của ngày chấm công đã khóa.");
                if (await repo.Query<AttendanceRecord>().AnyAsync(a => a.Id != attendance.Id && a.EmployeeId == e.Id && a.Date == r.From && a.Type == attendance.Type && a.RecordedAt == actualAt, ct)) throw Error("Ngày thực tế đã có lượt chấm công trùng giờ.");
                var actualSchedule = await repo.Query<WorkSchedule>().SingleOrDefaultAsync(s => s.EmployeeId == e.Id && s.Date == r.From, ct);
                if (actualSchedule is null || actualSchedule.Kind != "work" || actualSchedule.ShiftId is null || actualSchedule.LocationId is null) throw Error("Ngày thực tế cần có lịch làm việc hợp lệ.");
            }
        }
        if (r.Type == "supplement")
        {
            var punches = GetSupplementPunches(r);
            if (r.From != r.To || r.From > Today || punches.Length is < 1 or > 4) throw Error("Đơn bổ sung cần ngày đã qua hoặc hôm nay và từ 1 đến 4 lượt giờ thực tế.");
            if (punches.Any(p => p.Type is not ("in" or "out")) || punches.Zip(punches.Skip(1), (a, b) => b.Time <= a.Time).Any(outOfOrder => outOfOrder)) throw Error("Các lượt chấm công phải theo đúng thứ tự thời gian và luân phiên vào/ra.");
            if (punches.Zip(punches.Skip(1), (a, b) => a.Type == b.Type).Any(repeatedType => repeatedType)) throw Error("Các lượt chấm công phải luân phiên giờ vào và giờ ra.");
            if (punches.Any(p => DateTime.SpecifyKind(r.From.ToDateTime(p.Time).AddHours(-7), DateTimeKind.Utc) > UtcNow)) throw Error("Giờ bổ sung không thể ở tương lai.");
            var s = await repo.Query<WorkSchedule>().SingleOrDefaultAsync(s => s.EmployeeId == e.Id && s.Date == r.From, ct);
            if (s is null || s.Kind != "work" || await repo.Query<PayrollLock>().AnyAsync(l => l.Id == r.From.Year * 100 + r.From.Month, ct)) throw Error("Không có ca làm hoặc bảng công đã khóa.");
            var timeline = await repo.Query<AttendanceRecord>().Where(a => a.EmployeeId == e.Id && a.Date == r.From).Select(a => new SupplementPunchInput(TimeOnly.FromDateTime(a.RecordedAt.AddHours(7)), a.Type)).ToListAsync(ct);
            var pending = await repo.Query<GoldRequest>().Where(q => q.Id != r.Id && q.EmployeeId == e.Id && q.Type == "supplement" && q.From == r.From && q.Status != "rejected" && q.Status != "cancelled").ToListAsync(ct);
            timeline.AddRange(pending.SelectMany(GetSupplementPunches));
            timeline.AddRange(punches);
            timeline = timeline.OrderBy(p => p.Time).ToList();
            if (timeline.Count > 4) throw Error("Một ngày chỉ có thể ghi nhận tối đa 4 lượt chấm công.");
            if (timeline.Zip(timeline.Skip(1), (a, b) => a.Time == b.Time).Any(duplicate => duplicate)) throw Error("Đã có lượt chấm công hoặc đơn bổ sung trùng thời gian.");
            if (timeline.Zip(timeline.Skip(1), (a, b) => a.Type == b.Type).Any(repeatedType => repeatedType)) throw Error("Các lượt chấm công trong ngày phải luân phiên giờ vào và giờ ra.");
        }
    }
    private static SupplementPunchInput[] GetSupplementPunches(GoldRequest request)
    {
        var punches = string.IsNullOrWhiteSpace(request.SupplementPunchesJson) ? [] : JsonSerializer.Deserialize<SupplementPunchInput[]>(request.SupplementPunchesJson) ?? [];
        return punches.Length > 0 ? punches : request.ActualTime is TimeOnly time && request.AttendanceType is string type ? [new(time, type)] : [];
    }
    public async Task<IReadOnlyList<CapacityWarning>> Capacity(Guid department, DateOnly from, DateOnly to, Guid? exclude, CancellationToken ct)
    {
        var warnings = new List<CapacityWarning>();
        var rules = await repo.Query<StaffingRule>().Where(r => r.DepartmentId == department).ToListAsync(ct);
        foreach (var day in GoldRules.Dates(from, to)) foreach (var rule in rules)
        {
            var people = repo.Query<Employee>().Where(e => e.DepartmentId == department && e.Position == rule.Position && e.Id != exclude).Select(e => e.Id);
            var pending = repo.Query<GoldRequest>().Where(r => (r.Type == "leave" || r.Type == "regular") && r.Status != "approved" && r.Status != "rejected" && r.Status != "cancelled" && r.From <= day && r.To >= day).Select(r => r.EmployeeId);
            var count = await repo.Query<WorkSchedule>().CountAsync(s => people.Contains(s.EmployeeId) && s.Date == day && s.Kind == "work" && !pending.Contains(s.EmployeeId), ct);
            var min = GoldRules.Weekend(day) ? rule.WeekendMinimum : rule.WeekdayMinimum;
            if (count < min) warnings.Add(new(day, rule.Position, count, min));
        }
        return warnings;
    }
    private async Task ValidateRegularExplanation(Employee employee, IEnumerable<DateOnly> dates, int monthlyTotal, string? reason, Guid? handover, CancellationToken ct)
    {
        if (!dates.Any(d => d.DayOfWeek == DayOfWeek.Sunday) && monthlyTotal < 5) return;
        if ((reason ?? "").Trim().Length < 5) throw Error("Nghỉ Chủ nhật hoặc từ 5 ngày trong tháng cần giải trình ít nhất 5 ký tự.");
        if (handover is not Guid person || !await (from e in repo.Query<Employee>()
                                                   join a in repo.Query<Account>() on e.Id equals a.Id
                                                   where e.Id == person && e.Id != employee.Id && e.DepartmentId == employee.DepartmentId && a.IsActive
                                                   select e.Id).AnyAsync(ct))
            throw Error("Vui lòng chọn người bàn giao đang hoạt động trong cùng phòng ban.");
    }
    private async Task<(string Session, TimeOnly Start, TimeOnly End)> ResolveLeaveSession(string? value, CancellationToken ct)
    {
        var session = value is "morning" or "afternoon" or "full" ? value : "full";
        var shifts = await repo.Query<WorkShift>().ToListAsync(ct);
        static bool Has(string name, string word) => name.Contains(word, StringComparison.OrdinalIgnoreCase);
        var shift = session == "morning" ? shifts.FirstOrDefault(s => Has(s.Name, "sáng"))
            : session == "afternoon" ? shifts.FirstOrDefault(s => Has(s.Name, "chiều"))
            : shifts.FirstOrDefault(s => Has(s.Name, "hành chính") || Has(s.Name, "cả ngày"));
        var fallback = session == "morning" ? (new TimeOnly(7, 30), new TimeOnly(11, 30))
            : session == "afternoon" ? (new TimeOnly(13, 30), new TimeOnly(17, 30))
            : (new TimeOnly(7, 30), new TimeOnly(17, 30));
        return (session, shift?.Start ?? fallback.Item1, shift?.End ?? fallback.Item2);
    }
    private async Task<HashSet<DateOnly>> RegularBooked(Guid id, DateOnly begin, CancellationToken ct, Guid? exclude = null, IReadOnlySet<DateOnly>? releasing = null, Guid? previous = null)
    {
        var end = begin.AddMonths(1).AddDays(-1);
        var replacedIds = repo.Query<GoldRequest>()
            .Where(r => r.EmployeeId == id && r.Id != exclude && r.ReplacesRequestId != null
                && r.Status != "approved" && r.Status != "rejected" && r.Status != "cancelled")
            .Select(r => r.ReplacesRequestId!.Value);
        var replaced = await repo.Query<GoldRequest>().Where(r => replacedIds.Contains(r.Id) && r.Status == "approved").ToListAsync(ct);
        var releasedDates = replaced.SelectMany(r => GoldRules.Dates(r.From, r.To)).ToHashSet();
        if (releasing is not null) releasedDates.UnionWith(releasing);
        var requests = await repo.Query<GoldRequest>()
            .Where(r => r.Id != exclude && r.Id != previous && !replacedIds.Contains(r.Id) && !r.IsCancellation
                && r.EmployeeId == id && (r.Type == "regular" || r.Type == "leave")
                && r.Status != "rejected" && r.Status != "cancelled" && r.From <= end && r.To >= begin)
            .ToListAsync(ct);
        var booked = requests.SelectMany(r => GoldRules.Dates(r.From, r.To)).Where(d => d >= begin && d <= end).ToHashSet();
        booked.UnionWith((await repo.Query<WorkSchedule>().Where(s => s.EmployeeId == id && (s.Kind == "off" || s.Kind == "leave") && s.Date >= begin && s.Date <= end).Select(s => s.Date).ToListAsync(ct)).Except(releasedDates));
        return booked;
    }
    public async Task<object> RegularCalendar(Guid id, bool admin, CancellationToken ct, string? month = null)
    {
        await Actor(id, admin, ct);
        var begin = new DateOnly(Today.Year, Today.Month, 1).AddMonths(1);
        if (month is not null) { if (!DateOnly.TryParseExact(month + "-01", "yyyy-MM-dd", out begin) || begin.Year < 2000 || begin.Year > 2100) throw Error("Tháng không hợp lệ."); }
        var end = begin.AddMonths(1).AddDays(-1);
        var policy = await repo.Query<AttendancePolicy>().SingleAsync(ct);
        var registered = await RegularBooked(id, begin, ct); var usedDays = registered.Count;
        registered.UnionWith(await repo.Query<WorkSchedule>().Where(s => s.EmployeeId == id && s.Kind != "work" && s.Date >= begin && s.Date <= end).Select(s => s.Date).ToListAsync(ct));
        var blocked = (await repo.Query<PeakDay>().Where(p => p.BlockLeave && p.Date >= begin && p.Date <= end).Select(p => p.Date).ToListAsync(ct)).ToHashSet();
        blocked.UnionWith(await repo.Query<WorkSchedule>().Where(s => s.EmployeeId == id && s.Date >= begin && s.Date <= end && (s.Kind != "work" || s.Locked)).Select(s => s.Date).ToListAsync(ct));
        var other = await repo.Query<GoldRequest>().Where(r => r.EmployeeId == id && r.Type == "leave" && r.Status != "rejected" && r.Status != "cancelled" && r.From <= end && r.To >= begin).ToListAsync(ct);
        blocked.UnionWith(other.SelectMany(r => GoldRules.Dates(r.From, r.To)).Where(d => d >= begin && d <= end));
        blocked.UnionWith(GoldRules.Dates(begin, end).Where(d => d < Today));
        blocked.UnionWith(await repo.Query<AttendanceRecord>().Where(a => a.EmployeeId == id && a.Date >= begin && a.Date <= end).Select(a => a.Date).ToListAsync(ct));
        var period = begin.Year * 100 + begin.Month;
        var locked = await repo.Query<ScheduleLock>().AnyAsync(l => l.Id == period, ct) || await repo.Query<PayrollLock>().AnyAsync(l => l.Id == period, ct);
        var registrations = new List<object>();
        foreach (var r in await repo.Query<GoldRequest>().Where(r => r.EmployeeId == id && r.Type == "regular" && r.Status != "rejected" && r.Status != "cancelled" && r.From <= end && r.To >= begin).ToListAsync(ct))
            registrations.Add(new { r.Id, r.From, r.To, r.Status, r.IsCancellation, r.ReplacesRequestId, canEdit = await CanEditRegular(id, r, ct) });
        return new { month = begin.ToString("yyyy-MM"), today = Today, maxDays = policy.RegularDays, usedDays, registeredDates = registered.Order().ToArray(), blockedDates = blocked.Order().ToArray(), registrations, locked, deadlineDay = policy.DeadlineDay, expired = end < Today };
    }
    public async Task<object> RegisterRegular(Guid id, bool admin, RegularLeaveInput input, CancellationToken ct)
    {
        var e = await Actor(id, admin, ct);
        if (!await repo.Query<Employee>().AnyAsync(p => p.Id == id, ct)) throw Error("Cần hồ sơ nhân viên trước khi đăng ký nghỉ.");
        var dates = input.Dates?.Order().ToArray() ?? [];
        if (dates.Length == 0 || dates.Distinct().Count() != dates.Length) throw Error("Chọn ngày nghỉ, mỗi ngày chỉ được chọn một lần.");
        await using var tx = await repo.Transaction(ct);
        var begin = new DateOnly(dates[0].Year, dates[0].Month, 1);
        if (dates.Any(d => d.Year != begin.Year || d.Month != begin.Month)) throw Error("Mỗi lần đăng ký chọn ngày trong cùng một tháng.");
        var booked = await RegularBooked(id, begin, ct);
        await ValidateRegularExplanation(e, dates, booked.Count + dates.Length, input.Reason, input.HandoverEmployeeId, ct);
        var leaveTime = await ResolveLeaveSession(input.LeaveSession, ct);
        var requests = new List<GoldRequest>();
        foreach (var day in dates)
        {
            var r = new GoldRequest { EmployeeId = id, Type = "regular", From = day, To = day, LeaveSession = leaveTime.Session, LeaveStart = leaveTime.Start, LeaveEnd = leaveTime.End, Reason = (input.Reason ?? "").Trim(), HandoverEmployeeId = input.HandoverEmployeeId, Handover = input.Handover ?? "", Status = "manager", CreatedAt = UtcNow };
            await ValidateRequest(e, r, ct); requests.Add(r);
        }
        foreach (var r in requests) repo.Add(r);
        audit.Begin("Create", "GoldRegularCalendar", id);
        await NotifyNext(requests[0], e.DepartmentId, ct);
        await repo.Save(ct); await tx.CommitAsync(ct); audit.Complete(new { dates });
        return new { dates, status = "manager", ids = requests.Select(r => r.Id).ToArray() };
    }
    public async Task<object> CreateRequest(Guid id, bool admin, RequestInput x, CancellationToken ct)
    {
        var e = await Actor(id, admin, ct);
        if (!await repo.Query<Employee>().AnyAsync(p => p.Id == id, ct)) throw Error("Cần hồ sơ nhân viên trước khi tạo đơn.");
        if (x.Type == "regular") throw Error("Đăng ký nghỉ định kỳ phải thực hiện qua lịch đăng ký nghỉ.");
        if (x.Type == "leave")
        {
            if (x.From > x.To) throw Error("Ngày bắt đầu phải trước hoặc bằng ngày kết thúc.");
            var leaveDays = GoldRules.Dates(x.From, x.To).ToArray();
            if (leaveDays.Length > 1 && leaveDays.Zip(leaveDays.Skip(1), (a, b) => b.DayNumber - a.DayNumber).Any(gap => gap != 1))
                throw Error("Một đơn nghỉ phép chỉ được chọn các ngày liền mạch.");
            if (leaveDays.Select(d => d.Month).Distinct().Count() > 1 || leaveDays.Select(d => d.Year).Distinct().Count() > 1)
                throw Error("Mỗi đơn nghỉ phép chỉ được đăng ký trong cùng một tháng.");
            var relevantRequests = await repo.Query<GoldRequest>().Where(r => r.EmployeeId == id && (r.Type == "leave" || r.Type == "regular")
                && !r.IsCancellation && r.Status != "rejected" && r.Status != "cancelled"
                && r.From <= x.To && r.To >= x.From).ToListAsync(ct);
            var overlapping = relevantRequests.FirstOrDefault(r => r.From <= x.To && r.To >= x.From);
            if (overlapping is not null) throw Error($"Ngày nghỉ trùng với đơn đã đăng ký ({overlapping.From:dd/MM/yyyy}–{overlapping.To:dd/MM/yyyy}, trạng thái: {overlapping.Status}).");
            var begin = new DateOnly(x.From.Year, x.From.Month, 1);
            var end = begin.AddMonths(1).AddDays(-1);
            var approvedAndPendingDays = await repo.Query<GoldRequest>().Where(r => r.EmployeeId == id && (r.Type == "leave" || r.Type == "regular")
                && !r.IsCancellation && r.Status != "rejected" && r.Status != "cancelled"
                && r.From <= end && r.To >= begin).ToListAsync(ct);
            var occupied = approvedAndPendingDays.SelectMany(r => GoldRules.Dates(r.From, r.To)).Distinct().Count();
            var requestedTotal = occupied + (x.To.DayNumber - x.From.DayNumber + 1);
            await ValidateRegularExplanation(e, leaveDays, requestedTotal, x.Reason, x.HandoverEmployeeId, ct);
        }
        var leaveTime = await ResolveLeaveSession(x.LeaveSession, ct);
        var endLeaveTime = await ResolveLeaveSession(x.EndLeaveSession, ct);
        if (x.Type == "leave" && x.From == x.To && x.LeaveSession != x.EndLeaveSession)
            throw Error("Đơn nghỉ một ngày chỉ chọn một ca nghỉ.");
        if (x.Type == "leave" && x.From < x.To && (x.LeaveSession == "morning" || x.EndLeaveSession == "afternoon"))
            throw Error("Đơn nghỉ nhiều ngày phải chọn ngày đầu từ ca chiều/cả ngày và ngày cuối đến ca sáng/cả ngày.");
        await using var tx = await repo.Transaction(ct);
        var r = new GoldRequest
        {
            EmployeeId = id,
            Type = x.Type,
            LeaveKind = x.LeaveKind,
            From = x.From,
            To = x.To,
            LeaveSession = leaveTime.Session,
            LeaveStart = leaveTime.Start,
            LeaveEnd = x.Type == "leave" ? endLeaveTime.End : leaveTime.End,
            Reason = (x.Reason ?? "").Trim(),
            PartnerId = x.PartnerId,
            PartnerDate = x.PartnerDate,
            AttendanceId = x.AttendanceId,
            ActualTime = x.Type == "supplement" ? x.SupplementPunches?.FirstOrDefault()?.Time : x.ActualTime,
            SystemRecordedAt = x.Type == "explanation" && x.AttendanceId is Guid attendanceId ? await repo.Query<AttendanceRecord>().Where(a => a.Id == attendanceId && a.EmployeeId == id).Select(a => (DateTime?)a.RecordedAt).SingleOrDefaultAsync(ct) : null,
            SystemRecordedDate = x.Type == "explanation" && x.AttendanceId is Guid attendanceDateId ? await repo.Query<AttendanceRecord>().Where(a => a.Id == attendanceDateId && a.EmployeeId == id).Select(a => (DateOnly?)a.Date).SingleOrDefaultAsync(ct) : null,
            AttendanceType = x.Type == "supplement" ? x.SupplementPunches?.FirstOrDefault()?.Type : x.AttendanceType,
            SupplementPunchesJson = x.Type == "supplement" ? JsonSerializer.Serialize(x.SupplementPunches ?? []) : "[]",
            HandoverEmployeeId = x.HandoverEmployeeId,
            Handover = x.Handover,
            AttachmentId = x.AttachmentId,
            Status = x.Type == "swap" ? "partner" : "manager",
            CreatedAt = UtcNow
        };
        await ValidateRequest(e, r, ct, true); repo.Add(r); audit.Begin("Create", "GoldRequest", r.Id);
        await NotifyNext(r, e.DepartmentId, ct);
        await repo.Save(ct); await tx.CommitAsync(ct); audit.Complete(new { r.Id, r.Type, r.Status, r.From, r.To });
        return new { r.Id, r.Status, warnings = await Capacity(e.DepartmentId, r.From, r.To, id, ct) };
    }
    private async Task<bool> CanEditRegular(Guid id, GoldRequest r, CancellationToken ct)
    {
        if (r.EmployeeId != id || r.Type != "regular" || r.IsCancellation || r.From <= Today || r.Status is not ("manager" or "hr" or "approved")) return false;
        if (await repo.Query<GoldRequest>().AnyAsync(q => q.ReplacesRequestId == r.Id && q.Status != "approved" && q.Status != "rejected" && q.Status != "cancelled", ct)) return false;
        var periods = GoldRules.Dates(r.From, r.To).Select(d => d.Year * 100 + d.Month).Distinct().ToArray();
        return !await repo.Query<ScheduleLock>().AnyAsync(l => periods.Contains(l.Id), ct)
            && !await repo.Query<PayrollLock>().AnyAsync(l => periods.Contains(l.Id), ct)
            && !await repo.Query<WorkSchedule>().AnyAsync(s => s.EmployeeId == id && s.Date >= r.From && s.Date <= r.To && s.Locked, ct)
            && !await repo.Query<AttendanceRecord>().AnyAsync(a => a.EmployeeId == id && a.Date >= r.From && a.Date <= r.To, ct);
    }
    private async Task<bool> CanEditLeave(Guid id, GoldRequest r, CancellationToken ct)
    {
        if (r.EmployeeId != id || r.Type != "leave" || r.IsCancellation || r.From <= Today || r.Status is not ("manager" or "hr")) return false;
        var periods = GoldRules.Dates(r.From, r.To).Select(d => d.Year * 100 + d.Month).Distinct().ToArray();
        return !await repo.Query<ScheduleLock>().AnyAsync(l => periods.Contains(l.Id), ct)
            && !await repo.Query<PayrollLock>().AnyAsync(l => periods.Contains(l.Id), ct)
            && !await repo.Query<WorkSchedule>().AnyAsync(s => s.EmployeeId == id && s.Date >= r.From && s.Date <= r.To && s.Locked, ct)
            && !await repo.Query<AttendanceRecord>().AnyAsync(a => a.EmployeeId == id && a.Date >= r.From && a.Date <= r.To, ct);
    }
    public async Task EditLeaveRequest(Guid id, bool admin, Guid requestId, LeaveRequestEditInput input, CancellationToken ct)
    {
        var employee = await Actor(id, admin, ct);
        var r = await repo.Query<GoldRequest>().SingleOrDefaultAsync(r => r.Id == requestId, ct) ?? throw Error("Không tìm thấy đơn nghỉ phép.", 404);
        if (r.EmployeeId != id) throw Error("Chỉ được sửa đơn nghỉ phép của mình.", 403);
        if (!await CanEditLeave(id, r, ct) || input.From <= Today) throw Error("Chỉ sửa đơn chưa được duyệt, trước ngày nghỉ và khi lịch chưa khóa.", 409);
        if (input.Version != r.Version) throw Error("Đơn đã thay đổi. Vui lòng tải lại trước khi sửa.", 409);
        if (input.From > input.To || input.From.Year != input.To.Year || input.From.Month != input.To.Month)
            throw Error("Đơn nghỉ phép phải nằm trong cùng một tháng và ngày kết thúc không trước ngày bắt đầu.");
        var leaveTime = await ResolveLeaveSession(input.LeaveSession, ct);
        var endTime = await ResolveLeaveSession(input.EndLeaveSession, ct);
        if (input.From == input.To && input.LeaveSession != input.EndLeaveSession) throw Error("Đơn nghỉ một ngày chỉ chọn một ca.");
        if (input.From < input.To && (input.LeaveSession == "morning" || input.EndLeaveSession == "afternoon")) throw Error("Đơn nghỉ nhiều ngày cần bắt đầu từ ca chiều/cả ngày và kết thúc ca sáng/cả ngày.");
        var candidate = new GoldRequest { Id = r.Id, EmployeeId = id, Type = "leave", LeaveKind = input.LeaveKind, From = input.From, To = input.To, LeaveSession = leaveTime.Session, LeaveStart = leaveTime.Start, LeaveEnd = endTime.End, Reason = input.Reason.Trim(), HandoverEmployeeId = input.HandoverEmployeeId, Handover = input.Handover ?? "", Status = "manager", CreatedAt = r.CreatedAt };
        await ValidateRequest(employee, candidate, ct, true);
        audit.Begin("Edit", "GoldRequest", r.Id, new { r.From, r.To, r.Status, r.Version });
        r.From = candidate.From; r.To = candidate.To; r.LeaveKind = candidate.LeaveKind; r.LeaveSession = candidate.LeaveSession;
        r.LeaveStart = candidate.LeaveStart; r.LeaveEnd = candidate.LeaveEnd; r.Reason = candidate.Reason;
        r.HandoverEmployeeId = candidate.HandoverEmployeeId; r.Handover = candidate.Handover; r.Status = "manager";
        r.Version++; r.ReviewerId = null; r.ReviewedAt = null; r.ReviewNote = "";
        await NotifyNext(r, employee.DepartmentId, ct); await repo.Save(ct); audit.Complete(new { r.Id, r.From, r.To, r.Status, r.Version });
    }
    public async Task EditRegular(Guid id, bool admin, Guid requestId, RegularLeaveEditInput input, CancellationToken ct)
    {
        var employee = await Actor(id, admin, ct);
        await using var tx = await repo.Transaction(ct);
        var r = await repo.Query<GoldRequest>().SingleOrDefaultAsync(r => r.Id == requestId, ct) ?? throw Error("Không tìm thấy đăng ký nghỉ.", 404);
        if (r.EmployeeId != id) throw Error("Chỉ được sửa đăng ký nghỉ của mình.", 403);
        if (!await CanEditRegular(id, r, ct) || input.From <= Today) throw Error("Chỉ sửa trước ngày nghỉ, khi lịch chưa khóa và chưa chấm công.", 409);
        if (input.Version != r.Version) throw Error("Đăng ký đã thay đổi. Vui lòng tải lại lịch.", 409);
        var previous = r.ReplacesRequestId is Guid parentId ? await repo.Query<GoldRequest>().SingleAsync(q => q.Id == parentId, ct) : r;
        var previousDays = previous.Status == "approved" ? GoldRules.Dates(previous.From, previous.To).ToHashSet() : new HashSet<DateOnly>();
        var leaveTime = await ResolveLeaveSession(input.LeaveSession, ct);
        var candidate = new GoldRequest { Id = r.Status == "approved" ? Guid.NewGuid() : r.Id, EmployeeId = id, Type = "regular", ReplacesRequestId = r.Status == "approved" ? r.Id : r.ReplacesRequestId, From = input.From, To = input.To, LeaveSession = leaveTime.Session, LeaveStart = leaveTime.Start, LeaveEnd = leaveTime.End, Reason = (input.Reason ?? "").Trim(), HandoverEmployeeId = input.HandoverEmployeeId, Handover = input.Handover ?? "", CreatedAt = UtcNow };
        await ValidateRequest(employee, candidate, ct, true, previousDays);
        audit.Begin("Edit", "GoldRequest", r.Id, new { r.From, r.To, r.Status, r.Version });
        if (r.Status == "approved") repo.Add(candidate);
        else
        {
            r.From = candidate.From; r.To = candidate.To; r.LeaveSession = candidate.LeaveSession; r.LeaveStart = candidate.LeaveStart; r.LeaveEnd = candidate.LeaveEnd; r.Reason = candidate.Reason; r.HandoverEmployeeId = candidate.HandoverEmployeeId; r.Handover = candidate.Handover;
            r.Status = "manager"; r.Version++; r.ReviewerId = null; r.ReviewedAt = null; r.ReviewNote = "";
            candidate = r;
        }
        await NotifyNext(candidate, employee.DepartmentId, ct);
        await repo.Save(ct); await tx.CommitAsync(ct); audit.Complete(new { candidate.Id, candidate.From, candidate.To, candidate.Status, candidate.Version });
    }
    private async Task RestorePrevious(GoldRequest previous, CancellationToken ct)
    {
        if (previous.Status == "approved" && previous.Type == "leave")
        {
            if (previous.LeaveKind == "annual" && previous.PaidLeaveDays > 0)
            {
                var employee = await repo.Query<Employee>().SingleAsync(e => e.Id == previous.EmployeeId, ct);
                employee.AnnualLeaveBalance += previous.PaidLeaveDays;
            }
            var schedules = await repo.Query<WorkSchedule>().Where(s => s.EmployeeId == previous.EmployeeId && s.Date >= previous.From && s.Date <= previous.To && s.Kind == "leave").ToListAsync(ct);
            foreach (var schedule in schedules)
            {
                if (schedule.Locked) throw Error("Lịch nghỉ cũ đã bị khóa.", 409);
                schedule.Kind = "work";
                schedule.Version++;
            }
        }
        else if (previous.Status == "approved" && previous.Type == "regular" && previous.LeaveSession == "full")
        {
            var schedules = await repo.Query<WorkSchedule>().Where(s => s.EmployeeId == previous.EmployeeId && s.Date >= previous.From && s.Date <= previous.To).ToListAsync(ct);
            foreach (var s in schedules)
            {
                if (s.Kind != "off" || s.Locked) throw Error("Lịch nghỉ cũ đã thay đổi hoặc bị khóa.", 409);
                if (s.ShiftId is not null && s.LocationId is not null) { s.Kind = "work"; s.Version++; }
                else repo.Remove(s);
            }
        }
        previous.Status = "cancelled"; previous.Version++;
    }
    private async Task AllocateAnnualLeave(Employee employee, GoldRequest request, CancellationToken ct)
    {
        var shifts = await repo.Query<WorkShift>().ToListAsync(ct);
        var morningEnd = shifts.FirstOrDefault(s => s.Name.Contains("sáng", StringComparison.OrdinalIgnoreCase))?.End ?? new TimeOnly(11, 30);
        var fullEnd = shifts.FirstOrDefault(s => s.Name.Contains("hành chính", StringComparison.OrdinalIgnoreCase) || s.Name.Contains("cả ngày", StringComparison.OrdinalIgnoreCase))?.End ?? new TimeOnly(17, 30);
        var requestedDays = LeaveDurationDays(request, morningEnd, fullEnd);
        var paidDays = Math.Min(employee.AnnualLeaveBalance, requestedDays);
        request.PaidLeaveDays = paidDays;
        request.UnpaidLeaveDays = requestedDays - paidDays;
        employee.AnnualLeaveBalance -= paidDays;
    }
    private static decimal LeaveDurationDays(GoldRequest request, TimeOnly morningEnd, TimeOnly fullEnd)
    {
        if (request.From == request.To)
            return request.LeaveSession == "full" && request.LeaveEnd >= fullEnd ? 1m : 0.5m;

        decimal days = request.To.DayNumber - request.From.DayNumber + 1;
        if (request.LeaveSession == "afternoon") days -= 0.5m;
        if (request.LeaveEnd <= morningEnd) days -= 0.5m;
        return Math.Max(0m, days);
    }
    public async Task Review(Guid id, bool admin, Guid requestId, ReviewInput x, CancellationToken ct)
    {
        var actor = await Actor(id, admin, ct);
        await using var tx = await repo.Transaction(ct);
        var r = await repo.Query<GoldRequest>().SingleOrDefaultAsync(r => r.Id == requestId, ct) ?? throw Error("Không tìm thấy đơn.", 404);
        var owner = await repo.Query<Employee>().SingleAsync(e => e.Id == r.EmployeeId, ct);
        audit.Begin("Review", "GoldRequest", r.Id, new { r.Status, r.ReviewNote, r.Version });
        if (await repo.Query<GoldRequest>().AnyAsync(q => q.ReplacesRequestId == r.Id && q.Status != "approved" && q.Status != "rejected" && q.Status != "cancelled", ct)) throw Error("Đang có yêu cầu thay đổi hoặc hủy chờ duyệt.", 409);
        if (r.Status is "rejected" or "cancelled" || (r.Status == "approved" && x.Action != "cancel")) throw Error("Đơn đã hoàn tất.", 409);
        if (x.Action == "cancel")
        {
            if (r.EmployeeId != id) throw Error("Chỉ người tạo được hủy đơn đang chờ.", 403);
            if (r.Type == "regular" && !await CanEditRegular(id, r, ct)) throw Error("Không thể hủy ngày đã đến hoặc đăng ký trong lịch đã khóa.", 409);
            if (r.Type == "regular" && r.Status == "approved")
            {
                var cancellation = new GoldRequest { EmployeeId = id, Type = "regular", From = r.From, To = r.To, ReplacesRequestId = r.Id, IsCancellation = true, Reason = "Đề nghị hủy đăng ký nghỉ", CreatedAt = UtcNow };
                repo.Add(cancellation); await NotifyNext(cancellation, owner.DepartmentId, ct); await repo.Save(ct); await tx.CommitAsync(ct); audit.Complete(new { cancellation.Id, cancellation.Status }); return;
            }
            if (r.Type == "leave" && r.Status == "approved")
            {
                if (!await CanApproveChange(r, ct)) throw Error("Chỉ gửi đề nghị hủy trước ngày nghỉ và khi lịch chưa khóa.", 409);
                var cancellation = new GoldRequest
                {
                    EmployeeId = id,
                    Type = "leave",
                    LeaveKind = r.LeaveKind,
                    From = r.From,
                    To = r.To,
                    LeaveSession = r.LeaveSession,
                    LeaveStart = r.LeaveStart,
                    LeaveEnd = r.LeaveEnd,
                    ReplacesRequestId = r.Id,
                    IsCancellation = true,
                    Reason = "Đề nghị hủy đơn nghỉ phép",
                    Status = "manager",
                    CreatedAt = UtcNow
                };
                repo.Add(cancellation);
                await NotifyNext(cancellation, owner.DepartmentId, ct);
                await repo.Save(ct);
                await tx.CommitAsync(ct);
                audit.Complete(new { cancellation.Id, cancellation.Status });
                return;
            }
            // Pending registrations are drafts until approved. Removing them keeps
            // cancelled, never-approved rows out of the request history.
            if (r.Status is "manager" or "hr" or "pending" or "partner")
            {
                repo.Remove(r);
                await repo.Save(ct);
                await tx.CommitAsync(ct);
                audit.Complete(new { r.Id, Status = "deleted" });
                return;
            }
            r.Status = "cancelled";
        }
        else if (r.Status == "partner")
        {
            if (r.PartnerId != id) throw Error("Chỉ người được đề nghị đổi lịch được xác nhận.", 403);
            if (x.Action != "accept" && x.Action != "reject") throw Error("Cần xác nhận đồng ý hoặc từ chối.");
            r.Status = x.Action == "accept" ? "manager" : "rejected";
        }
        else
        {
            if (id == r.EmployeeId) throw Error("Không được tự duyệt đơn của mình.", 403);
            var leaveWorkflow = r.Type is "regular" or "leave" or "swap";
            var canReview = r.Type == "supplement" ? admin
                : r.Type == "explanation" && !admin && actor.GoldRole == "manager" ? false
                : leaveWorkflow ? admin || (actor.GoldRole == "manager" && actor.DepartmentId == owner.DepartmentId)
                : r.Status == "manager" ? admin || (actor.GoldRole == "manager" && actor.DepartmentId == owner.DepartmentId)
                : r.Status == "hr" && (admin || actor.GoldRole == "hr");
            if (!canReview) throw Error("Không có quyền duyệt ở bước này.", 403);
            if (x.Action != "approve" && x.Action != "reject") throw Error("Hành động duyệt không hợp lệ.");
            if (x.Action == "approve" && r.Type == "explanation")
            {
                if (r.AttendanceId is not null && (x.ActualDate is null) != (x.ActualTime is null)) throw Error("Cần nhập cả ngày và giờ chấm công thực tế.");
                if (x.ActualDate is DateOnly actualDate && x.ActualTime is TimeOnly actualTime)
                {
                    r.From = actualDate;
                    r.To = actualDate;
                    r.ActualTime = actualTime;
                }
                if (r.AttendanceId is not null && r.ActualTime is null) throw Error("Đơn giải trình chưa có giờ chấm công thực tế.");
            }
            if (x.Action == "reject")
            {
                if (x.Note.Trim().Length < 5) throw Error("Từ chối cần lý do ít nhất 5 ký tự."); r.Status = "rejected";
            }
            else if (r.Status == "manager" && !leaveWorkflow) r.Status = "hr";
            else
            {
                GoldRequest? previous = r.ReplacesRequestId is Guid previousId ? await repo.Query<GoldRequest>().SingleAsync(q => q.Id == previousId, ct) : null;
                if (previous is not null && !await CanApproveChange(previous, ct)) throw Error("Ngày nghỉ cũ đã đến hoặc lịch bị khóa.", 409);
                var releasing = previous?.Status == "approved" ? GoldRules.Dates(previous.From, previous.To).ToHashSet() : null;
                if (!r.IsCancellation) await ValidateRequest(owner, r, ct, false, releasing);
                var warnings = await Capacity(owner.DepartmentId, r.From, r.To, owner.Id, ct);
                if (!r.IsCancellation && warnings.Count > 0 && x.Note.Trim().Length < 5) throw Error("Thiếu nhân sự tối thiểu. Người duyệt cần ghi lý do duyệt ngoại lệ.");
                if (previous is not null) { await RestorePrevious(previous, ct); await repo.Save(ct); }
                if (!r.IsCancellation)
                {
                    if (r.Type == "leave" && r.LeaveKind == "annual") await AllocateAnnualLeave(owner, r, ct);
                    await ApplyApproved(r, ct);
                }
                r.Status = "approved";
            }
        }
        r.Version++; r.ReviewNote = x.Note; r.ReviewerId = id; r.ReviewedAt = UtcNow;
        await NotifyNext(r, owner.DepartmentId, ct);
        if (r.Status is "approved" or "rejected")
        {
            var requestName = r.Type switch
            {
                "leave" => "Đơn nghỉ phép",
                "regular" => "Đăng ký nghỉ định kỳ",
                "swap" => "Yêu cầu đổi lịch",
                "explanation" => "Đơn giải trình chấm công",
                "supplement" => "Đơn bổ sung chấm công",
                _ => "Yêu cầu"
            };
            var title = r.Status == "approved" ? "Đơn của bạn đã được duyệt" : "Đơn của bạn bị từ chối";
            var content = $"{requestName} · {r.From:dd/MM/yyyy} → {r.To:dd/MM/yyyy}";
            if (r.Status == "rejected" && !string.IsNullOrWhiteSpace(r.ReviewNote)) content += $" · Lý do: {r.ReviewNote}";
            repo.Add(new Notification { UserId = r.EmployeeId, Title = title, Content = content, Link = "/#requests" });
        }
        await repo.Save(ct); await tx.CommitAsync(ct); audit.Complete(new { r.Status, r.ReviewNote, r.ReviewerId, r.Version });
    }
    private async Task<bool> CanApproveChange(GoldRequest r, CancellationToken ct)
    {
        var periods = GoldRules.Dates(r.From, r.To).Select(d => d.Year * 100 + d.Month).Distinct().ToArray();
        return r.From > Today && r.Status is ("approved" or "manager" or "hr")
            && !await repo.Query<ScheduleLock>().AnyAsync(l => periods.Contains(l.Id), ct)
            && !await repo.Query<PayrollLock>().AnyAsync(l => periods.Contains(l.Id), ct)
            && !await repo.Query<WorkSchedule>().AnyAsync(s => s.EmployeeId == r.EmployeeId && s.Date >= r.From && s.Date <= r.To && s.Locked, ct)
            && !await repo.Query<AttendanceRecord>().AnyAsync(a => a.EmployeeId == r.EmployeeId && a.Date >= r.From && a.Date <= r.To, ct);
    }
    private async Task NotifyNext(GoldRequest r, Guid department, CancellationToken ct)
    {
        var recipients = r.Status == "partner" ? new List<Guid> { r.PartnerId!.Value }
            : r.Type == "supplement" && (r.Status is "manager" or "hr") ? await repo.Query<Account>().Where(a => a.IsActive && a.Role == "admin" && a.Id != r.EmployeeId).Select(a => a.Id).ToListAsync(ct)
            : r.Type == "explanation" && r.Status == "manager" ? await repo.Query<Account>().Where(a => a.IsActive && a.Role == "admin" && a.Id != r.EmployeeId).Select(a => a.Id).ToListAsync(ct)
            : r.Status is "manager" or "hr" ? await (from e in repo.Query<Employee>()
                                                     join a in repo.Query<Account>() on e.Id equals a.Id
                                                     where a.IsActive && e.Id != r.EmployeeId && (r.Status == "manager" ? e.GoldRole == "manager" && e.DepartmentId == department : e.GoldRole == "hr")
                                                     select e.Id).ToListAsync(ct) : [];
        if ((r.Type is "regular" or "leave" or "swap" or "explanation") && (r.Status is "manager" or "hr"))
            recipients.AddRange(await repo.Query<Account>().Where(a => a.IsActive && a.Role == "admin" && a.Id != r.EmployeeId).Select(a => a.Id).ToListAsync(ct));
        var requestName = r.Type switch
        {
            "leave" => "Đơn nghỉ phép",
            "regular" => "Đăng ký nghỉ định kỳ",
            "swap" => "Yêu cầu đổi lịch",
            "explanation" => "Đơn giải trình chấm công",
            "supplement" => "Đơn bổ sung chấm công",
            _ => "Yêu cầu"
        };
        var reviewLink = r.Type == "explanation" ? "/#explanation-approvals" : r.Type == "supplement" ? "/#supplement-approvals" : "/#approvals";
        foreach (var recipient in recipients.Distinct()) repo.Add(new Notification { UserId = recipient, Title = "Có yêu cầu cần xử lý", Content = $"{requestName} · {r.From:dd/MM/yyyy} → {r.To:dd/MM/yyyy}", Link = reviewLink });
    }
    private async Task ApplyApproved(GoldRequest r, CancellationToken ct)
    {
        if (r.Type is "leave" or "regular")
        {
            var schedules = await repo.Query<WorkSchedule>().Where(s => s.EmployeeId == r.EmployeeId && s.Date >= r.From && s.Date <= r.To).ToListAsync(ct);
            var shifts = r.Type == "leave" ? await repo.Query<WorkShift>().ToListAsync(ct) : [];
            var morningStart = shifts.FirstOrDefault(s => s.Name.Contains("sáng", StringComparison.OrdinalIgnoreCase))?.Start ?? new TimeOnly(7, 30);
            var afternoonEnd = shifts.FirstOrDefault(s => s.Name.Contains("chiều", StringComparison.OrdinalIgnoreCase))?.End ?? new TimeOnly(17, 30);
            foreach (var s in schedules)
            {
                var fullDay = r.Type == "regular" ? r.LeaveSession == "full" : IsFullLeaveDay(r, s.Date, morningStart, afternoonEnd);
                if (fullDay) { s.Kind = r.Type == "regular" ? "off" : "leave"; s.Version++; }
            }
            if (r.Type == "regular" && r.LeaveSession == "full") foreach (var day in GoldRules.Dates(r.From, r.To).Except(schedules.Select(s => s.Date)))
                repo.Add(new WorkSchedule { EmployeeId = r.EmployeeId, Date = day, Kind = "off" });
        }
        else if (r.Type == "swap")
        {
            var schedules = await repo.Query<WorkSchedule>().Where(s => (s.EmployeeId == r.EmployeeId || s.EmployeeId == r.PartnerId) && (s.Date == r.From || s.Date == r.PartnerDate)).ToListAsync(ct);
            var ownOff = schedules.Single(s => s.EmployeeId == r.EmployeeId && s.Date == r.From);
            var otherWork = schedules.Single(s => s.EmployeeId == r.PartnerId && s.Date == r.From);
            var otherOff = schedules.Single(s => s.EmployeeId == r.PartnerId && s.Date == r.PartnerDate);
            var ownWork = schedules.Single(s => s.EmployeeId == r.EmployeeId && s.Date == r.PartnerDate);
            (ownOff.Kind, ownOff.ShiftId, ownOff.LocationId) = (otherWork.Kind, otherWork.ShiftId, otherWork.LocationId);
            (otherOff.Kind, otherOff.ShiftId, otherOff.LocationId) = (ownWork.Kind, ownWork.ShiftId, ownWork.LocationId);
            ownWork.Kind = "off"; otherWork.Kind = "off"; foreach (var s in schedules) s.Version++;
        }
        else if (r.Type == "explanation")
        {
            if (r.AttendanceId is not Guid attendanceId) return;
            var record = await repo.Query<AttendanceRecord>().SingleAsync(a => a.Id == attendanceId, ct);
            var schedule = await repo.Query<WorkSchedule>().SingleAsync(s => s.EmployeeId == r.EmployeeId && s.Date == r.From && s.Kind == "work", ct);
            if (schedule.ShiftId is null || schedule.LocationId is null) throw Error("Ngày chấm công thực tế không có ca làm hợp lệ.");
            record.Date = r.From;
            record.RecordedAt = DateTime.SpecifyKind(r.From.ToDateTime(r.ActualTime!.Value).AddHours(-7), DateTimeKind.Utc);
            record.ShiftId = schedule.ShiftId.Value;
            record.LocationId = schedule.LocationId.Value;
            record.Status = "explained"; // The request retains the original server timestamp and this applies the approved actual time.
        }
        else if (r.Type == "supplement")
        {
            var s = await repo.Query<WorkSchedule>().SingleAsync(s => s.EmployeeId == r.EmployeeId && s.Date == r.From, ct);
            var punches = GetSupplementPunches(r);
            for (var index = 0; index < punches.Length; index++)
            {
                var punch = punches[index];
                repo.Add(new AttendanceRecord
                {
                    EmployeeId = r.EmployeeId,
                    Date = r.From,
                    ShiftId = s.ShiftId!.Value,
                    LocationId = s.LocationId!.Value,
                    RecordedAt = DateTime.SpecifyKind(r.From.ToDateTime(punch.Time).AddHours(-7), DateTimeKind.Utc),
                    Type = punch.Type,
                    Status = "supplemented",
                    Reason = r.Reason,
                    VerificationId = $"request:{r.Id}:{index + 1}",
                    Device = "Approved supplement"
                });
            }
        }
    }

    private static bool IsFullLeaveDay(GoldRequest request, DateOnly date, TimeOnly morningStart, TimeOnly afternoonEnd)
    {
        if (date > request.From && date < request.To) return true;
        if (request.From == request.To)
            return request.LeaveStart <= morningStart && request.LeaveEnd >= afternoonEnd;
        if (date == request.From) return request.LeaveStart <= morningStart;
        if (date == request.To) return request.LeaveEnd >= afternoonEnd;
        return false;
    }
    public async Task<object> SaveStaffing(Guid id, bool admin, StaffingInput x, CancellationToken ct)
    {
        await RequireHr(id, admin, ct);
        if (!await repo.Query<Department>().AnyAsync(d => d.Id == x.DepartmentId, ct)) throw Error("Phòng ban không tồn tại.");
        var r = await repo.Query<StaffingRule>().SingleOrDefaultAsync(r => r.DepartmentId == x.DepartmentId && r.Position == x.Position, ct);
        if (r is null) { r = new StaffingRule { DepartmentId = x.DepartmentId, Position = x.Position }; repo.Add(r); }
        audit.Begin("Update", "GoldStaffingRule", r.Id, new { r.WeekdayMinimum, r.WeekendMinimum });
        r.WeekdayMinimum = x.WeekdayMinimum; r.WeekendMinimum = x.WeekendMinimum; await repo.Save(ct); audit.Complete(r); return r;
    }
    public async Task SavePeak(Guid id, bool admin, PeakInput x, CancellationToken ct)
    {
        await RequireHr(id, admin, ct); var p = await repo.Query<PeakDay>().SingleOrDefaultAsync(p => p.Date == x.Date, ct);
        audit.Begin("Update", "GoldPeakDay", Guid.Empty, p is null ? null : new { p.Date, p.Reason, p.BlockLeave });
        if (p is null) { p = new PeakDay { Date = x.Date }; repo.Add(p); }
        p.Reason = x.Reason; p.BlockLeave = x.BlockLeave;
        await repo.Save(ct); audit.Complete(p);
    }
}
