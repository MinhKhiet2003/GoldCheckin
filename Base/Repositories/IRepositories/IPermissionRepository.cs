using Base.Entities;

namespace Base.Repositories.IRepositories;

public interface IPermissionRepository
{
    Task<List<string>> GetByRoleAsync(string role, CancellationToken cancellationToken);
    Task<List<Permission>> ListAsync(CancellationToken cancellationToken);
    Task ReplaceForRoleAsync(string role, IReadOnlyCollection<string> permissionCodes, CancellationToken cancellationToken);
}
