using Base.Entities;
using Base.Models.Requests;
using Base.Models.Responses;

namespace Base.Services.IServices;

public interface ISystemLogService
{
    Task<SystemLogDetailResponse> GetAsync(Guid id, CancellationToken cancellationToken);
    Task WriteAsync(SystemLog log, CancellationToken cancellationToken);
    Task<PagedResult<SystemLogResponse>> ListAsync(SystemLogQueryRequest request, CancellationToken cancellationToken);
}