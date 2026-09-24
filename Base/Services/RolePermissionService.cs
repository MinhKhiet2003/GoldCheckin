using Base.Authorization;
using Base.Exceptions;
using Base.Models.Responses;
using Base.Repositories.IRepositories;
using Base.Services.IServices;

namespace Base.Services;

public class RolePermissionService(IPermissionRepository repository) : IRolePermissionService
{
    public async Task<IEnumerable<PermissionResponse>> ListPermissionsAsync(CancellationToken cancellationToken)
    {
        var permissions = await repository.ListAsync(cancellationToken);
        return permissions.Select(item => new PermissionResponse { Code = item.Code, Name = item.Name });
    }

    public async Task<IEnumerable<RoleResponse>> ListRolesAsync(CancellationToken cancellationToken)
    {
        var userPermissions = await repository.GetByRoleAsync(AppRoles.User, cancellationToken);
        return
        [
            new RoleResponse { Code = AppRoles.Admin, Name = "Quản trị viên", PermissionCodes = AppPermissions.All },
            new RoleResponse { Code = AppRoles.User, Name = "Người dùng", PermissionCodes = userPermissions.Order() }
        ];
    }

    public async Task<RoleResponse> UpdateUserRolePermissionsAsync(IEnumerable<string> permissionCodes, CancellationToken cancellationToken)
    {
        var codes = permissionCodes
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Select(code => code.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        var validCodes = await repository.ListAsync(cancellationToken);
        var knownCodes = validCodes.Select(item => item.Code).ToHashSet(StringComparer.Ordinal);
        var invalidCodes = codes.Where(code => !knownCodes.Contains(code)).ToArray();
        if (invalidCodes.Length > 0)
        {
            throw new ApiException(400, "Danh sách quyền có mã không tồn tại.");
        }

        await repository.ReplaceForRoleAsync(AppRoles.User, codes, cancellationToken);
        return new RoleResponse { Code = AppRoles.User, Name = "Người dùng", PermissionCodes = codes.Order() };
    }
}
