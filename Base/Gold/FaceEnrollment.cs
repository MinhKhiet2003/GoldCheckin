using System.Net.Http.Json;
using Base.Exceptions;

namespace Base.Gold;

public record FaceEnrollmentResult(bool Enrolled, bool Live, Guid EmployeeId, Guid RegisteredBy,
    string EnrollmentId, DateTime VerifiedAt);

public interface IFaceEnrollment
{
    Task<FaceEnrollmentResult> Register(Guid adminId, Guid employeeId, string sessionToken, CancellationToken ct);
}

// Enrollment uses an admin-authorized provider session, separate from the employee verification flow.
public class FaceEnrollment(HttpClient client, IConfiguration config) : IFaceEnrollment
{
    public async Task<FaceEnrollmentResult> Register(Guid adminId, Guid employeeId, string sessionToken, CancellationToken ct)
    {
        var key = config["Gold:FaceApiKey"];
        if (!Uri.TryCreate(config["Gold:FaceEnrollmentUrl"], UriKind.Absolute, out var url)
            || url.Scheme != "https" || string.IsNullOrWhiteSpace(key))
            throw new ApiException(503, "Chưa kết nối dịch vụ đăng ký khuôn mặt. Vui lòng liên hệ quản trị hệ thống.");

        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Add("X-Api-Key", key);
        request.Content = JsonContent.Create(new { employeeId, registeredBy = adminId, sessionToken });
        using var response = await client.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
            throw new ApiException(503, "Dịch vụ đăng ký khuôn mặt chưa sẵn sàng. Vui lòng thử lại sau.");

        var result = await response.Content.ReadFromJsonAsync<FaceEnrollmentResult>(ct);
        var age = result is null ? TimeSpan.MaxValue : DateTime.UtcNow - result.VerifiedAt;
        if (result is null || !result.Enrolled || !result.Live || result.EmployeeId != employeeId
            || result.RegisteredBy != adminId || string.IsNullOrWhiteSpace(result.EnrollmentId)
            || result.EnrollmentId.Length > 100 || result.VerifiedAt.Kind != DateTimeKind.Utc
            || age.TotalSeconds < -5 || age.TotalSeconds > 90)
            throw new ApiException(422, "Đăng ký khuôn mặt không hợp lệ. Admin cần thực hiện xác thực lại cho nhân viên.");
        return result;
    }
}
