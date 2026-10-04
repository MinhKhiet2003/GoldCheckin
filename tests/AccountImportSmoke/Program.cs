using Base.Common;
using Base.Entities;
using Base.Exceptions;
using Base.Models.Requests;
using Base.Repositories.IRepositories;
using Base.Services;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;

var repo = new MemoryRepository();
var hasher = new PasswordHasher<Account>();
var service = new AccountService(repo, hasher, null!, new AuditContext());
using var template = new MemoryStream(AccountService.CreateImportTemplate());
using var workbook = new XLWorkbook(template);
var sheet = workbook.Worksheet(1);
void Row(int row, string username, string email, string password = "ValidPassword123!")
{
    sheet.Cell(row, 1).Value = username;
    sheet.Cell(row, 2).Value = password;
    sheet.Cell(row, 3).Value = "Test User";
    sheet.Cell(row, 4).Value = email;
}
Row(2, " ALICE ", " ALICE@example.com ");
sheet.Cell(2, 5).Value = "0901234567";
sheet.Cell(2, 6).Value = "2000-01-02";
Row(3, "alice", "second@example.com");
Row(4, "bob", "bad-email");
Row(5, "existing", "new@example.com");
Row(6, "charlie", "charlie@example.com");
sheet.Cell(6, 3).FormulaA1 = "1+1";
Row(7, "david", "david@example.com", "short");
Row(8, "phone_db", "phone_db@example.com");
sheet.Cell(8, 5).Value = "091 234 5678";
Row(9, "phone_file", "phone_file@example.com");
sheet.Cell(9, 5).Value = "090-123-4567";
Row(10, "email_db", "existing@example.com");
Row(11, "new_user", "new@example.net");
using var data = new MemoryStream();
workbook.SaveAs(data);
data.Position = 0;
var result = await service.ImportAsync(new FormFile(data, 0, data.Length, "file", "accounts.xlsx"), default);
Check(result.TotalRows == 10 && result.ImportedCount == 2 && result.DuplicateCount == 5 && result.InvalidCount == 3 && result.FailedCount == 8, "Row counts");
Check(result.Errors.Select(x => x.Row).SequenceEqual(new[] {4, 6, 7}), "Excel error rows");
Check(result.Duplicates.Select(x => x.Row).SequenceEqual(new[] {3, 5, 8, 9, 10}), "Duplicate rows");
using var reportStream = new MemoryStream(AccountService.CreateDuplicateReport(result.Duplicates));
using var report = new XLWorkbook(reportStream);
Check(report.Worksheet(1).LastRowUsed()!.RowNumber() == 6, "Report row count");
Check(!report.Worksheet(1).Row(1).CellsUsed().Any(c => c.GetString() == "Password"), "Report excludes passwords");
Check(report.Worksheet(1).Cell(2, 2).GetString() == "alice", "Report identity");
var account = repo.Items.First();
Check(account.Username == "alice" && account.Email == "alice@example.com", "Normalization");
Check(account.Role == "user" && account.IsActive, "Default role");
Check(account.PhoneNumber == "0901234567" && account.DateOfBirth == new DateOnly(2000, 1, 2), "Profile fields");
Check(hasher.VerifyHashedPassword(account, account.PasswordHash, "ValidPassword123!") != PasswordVerificationResult.Failed, "Password hash");
Check(repo.SaveCount == 1, "Single save");
using var invalid = new MemoryStream(new byte[] {1, 2, 3});
try
{
    await service.ImportAsync(new FormFile(invalid, 0, invalid.Length, "file", "bad.xlsx"), default);
    throw new Exception("Invalid workbook accepted");
}
catch (ApiException ex) { Check(ex.StatusCode == 400, "Invalid workbook response"); }
using var missingWorkbook = new XLWorkbook();
missingWorkbook.AddWorksheet("Accounts").Cell(2, 1).Value = "missing headers";
using var missing = new MemoryStream();
missingWorkbook.SaveAs(missing);
missing.Position = 0;
try
{
    await service.ImportAsync(new FormFile(missing, 0, missing.Length, "file", "missing.xlsx"), default);
    throw new Exception("Missing headers accepted");
}
catch (ApiException ex) { Check(ex.StatusCode == 400, "Missing header response"); }
Console.WriteLine("PASS: template, valid import, duplicates, validation, formulas, password hashing, profile, invalid workbook and missing headers.");
static void Check(bool passed, string name) { if (!passed) throw new Exception(name); }

sealed class MemoryRepository : IAccountRepository
{
    public List<Account> Items { get; } = [];
    public int SaveCount { get; private set; }
    public Task<bool> ExistsForImportAsync(string username, string email, string? phoneNumber, CancellationToken ct) =>
        Task.FromResult(username == "existing" || email == "existing@example.com" || phoneNumber == "0912345678");
    public Task<bool> ExistsAsync(string username, string email, Guid? exceptId, CancellationToken ct) => Task.FromResult(username == "existing");
    public Task AddAsync(Account account, CancellationToken ct) { Items.Add(account); return Task.CompletedTask; }
    public Task SaveAsync(CancellationToken ct) { SaveCount++; return Task.CompletedTask; }
    public Task<Account?> GetAsync(Guid id, CancellationToken ct) => throw new NotSupportedException();
    public Task<Account?> FindByUsernameAsync(string name, CancellationToken ct) => throw new NotSupportedException();
    public Task<Account?> FindByEmailAsync(string email, CancellationToken ct) => throw new NotSupportedException();
    public Task<(List<Account> Items, int Total)> ListAsync(AccountQueryRequest request, CancellationToken ct) => throw new NotSupportedException();
    public Task<int> CountActiveAdminsAsync(CancellationToken ct) => throw new NotSupportedException();
    public void Delete(Account account) => throw new NotSupportedException();
}
