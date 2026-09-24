using Base.Models.Requests;
using Base.Models.Responses;

namespace Base.Services.IServices;

public interface INotificationService
{
    Task<PagedResult<NotificationResponse>> ListAsync(Guid userId, NotificationQueryRequest request, CancellationToken cancellationToken);
    Task<int> CountUnreadAsync(Guid userId, CancellationToken cancellationToken);
    Task MarkReadAsync(Guid userId, Guid id, CancellationToken cancellationToken);
    Task MarkAllReadAsync(Guid userId, CancellationToken cancellationToken);
}
