using Base.Data;
using Base.Entities;
using Base.Repositories.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace Base.Repositories;

public class DashboardRepository(AppDbContext dbContext) : IDashboardRepository
{
    public async Task<(int Total, int Active, int Locked, int LoginCount, List<Account> Recent)> GetAsync(CancellationToken cancellationToken)
    {
        var since = DateTime.UtcNow.AddDays(-7);
        var total = await dbContext.Accounts.CountAsync(cancellationToken);
        var active = await dbContext.Accounts.CountAsync(item => item.IsActive, cancellationToken);
        var loginCount = await dbContext.SystemLogs.CountAsync(
            item => item.Action == "Login" && item.Success && item.CreatedAt >= since,
            cancellationToken);
        var recent = await dbContext.Accounts.AsNoTracking()
            .Where(item => item.LastLoginAt != null)
            .OrderByDescending(item => item.LastLoginAt)
            .Take(10)
            .ToListAsync(cancellationToken);

        return (total, active, total - active, loginCount, recent);
    }
}
