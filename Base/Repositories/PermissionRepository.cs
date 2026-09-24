using Base.Data;
using Base.Entities;
using Base.Repositories.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace Base.Repositories;

public class PermissionRepository(AppDbContext dbContext) : IPermissionRepository
{
    public Task<List<string>> GetByRoleAsync(string role, CancellationToken cancellationToken)
    {
        return dbContext.RolePermissions
            .AsNoTracking()
            .Where(item => item.Role == role)
            .Select(item => item.PermissionCode)
            .ToListAsync(cancellationToken);
    }

    public Task<List<Permission>> ListAsync(CancellationToken cancellationToken)
    {
        return dbContext.Permissions.AsNoTracking().OrderBy(item => item.Code).ToListAsync(cancellationToken);
    }

    public async Task ReplaceForRoleAsync(string role, IReadOnlyCollection<string> permissionCodes, CancellationToken cancellationToken)
    {
        await dbContext.RolePermissions
            .Where(item => item.Role == role)
            .ExecuteDeleteAsync(cancellationToken);

        var grants = permissionCodes.Select(code => new RolePermission
        {
            Role = role,
            PermissionCode = code
        });
        await dbContext.RolePermissions.AddRangeAsync(grants, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
