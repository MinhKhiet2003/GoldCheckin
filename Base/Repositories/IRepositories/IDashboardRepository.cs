using Base.Entities;

namespace Base.Repositories.IRepositories;

public interface IDashboardRepository
{
    Task<(int Total, int Active, int Locked, int LoginCount, List<Account> Recent)> GetAsync(CancellationToken cancellationToken);
}
