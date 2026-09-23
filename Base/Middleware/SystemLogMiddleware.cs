using System.Diagnostics;
using Base.Common;
using Base.Entities;
using Base.Services.IServices;

namespace Base.Middleware;

public class SystemLogMiddleware(RequestDelegate next, IServiceScopeFactory scopeFactory, ILogger<SystemLogMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context, AuditContext audit)
    {
        if (!context.Request.Path.StartsWithSegments("/api"))
        {
            await next(context);
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        var failed = false;

        try
        {
            await next(context);
        }
        catch
        {
            failed = true;
            throw;
        }
        finally
        {
            stopwatch.Stop();
            var status = failed ? 500 : context.Response.StatusCode;
            Guid? userId = null;

            if (context.User.Identity?.IsAuthenticated == true && Guid.TryParse(context.User.FindFirst("sub")?.Value, out var id))
            {
                userId = id;
            }
            else if (context.Items["AuthenticatedUserId"] is Guid loginId)
            {
                userId = loginId;
            }

            var log = new SystemLog
            {
                UserId = userId,
                Action = audit.Action,
                EntityType = audit.EntityType,
                EntityId = audit.EntityId,
                ResponseData = status is >= 200 and < 300 ? audit.ResponseData : null,
                BeforeData = status is >= 200 and < 300 ? audit.BeforeData : null,
                AfterData = status is >= 200 and < 300 ? audit.AfterData : null,
                Method = Limit(context.Request.Method, 16),
                Path = Limit(context.Request.Path.Value ?? string.Empty, 2048),
                StatusCode = status,
                Success = status < 400,
                Message = status < 400 ? "Xử lý yêu cầu thành công." : ApiResponseWriter.GetErrorMessage(status),
                IpAddress = context.Connection.RemoteIpAddress?.ToString(),
                TraceId = Limit(context.TraceIdentifier, 128),
                DurationMs = stopwatch.ElapsedMilliseconds
            };

            try
            {
                // Scope riêng để không lưu lại các entity đang lỗi trong request gốc.
                await using var scope = scopeFactory.CreateAsyncScope();
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                var service = scope.ServiceProvider.GetRequiredService<ISystemLogService>();
                await service.WriteAsync(log, timeout.Token);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Không thể lưu nhật ký hệ thống. Mã truy vết: {TraceId}", log.TraceId);
            }
        }
    }

    private static string Limit(string value, int length)
    {
        return value.Length <= length ? value : value[..length];
    }
}