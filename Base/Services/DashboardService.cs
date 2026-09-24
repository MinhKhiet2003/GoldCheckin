using Base.Models.Responses;
using Base.Repositories.IRepositories;
using Base.Services.IServices;

namespace Base.Services;

public class DashboardService(IDashboardRepository repository) : IDashboardService
{
    public async Task<DashboardResponse> GetAsync(CancellationToken cancellationToken)
    {
        var data = await repository.GetAsync(cancellationToken);
        return new DashboardResponse
        {
            TotalUsers = data.Total,
            ActiveUsers = data.Active,
            LockedUsers = data.Locked,
            LoginsLast7Days = data.LoginCount,
            RecentLogins = data.Recent.Select(UserResponse.From)
        };
    }
}
