using Base.Models.Responses;

namespace Base.Services.IServices;

public interface IDashboardService
{
    Task<DashboardResponse> GetAsync(CancellationToken cancellationToken);
}
