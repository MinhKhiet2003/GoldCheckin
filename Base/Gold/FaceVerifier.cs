using System.Net.Http.Json;
using Base.Exceptions;

namespace Base.Gold;

public record FaceResult(bool Verified, bool Live, Guid EmployeeId, string VerificationId,
    double Confidence, DateTime VerifiedAt);
public interface IFaceVerifier
{
    Task<FaceResult> Verify(Guid employeeId, string token, CancellationToken ct);
}
// The provider validates a short-lived session issued by its own enrollment/liveness SDK.
// No client-provided confidence score or photo is accepted as proof of identity.
public class FaceVerifier(HttpClient client, IConfiguration config) : IFaceVerifier
{
    public async Task<FaceResult> Verify(Guid employeeId, string token, CancellationToken ct)
    {
        var url = config["Gold:FaceVerificationUrl"];
        var key = config["Gold:FaceApiKey"];
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme != "https" || string.IsNullOrWhiteSpace(key))
            throw new ApiException(503, "Chưa cấu hình dịch vụ nhận diện khuôn mặt và liveness. Không thể ghi nhận chấm công.");
        using var request = new HttpRequestMessage(HttpMethod.Post, uri);
        request.Headers.Add("X-Api-Key", key);
        request.Content = JsonContent.Create(new { employeeId, sessionToken = token });
        using var response = await client.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode) throw new ApiException(503, "Dịch vụ xác thực khuôn mặt chưa sẵn sàng.");
        var result = await response.Content.ReadFromJsonAsync<FaceResult>(ct);
        var age = result is null ? TimeSpan.MaxValue : DateTime.UtcNow - result.VerifiedAt;
        if (result is null || !result.Verified || !result.Live || result.EmployeeId != employeeId
            || string.IsNullOrWhiteSpace(result.VerificationId) || result.VerificationId.Length > 100
            || !double.IsFinite(result.Confidence) || result.Confidence < .9 || result.Confidence > 1
            || result.VerifiedAt.Kind != DateTimeKind.Utc || age.TotalSeconds < -5 || age.TotalSeconds > 90)
            throw new ApiException(422, "Khuôn mặt hoặc kiểm tra sống không đạt. Vui lòng xác thực lại.");
        return result;
    }
}
