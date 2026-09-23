namespace Base.Repositories.IRepositories;

public interface IPermissionRepository
{
    Task<List<string>> GetByRoleAsync(string role, CancellationToken cancellationToken);
}