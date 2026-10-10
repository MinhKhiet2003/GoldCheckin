using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Base.Exceptions;

namespace Base.Gold;

public record FaceEnrollmentResult(bool Enrolled, Guid EmployeeId, Guid RegisteredBy,
    string EnrollmentId, DateTime VerifiedAt);

public interface IFaceEnrollment
{
    Task<FaceEnrollmentResult> Register(Guid adminId, Guid employeeId, string imageBase64, CancellationToken ct);
}

// Stores a face sample in the self-hosted CompreFace recognition collection.
public class FaceEnrollment(HttpClient client, IConfiguration config) : IFaceEnrollment
{
    public async Task<FaceEnrollmentResult> Register(Guid adminId, Guid employeeId, string imageBase64, CancellationToken ct)
    {
        var endpoint = config["Gold:FaceEnrollmentUrl"];
        if (string.IsNullOrWhiteSpace(endpoint)) endpoint = "http://localhost:8000/api/v1/recognition/faces";
        var key = config["Gold:FaceApiKey"];
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var url) || url.Scheme != Uri.UriSchemeHttp && url.Scheme != Uri.UriSchemeHttps || string.IsNullOrWhiteSpace(key))
            throw new ApiException(503, "Chưa cấu hình CompreFace. Hãy khởi động dịch vụ và nhập URL/API key.");

        var bytes = DecodeImage(imageBase64);
        using var request = new HttpRequestMessage(HttpMethod.Post, AddQuery(url, $"subject={Uri.EscapeDataString(employeeId.ToString())}&det_prob_threshold=0.8"));
        request.Headers.Add("x-api-key", key);
        request.Content = JsonContent.Create(new { file = Convert.ToBase64String(bytes) });
        using var response = await client.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode) throw new ApiException(503, "CompreFace không lưu được ảnh. Kiểm tra một khuôn mặt rõ trong ảnh và API key.");
        var result = await response.Content.ReadFromJsonAsync<CompreFaceEnrollResponse>(ct);
        if (result is null || !Guid.TryParse(result.Subject, out var subject) || subject != employeeId || string.IsNullOrWhiteSpace(result.ImageId))
            throw new ApiException(422, "Ảnh đăng ký không hợp lệ hoặc không tìm thấy duy nhất một khuôn mặt.");
        return new FaceEnrollmentResult(true, employeeId, adminId, result.ImageId, DateTime.UtcNow);
    }

    internal static byte[] DecodeImage(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ApiException(400, "Chụp ảnh khuôn mặt trước khi tiếp tục.");
        var comma = value.IndexOf(',');
        if (value.StartsWith("data:", StringComparison.OrdinalIgnoreCase) && comma >= 0) value = value[(comma + 1)..];
        try
        {
            var bytes = Convert.FromBase64String(value);
            if (bytes.Length is < 1000 or > 5 * 1024 * 1024) throw new ApiException(400, "Ảnh cần có dung lượng từ 1 KB đến 5 MB.");
            return bytes;
        }
        catch (FormatException) { throw new ApiException(400, "Dữ liệu ảnh không hợp lệ."); }
    }

    private static Uri AddQuery(Uri uri, string query) => new UriBuilder(uri) { Query = string.IsNullOrEmpty(uri.Query) ? query : uri.Query.TrimStart('?') + "&" + query }.Uri;
    private sealed record CompreFaceEnrollResponse([property: JsonPropertyName("image_id")] string ImageId, [property: JsonPropertyName("subject")] string Subject);
}
