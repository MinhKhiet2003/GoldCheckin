using Base.Data;
using Base.Entities;
using Base.Repositories.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace Base.Repositories;

public class SystemSettingRepository(AppDbContext dbContext) : ISystemSettingRepository
{
    public Task<List<SystemSetting>> ListAsync(CancellationToken cancellationToken)
    {
        return dbContext.SystemSettings.AsNoTracking().OrderBy(item => item.Key).ToListAsync(cancellationToken);
    }

    public Task<SystemSetting?> GetAsync(string key, CancellationToken cancellationToken)
    {
        return dbContext.SystemSettings.SingleOrDefaultAsync(item => item.Key == key, cancellationToken);
    }

    public Task AddAsync(SystemSetting setting, CancellationToken cancellationToken)
    {
        return dbContext.SystemSettings.AddAsync(setting, cancellationToken).AsTask();
    }

    public Task SaveAsync(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
