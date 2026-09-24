using Base.Authorization;
using Base.Models.Requests;
using Base.Models.Responses;
using Base.Services.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Base.Controllers;

[ApiController]
[Route("api/system-settings")]
[Produces("application/json")]
[Authorize(Policy = AppPermissions.ManageSettings)]
public class SystemSettingsController(ISystemSettingService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<SystemSettingResponse>>>> List(CancellationToken cancellationToken)
    {
        var result = await service.ListAsync(cancellationToken);
        return Ok(new ApiResponse<IEnumerable<SystemSettingResponse>>
        {
            StatusCode = 200,
            Success = true,
            Message = "Lấy cấu hình hệ thống thành công.",
            Data = result
        });
    }

    [HttpPut("{key}")]
    public async Task<ActionResult<ApiResponse<SystemSettingResponse>>> Upsert(
        string key,
        SystemSettingRequest request,
        CancellationToken cancellationToken)
    {
        var result = await service.UpsertAsync(key, request, cancellationToken);
        return Ok(new ApiResponse<SystemSettingResponse>
        {
            StatusCode = 200,
            Success = true,
            Message = "Lưu cấu hình hệ thống thành công.",
            Data = result
        });
    }
}
