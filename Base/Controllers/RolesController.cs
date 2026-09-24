using Base.Authorization;
using Base.Models.Requests;
using Base.Models.Responses;
using Base.Services.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Base.Controllers;

[ApiController]
[Route("api")]
[Produces("application/json")]
[Authorize(Roles = AppRoles.Admin)]
public class RolesController(IRolePermissionService service) : ControllerBase
{
    [Authorize(Policy = AppPermissions.ManagePermissions)]
    [HttpGet("permissions")]
    public async Task<ActionResult<ApiResponse<IEnumerable<PermissionResponse>>>> ListPermissions(CancellationToken cancellationToken)
    {
        var result = await service.ListPermissionsAsync(cancellationToken);
        return Ok(new ApiResponse<IEnumerable<PermissionResponse>> { StatusCode = 200, Success = true, Message = "Lấy danh sách quyền thành công.", Data = result });
    }

    [Authorize(Policy = AppPermissions.ManageRoles)]
    [HttpGet("roles")]
    public async Task<ActionResult<ApiResponse<IEnumerable<RoleResponse>>>> ListRoles(CancellationToken cancellationToken)
    {
        var result = await service.ListRolesAsync(cancellationToken);
        return Ok(new ApiResponse<IEnumerable<RoleResponse>> { StatusCode = 200, Success = true, Message = "Lấy danh sách vai trò thành công.", Data = result });
    }

    [Authorize(Policy = AppPermissions.ManageRoles)]
    [HttpPut("roles/user/permissions")]
    public async Task<ActionResult<ApiResponse<RoleResponse>>> UpdateUserPermissions(RolePermissionRequest request, CancellationToken cancellationToken)
    {
        var result = await service.UpdateUserRolePermissionsAsync(request.PermissionCodes, cancellationToken);
        return Ok(new ApiResponse<RoleResponse> { StatusCode = 200, Success = true, Message = "Cập nhật quyền cho role user thành công.", Data = result });
    }
}
