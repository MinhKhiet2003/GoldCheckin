namespace Base.Models.Responses;

public class UploadFileResponse
{
    public Guid Id { get; set; }
    public string OriginalName { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long Size { get; set; }
    public DateTime CreatedAt { get; set; }
}