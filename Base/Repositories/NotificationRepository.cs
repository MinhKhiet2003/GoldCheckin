using Base.Data;
using Base.Entities;
using Base.Models.Requests;
using Base.Repositories.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace Base.Repositories;

public class NotificationRepository(AppDbContext dbContext) : INotificationRepository
{
    public async Task<(List<Notification> Items, int Total)> ListAsync(Guid userId, NotificationQueryRequest request, CancellationToken cancellationToken)
    {
        var query = dbContext.Notifications.AsNoTracking().Where(item => item.UserId == userId);

        if (request.IsRead.HasValue)
        {
            query = request.IsRead.Value
                ? query.Where(item => item.ReadAt != null)
                : query.Where(item => item.ReadAt == null);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(item => item.CreatedAt)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public Task<int> CountUnreadAsync(Guid userId, CancellationToken cancellationToken)
    {
        return dbContext.Notifications.CountAsync(item => item.UserId == userId && item.ReadAt == null, cancellationToken);
    }

    public Task<Notification?> GetAsync(Guid id, Guid userId, CancellationToken cancellationToken)
    {
        return dbContext.Notifications.SingleOrDefaultAsync(item => item.Id == id && item.UserId == userId, cancellationToken);
    }

    public async Task MarkAllReadAsync(Guid userId, CancellationToken cancellationToken)
    {
        await dbContext.Notifications
            .Where(item => item.UserId == userId && item.ReadAt == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.ReadAt, DateTime.UtcNow), cancellationToken);
    }

    public Task SaveAsync(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
