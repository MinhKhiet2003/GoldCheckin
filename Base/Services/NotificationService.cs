using Base.Exceptions;
using Base.Models.Requests;
using Base.Models.Responses;
using Base.Repositories.IRepositories;
using Base.Services.IServices;

namespace Base.Services;

public class NotificationService(INotificationRepository repository) : INotificationService
{
    public async Task<PagedResult<NotificationResponse>> ListAsync(Guid userId, NotificationQueryRequest request, CancellationToken cancellationToken)
    {
        if (request.PageNumber < 1 || request.PageSize is < 1 or > 100)
        {
            throw new ApiException(400, "Thông tin phân trang không hợp lệ.");
        }

        var (items, total) = await repository.ListAsync(userId, request, cancellationToken);

        return new PagedResult<NotificationResponse>
        {
            Items = items.Select(ToResponse),
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            TotalItems = total,
            TotalPages = (int)Math.Ceiling(total / (double)request.PageSize)
        };
    }

    public Task<int> CountUnreadAsync(Guid userId, CancellationToken cancellationToken)
    {
        return repository.CountUnreadAsync(userId, cancellationToken);
    }

    public async Task MarkReadAsync(Guid userId, Guid id, CancellationToken cancellationToken)
    {
        var notification = await repository.GetAsync(id, userId, cancellationToken)
            ?? throw new ApiException(404, "Không tìm thấy thông báo.");

        notification.ReadAt ??= DateTime.UtcNow;
        await repository.SaveAsync(cancellationToken);
    }

    public async Task MarkAllReadAsync(Guid userId, CancellationToken cancellationToken)
    {
        await repository.MarkAllReadAsync(userId, cancellationToken);
    }

    private static NotificationResponse ToResponse(Base.Entities.Notification item)
    {
        return new NotificationResponse
        {
            Id = item.Id,
            Title = item.Title,
            Content = item.Content,
            Link = item.Link,
            ReadAt = item.ReadAt,
            CreatedAt = item.CreatedAt
        };
    }
}
