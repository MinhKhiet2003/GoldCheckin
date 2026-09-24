namespace Base.Services.IServices;

public interface IEmailService
{
    bool IsConfigured { get; }
    Task SendPasswordResetAsync(string recipientEmail, string recipientName, string resetToken, DateTime expiresAt, CancellationToken cancellationToken);
}
