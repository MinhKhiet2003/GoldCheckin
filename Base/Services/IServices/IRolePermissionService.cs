using Base.Models.Responses;

namespace Base.Services.IServices;

public interface IRolePermissionService
{
    Task<IEnumerable<PermissionResponse>> ListPermissionsAsync(CancellationToken cancellationToken);
    Task<IEnumerable<RoleResponse>> ListRolesAsync(CancellationToken cancellationToken);
    Task<RoleResponse> UpdateUserRolePermissionsAsync(IEnumerable<string> permissionCodes, CancellationToken cancellationToken);
}
