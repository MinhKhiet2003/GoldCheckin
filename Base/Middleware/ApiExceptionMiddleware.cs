using Base.Common;
using Base.Exceptions;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Base.Middleware;

// Catch in application code so expected business errors are handled before
// crossing into framework exception handling under Visual Studio Just My Code.
public class ApiExceptionMiddleware(RequestDelegate next, ILogger<ApiExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            if (!context.Response.HasStarted) context.Response.StatusCode = 499;
        }
        catch (Exception error) when (!context.Response.HasStarted)
        {
            var statusCode = StatusCodes.Status500InternalServerError;
            string? message = null;

            switch (error)
            {
                case ApiException apiException:
                    statusCode = apiException.StatusCode;
                    message = apiException.Message;
                    break;
                case BadHttpRequestException badRequest:
                    statusCode = badRequest.StatusCode;
                    break;
                case DbUpdateConcurrencyException:
                case PostgresException { SqlState: PostgresErrorCodes.SerializationFailure }:
                case DbUpdateException { InnerException: PostgresException { SqlState: PostgresErrorCodes.SerializationFailure } }:
                    statusCode = StatusCodes.Status409Conflict;
                    message = "Dữ liệu vừa thay đổi bởi thao tác khác. Vui lòng tải lại và thử lại.";
                    break;
                case DbUpdateException { InnerException: PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } }:
                    statusCode = StatusCodes.Status409Conflict;
                    message = context.Request.Path.StartsWithSegments("/api/gold")
                        ? "Dữ liệu đã tồn tại hoặc vừa được cập nhật. Vui lòng tải lại và kiểm tra."
                        : "Tên đăng nhập hoặc email đã được sử dụng.";
                    break;
                default:
                    logger.LogError(error, "Lỗi xử lý yêu cầu {Path}. Mã truy vết: {TraceId}",
                        context.Request.Path, context.TraceIdentifier);
                    break;
            }

            context.Response.Clear();
            await ApiResponseWriter.WriteErrorAsync(context, statusCode, message,
                cancellationToken: context.RequestAborted);
        }
    } 
}
