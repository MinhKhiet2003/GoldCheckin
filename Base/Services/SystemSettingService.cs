using Base.Entities;
using Base.Exceptions;
using Base.Models.Requests;
using Base.Models.Responses;
using Base.Repositories.IRepositories;
using Base.Services.IServices;

namespace Base.Services;

public class SystemSettingService(ISystemSettingRepository repository) : ISystemSettingService
{
    public async Task<IEnumerable<SystemSettingResponse>> ListAsync(CancellationToken cancellationToken)
    {
        var settings = await repository.ListAsync(cancellationToken);
        return settings.Select(ToResponse);
    }

    public async Task<SystemSettingResponse> UpsertAsync(string key, SystemSettingRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(key) || key.Length > 100 || !key.All(item => char.IsLetterOrDigit(item) || item is '.' or '_' or '-'))
        {
            throw new ApiException(400, "Khóa cấu hình chỉ gồm chữ, số, dấu chấm, gạch dưới hoặc gạch ngang và không quá 100 ký tự.");
        }

        var setting = await repository.GetAsync(key, cancellationToken);
        if (setting is null)
        {
            setting = new SystemSetting { Key = key };
            await repository.AddAsync(setting, cancellationToken);
        }

        setting.Value = request.Value;
        setting.Description = request.Description;
        setting.UpdatedAt = DateTime.UtcNow;
        await repository.SaveAsync(cancellationToken);
        return ToResponse(setting);
    }

    private static SystemSettingResponse ToResponse(SystemSetting item)
    {
        return new SystemSettingResponse { Key = item.Key, Value = item.Value, Description = item.Description, UpdatedAt = item.UpdatedAt };
    }
}
