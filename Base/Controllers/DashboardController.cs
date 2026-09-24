using Base.Authorization;
using Base.Models.Responses;
using Base.Services.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http.Metadata;

namespace Base.Controllers;

[ApiController]
[Route("api/dashboard")]
[Produces("application/json")]
[Authorize(Policy = AppPermissions.ReadDashboard)]
public class DashboardController(IDashboardService service) : ControllerBase
{
    [HttpGet]
    [EndpointSummary("Lấy dữ liệu tổng quan dashboard")]
    public async Task<ActionResult<ApiResponse<DashboardResponse>>> Get(CancellationToken cancellationToken)
    {
        var result = await service.GetAsync(cancellationToken);
        return Ok(new ApiResponse<DashboardResponse>
        {
            StatusCode = StatusCodes.Status200OK,
            Success = true,
            Message = "Lấy dữ liệu dashboard thành công.",
            Data = result
        });
    }
}
