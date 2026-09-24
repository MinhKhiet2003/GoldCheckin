using Base.Models.Requests;
using Base.Models.Responses;

namespace Base.Services.IServices;

public interface ISystemSettingService
{
    Task<IEnumerable<SystemSettingResponse>> ListAsync(CancellationToken cancellationToken);
    Task<SystemSettingResponse> UpsertAsync(string key, SystemSettingRequest request, CancellationToken cancellationToken);
}
