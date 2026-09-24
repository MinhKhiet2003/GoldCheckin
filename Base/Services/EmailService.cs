using System.Net;
using System.Net.Mail;
using System.Text.Encodings.Web;
using Base.Configuration;
using Base.Exceptions;
using Base.Services.IServices;
using Microsoft.Extensions.Options;

namespace Base.Services;

public class EmailService(IOptions<MailSettings> options) : IEmailService
{
    private readonly MailSettings settings = options.Value;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(settings.Host)
        && !string.IsNullOrWhiteSpace(settings.Username)
        && !string.IsNullOrWhiteSpace(settings.Password)
        && !string.IsNullOrWhiteSpace(settings.FromEmail);

    public async Task SendPasswordResetAsync(
        string recipientEmail,
        string recipientName,
        string resetToken,
        DateTime expiresAt,
        CancellationToken cancellationToken)
    {
        if (!IsConfigured)
        {
            throw new ApiException(500, "Chưa cấu hình dịch vụ gửi email. Vui lòng kiểm tra mục Mail trong cấu hình hệ thống.");
        }

        var resetUrl = BuildResetUrl(resetToken);
        var encodedName = HtmlEncoder.Default.Encode(recipientName);
        var encodedToken = HtmlEncoder.Default.Encode(resetToken);
        var encodedUrl = HtmlEncoder.Default.Encode(resetUrl);
        var expiresText = expiresAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm");
        var body = $"""
            <p>Xin chào {encodedName},</p>
            <p>Bạn đã yêu cầu đặt lại mật khẩu. Yêu cầu có hiệu lực đến <strong>{expiresText}</strong>.</p>
            <p><a href=\"{encodedUrl}\">Đặt lại mật khẩu</a></p>
            <p>Nếu ứng dụng chưa có trang đặt lại mật khẩu, hãy dùng token sau:</p>
            <p><code>{encodedToken}</code></p>
            <p>Nếu bạn không gửi yêu cầu này, vui lòng bỏ qua email.</p>
            """;

        using var message = new MailMessage
        {
            From = new MailAddress(settings.FromEmail, settings.FromName),
            Subject = "Đặt lại mật khẩu",
            Body = body,
            IsBodyHtml = true
        };
        message.To.Add(recipientEmail);

        using var client = new SmtpClient(settings.Host, settings.Port)
        {
            EnableSsl = settings.UseSsl,
            Credentials = new NetworkCredential(settings.Username, settings.Password)
        };

        try
        {
            await client.SendMailAsync(message, cancellationToken);
        }
        catch (SmtpException)
        {
            throw new ApiException(500, "Không thể gửi email đặt lại mật khẩu. Vui lòng kiểm tra cấu hình SMTP.");
        }
    }

    private string BuildResetUrl(string token)
    {
        if (string.IsNullOrWhiteSpace(settings.PasswordResetUrl))
        {
            return token;
        }

        return settings.PasswordResetUrl.Replace("{token}", Uri.EscapeDataString(token), StringComparison.Ordinal);
    }
}
