using Base.Data;
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
}