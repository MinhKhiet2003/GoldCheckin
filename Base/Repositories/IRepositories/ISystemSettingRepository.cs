using Base.Entities;

namespace Base.Repositories.IRepositories;

public interface ISystemSettingRepository
{
    Task<List<SystemSetting>> ListAsync(CancellationToken cancellationToken);
    Task<SystemSetting?> GetAsync(string key, CancellationToken cancellationToken);
    Task AddAsync(SystemSetting setting, CancellationToken cancellationToken);
    Task SaveAsync(CancellationToken cancellationToken);
}
