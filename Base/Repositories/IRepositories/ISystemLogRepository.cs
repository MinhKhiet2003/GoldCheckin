using Base.Entities;
using Base.Models.Requests;

namespace Base.Repositories.IRepositories;

public interface ISystemLogRepository
{
    Task<SystemLog?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task AddAsync(SystemLog log, CancellationToken cancellationToken);
    Task<(List<SystemLog> Items, int Total)> ListAsync(SystemLogQueryRequest request, CancellationToken cancellationToken);
}