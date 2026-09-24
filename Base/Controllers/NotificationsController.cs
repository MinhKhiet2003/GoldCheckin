using Base.Authorization;
using Base.Models.Requests;
using Base.Models.Responses;
using Base.Services.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Base.Controllers;

[ApiController]
[Route("api/notifications")]
[Produces("application/json")]
[Authorize(Policy = AppPermissions.ReadNotifications)]
public class NotificationsController(INotificationService service) : ControllerBase
{
    private Guid CurrentUserId => Guid.Parse(User.FindFirst("sub")!.Value);

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<NotificationResponse>>>> List(
        [FromQuery] NotificationQueryRequest request,
        CancellationToken cancellationToken)
    {
        var result = await service.ListAsync(CurrentUserId, request, cancellationToken);
        return Ok(new ApiResponse<PagedResult<NotificationResponse>>
        {
            StatusCode = 200,
            Success = true,
            Message = "Lấy danh sách thông báo thành công.",
            Data = result
        });
    }

    [HttpGet("unread-count")]
    public async Task<ActionResult<ApiResponse<object>>> UnreadCount(CancellationToken cancellationToken)
    {
        var count = await service.CountUnreadAsync(CurrentUserId, cancellationToken);
        return Ok(new ApiResponse<object>
        {
            StatusCode = 200,
            Success = true,
            Message = "Lấy số thông báo chưa đọc thành công.",
            Data = new { count }
        });
    }

    [Authorize(Policy = AppPermissions.UpdateNotifications)]
    [HttpPut("{id:guid}/read")]
    public async Task<ActionResult<ApiResponse<object>>> MarkRead(Guid id, CancellationToken cancellationToken)
    {
        await service.MarkReadAsync(CurrentUserId, id, cancellationToken);
        return Ok(new ApiResponse<object> { StatusCode = 200, Success = true, Message = "Đã đánh dấu thông báo là đã đọc." });
    }

    [Authorize(Policy = AppPermissions.UpdateNotifications)]
    [HttpPut("read-all")]
    public async Task<ActionResult<ApiResponse<object>>> MarkAllRead(CancellationToken cancellationToken)
    {
        await service.MarkAllReadAsync(CurrentUserId, cancellationToken);
        return Ok(new ApiResponse<object> { StatusCode = 200, Success = true, Message = "Đã đánh dấu tất cả thông báo là đã đọc." });
    }
}
