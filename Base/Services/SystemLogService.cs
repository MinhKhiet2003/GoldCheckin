using System.Text.Json;
using Base.Entities;
using Base.Exceptions;
using Base.Models.Requests;
using Base.Models.Responses;
using Base.Repositories.IRepositories;
using Base.Services.IServices;

namespace Base.Services;

public class SystemLogService(ISystemLogRepository repository) : ISystemLogService
{
    public async Task<SystemLogDetailResponse> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var log = await repository.GetAsync(id, cancellationToken)
            ?? throw new ApiException(404, "Không tìm thấy nhật ký hệ thống.");

        return new SystemLogDetailResponse
        {
            Id = log.Id,
            UserId = log.UserId,
            Action = log.Action,
            EntityType = log.EntityType,
            EntityId = log.EntityId,
            Method = log.Method,
            Path = log.Path,
            StatusCode = log.StatusCode,
            Success = log.Success,
            Message = log.Message,
            IpAddress = log.IpAddress,
            TraceId = log.TraceId,
            DurationMs = log.DurationMs,
            CreatedAt = log.CreatedAt,
            ResponseData = Parse(log.ResponseData),
            BeforeData = Parse(log.BeforeData),
            AfterData = Parse(log.AfterData)
        };
    }

    private static JsonElement? Parse(string? json)
    {
        return json is null ? null : JsonSerializer.Deserialize<JsonElement>(json);
    }
    public Task WriteAsync(SystemLog log, CancellationToken cancellationToken)
    {
        return repository.AddAsync(log, cancellationToken);
    }

    public async Task<PagedResult<SystemLogResponse>> ListAsync(SystemLogQueryRequest request, CancellationToken cancellationToken)
    {
        if (request.PageSize < 1 || request.PageSize > 100 || request.PageNumber < 1 || request.PageNumber > int.MaxValue / request.PageSize)
        {
            throw new ApiException(400, "Thông tin phân trang không hợp lệ.");
        }

        var (items, total) = await repository.ListAsync(request, cancellationToken);

        return new PagedResult<SystemLogResponse>
        {
            Items = items.Select(item => new SystemLogResponse
            {
                Id = item.Id,
                Action = item.Action,
                EntityType = item.EntityType,
                EntityId = item.EntityId,
                UserId = item.UserId,
                Method = item.Method,
                Path = item.Path,
                StatusCode = item.StatusCode,
                Success = item.Success,
                Message = item.Message,
                IpAddress = item.IpAddress,
                TraceId = item.TraceId,
                DurationMs = item.DurationMs,
                CreatedAt = item.CreatedAt
            }),
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            TotalItems = total,
            TotalPages = (int)Math.Ceiling(total / (double)request.PageSize)
        };
    }
}