using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Base.Exceptions;

namespace Base.Gold;

public record FaceResult(bool Verified, Guid EmployeeId, string VerificationId, double Confidence);
public record FaceScanResult(int FaceCount, double Probability, string? Angle);
public interface IFaceVerifier
{
    Task<FaceResult> Verify(Guid employeeId, string imageBase64, CancellationToken ct);
    Task<FaceScanResult> Scan(string imageBase64, CancellationToken ct);
}

// Matches a fresh camera photo against the employee's CompreFace subject. Recognition alone is not liveness detection.
public class FaceVerifier(HttpClient client, IConfiguration config) : IFaceVerifier
{
    public async Task<FaceScanResult> Scan(string imageBase64, CancellationToken ct)
    {
        var endpoint = config["Gold:FaceVerificationUrl"];
        if (string.IsNullOrWhiteSpace(endpoint)) endpoint = "http://localhost:8000/api/v1/recognition/recognize";
        var key = config["Gold:FaceApiKey"];
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps || string.IsNullOrWhiteSpace(key))
            throw new ApiException(503, "Dịch vụ quét khuôn mặt chưa được cấu hình API key.");

        var bytes = FaceEnrollment.DecodeImage(imageBase64);
        // The landmarks plugin is available in all CompreFace configurations and lets us
        // estimate whether the face is centered or turned to either side.
        var scanUri = AddQuery(uri, "limit=2&prediction_count=1&det_prob_threshold=0.8&face_plugins=landmarks");
        using var request = new HttpRequestMessage(HttpMethod.Post, scanUri);
        request.Headers.Add("x-api-key", key);
        request.Content = JsonContent.Create(new { file = Convert.ToBase64String(bytes) });
        HttpResponseMessage response;
        try { response = await client.SendAsync(request, ct); }
        catch (HttpRequestException) { throw new ApiException(503, "Không kết nối được dịch vụ quét khuôn mặt. Kiểm tra CompreFace tại localhost:8000."); }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested) { throw new ApiException(504, "CompreFace xử lý ảnh quá lâu và đã vượt thời gian chờ 60 giây. Kiểm tra Docker Desktop, trạng thái các container và tài nguyên máy chủ rồi thử lại."); }
        using (response)
        {
            if (response.StatusCode is System.Net.HttpStatusCode.Unauthorized or System.Net.HttpStatusCode.Forbidden)
                throw new ApiException(503, "API key CompreFace không hợp lệ hoặc không thuộc Face Recognition service.");
            if (response.StatusCode == System.Net.HttpStatusCode.MethodNotAllowed)
                throw new ApiException(503, "CompreFace từ chối phương thức POST. Hãy kiểm tra Gold:FaceVerificationUrl phải trỏ đến API Recognition /api/v1/recognition/recognize, không phải trang quản trị hoặc URL Swagger.");
            if (!response.IsSuccessStatusCode)
                throw new ApiException(503, $"CompreFace không quét được ảnh (HTTP {(int)response.StatusCode}). Kiểm tra URL Recognition, trạng thái dịch vụ và API key.");
            var result = await response.Content.ReadFromJsonAsync<CompreFaceRecognizeResponse>(ct);
            var faces = result?.Result ?? [];
            if (faces.Length != 1) return new FaceScanResult(faces.Length, 0, null);
            var face = faces[0];
            var probability = face.Box?.Probability ?? 0;
            var angle = EstimateAngle(face.Landmarks);
            return new FaceScanResult(1, probability, probability >= .8 ? angle : null);
        }
    }

    private static string? EstimateAngle(double[][]? landmarks)
    {
        // CompreFace returns 5 points in the order eyes, nose, mouth corners.
        if (landmarks is not { Length: >= 3 } || landmarks[0].Length < 2 || landmarks[1].Length < 2 || landmarks[2].Length < 2)
            return null;
        var eyeDistance = Math.Abs(landmarks[1][0] - landmarks[0][0]);
        if (eyeDistance < 1) return null;
        var eyeMidpoint = (landmarks[0][0] + landmarks[1][0]) / 2d;
        var normalizedNoseOffset = (landmarks[2][0] - eyeMidpoint) / eyeDistance;
        if (normalizedNoseOffset < -.14) return "left";
        if (normalizedNoseOffset > .14) return "right";
        return "center";
    }

    public async Task<FaceResult> Verify(Guid employeeId, string imageBase64, CancellationToken ct)
    {
        var endpoint = config["Gold:FaceVerificationUrl"];
        if (string.IsNullOrWhiteSpace(endpoint)) endpoint = "http://localhost:8000/api/v1/recognition/recognize";
        var key = config["Gold:FaceApiKey"];
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps || string.IsNullOrWhiteSpace(key))
            throw new ApiException(503, "Chưa cấu hình CompreFace. Không thể xác minh khuôn mặt để chấm công.");
        var bytes = FaceEnrollment.DecodeImage(imageBase64);
        using var request = new HttpRequestMessage(HttpMethod.Post, uri);
        request.Headers.Add("x-api-key", key);
        request.Content = JsonContent.Create(new { file = Convert.ToBase64String(bytes) });
        using var response = await client.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode) throw new ApiException(503, "CompreFace chưa sẵn sàng để so khớp khuôn mặt.");
        var result = await response.Content.ReadFromJsonAsync<CompreFaceRecognizeResponse>(ct);
        var expected = employeeId.ToString();
        var similarity = result?.Result?.SelectMany(face => face.Subjects ?? [])
            .Where(subject => string.Equals(subject.Subject, expected, StringComparison.OrdinalIgnoreCase))
            .Select(subject => subject.Similarity).DefaultIfEmpty(0).Max() ?? 0;
        var threshold = config.GetValue("Gold:FaceMatchThreshold", .85);
        if (!double.IsFinite(similarity) || similarity < threshold)
            throw new ApiException(422, "Khuôn mặt không khớp hồ sơ đã đăng ký. Hãy chụp lại rõ mặt, đủ sáng và nhìn thẳng.");
        return new FaceResult(true, employeeId, Guid.NewGuid().ToString("N"), similarity);
    }

    private static Uri AddQuery(Uri uri, string query) => new UriBuilder(uri) { Query = string.IsNullOrEmpty(uri.Query) ? query : uri.Query.TrimStart('?') + "&" + query }.Uri;
    private sealed record CompreFaceRecognizeResponse([property: JsonPropertyName("result")] CompreFaceFace[]? Result);
    private sealed record CompreFaceFace([property: JsonPropertyName("subjects")] CompreFaceSubject[]? Subjects, [property: JsonPropertyName("box")] CompreFaceBox? Box, [property: JsonPropertyName("landmarks")] double[][]? Landmarks);
    private sealed record CompreFaceBox([property: JsonPropertyName("probability")] double Probability);
    private sealed record CompreFaceSubject([property: JsonPropertyName("subject")] string Subject, [property: JsonPropertyName("similarity")] double Similarity);
}
