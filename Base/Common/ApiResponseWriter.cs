using Base.Models.Responses;

namespace Base.Common;

public static class ApiResponseWriter
{
    public static Task WriteErrorAsync(
        HttpContext context,
        int statusCode,
        string? message = null,
        object? errors = null,
        CancellationToken cancellationToken = default)
    {
        context.Response.StatusCode = statusCode;

        return context.Response.WriteAsJsonAsync(new ApiResponse<object>
        {
            StatusCode = statusCode,
            Success = false,
            Message = message ?? GetErrorMessage(statusCode),
            Errors = errors
        }, cancellationToken);
    }

    public static string GetErrorMessage(int statusCode)
    {
        return statusCode switch
        {
            400 => "Dữ liệu yêu cầu không hợp lệ.",
            401 => "Vui lòng đăng nhập. Phiên đăng nhập không hợp lệ hoặc đã hết hạn.",
            403 => "Bạn không có quyền thực hiện thao tác này.",
            404 => "Không tìm thấy tài nguyên được yêu cầu.",
            405 => "Phương thức HTTP không được hỗ trợ cho đường dẫn này.",
            409 => "Dữ liệu đã tồn tại hoặc xung đột với dữ liệu hiện có.",
            413 => "Dữ liệu gửi lên vượt quá kích thước cho phép.",
            415 => "Định dạng dữ liệu không được hỗ trợ. Vui lòng dùng Content-Type phù hợp với API.",
            429 => "Bạn đã gửi quá nhiều yêu cầu. Vui lòng thử lại sau.",
            _ => "Đã xảy ra lỗi khi xử lý yêu cầu. Vui lòng thử lại sau."
        };
    }
}