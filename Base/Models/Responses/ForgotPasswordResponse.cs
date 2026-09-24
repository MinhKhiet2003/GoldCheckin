namespace Base.Models.Responses;

public class ForgotPasswordResponse
{
    public string? ResetToken { get; set; }
    public DateTime? ExpiresAt { get; set; }
}
