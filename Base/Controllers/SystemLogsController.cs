using Base.Authorization;
using Base.Models.Requests;
using Base.Models.Responses;
using Base.Services.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http.Metadata;

namespace Base.Controllers;

[ApiController]
[Route("api/system-logs")]
[Produces("application/json")]
[Authorize(Roles = AppRoles.Admin)]
[Authorize(Policy = AppPermissions.ReadSystemLogs)]
public class SystemLogsController(ISystemLogService service) : ControllerBase
{
    [HttpGet("{id:guid}")]
    [EndpointSummary("Xem chi tiết nhật ký hệ thống")]
    public async Task<ActionResult<ApiResponse<SystemLogDetailResponse>>> Get(Guid id, CancellationToken cancellationToken)
    {
        var result = await service.GetAsync(id, cancellationToken);

        return Ok(new ApiResponse<SystemLogDetailResponse>
        {
            StatusCode = StatusCodes.Status200OK,
            Success = true,
            Message = "Lấy chi tiết nhật ký hệ thống thành công.",
            Data = result
        });
    }
    [HttpGet]
    [EndpointSummary("Lấy danh sách nhật ký hệ thống")]
    public async Task<ActionResult<ApiResponse<PagedResult<SystemLogResponse>>>> List(
        [FromQuery] SystemLogQueryRequest request,
        CancellationToken cancellationToken)
    {
        var result = await service.ListAsync(request, cancellationToken);

        return Ok(new ApiResponse<PagedResult<SystemLogResponse>>
        {
            StatusCode = StatusCodes.Status200OK,
            Success = true,
            Message = "Lấy nhật ký hệ thống thành công.",
            Data = result
        });
    }
}
