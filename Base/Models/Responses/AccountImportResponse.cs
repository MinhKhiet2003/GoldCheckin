namespace Base.Models.Responses;

public class AccountImportResponse
{
    public int TotalRows { get; set; }
    public int ImportedCount { get; set; }
    public int DuplicateCount => Duplicates.Count;
    public int InvalidCount => Errors.Count;
    public int FailedCount => DuplicateCount + InvalidCount;
    public string? DuplicateDownloadUrl { get; set; }
    public List<AccountImportDuplicate> Duplicates { get; set; } = [];
    public List<AccountImportRowError> Errors { get; set; } = [];
}

public record AccountImportRowError(int Row, string Message);

public record AccountImportDuplicate(int Row, string Username, string FullName, string Email, string? PhoneNumber, string Reason);