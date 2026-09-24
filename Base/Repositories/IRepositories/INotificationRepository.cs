using Base.Entities;
using Base.Models.Requests;

namespace Base.Repositories.IRepositories;

public interface INotificationRepository
{
    Task<(List<Notification> Items, int Total)> ListAsync(Guid userId, NotificationQueryRequest request, CancellationToken cancellationToken);
    Task<int> CountUnreadAsync(Guid userId, CancellationToken cancellationToken);
    Task<Notification?> GetAsync(Guid id, Guid userId, CancellationToken cancellationToken);
    Task MarkAllReadAsync(Guid userId, CancellationToken cancellationToken);
    Task SaveAsync(CancellationToken cancellationToken);
}
