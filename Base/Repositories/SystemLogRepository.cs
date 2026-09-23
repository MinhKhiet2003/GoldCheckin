using Base.Data;
using Base.Entities;
using Base.Models.Requests;
using Base.Repositories.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace Base.Repositories;

public class SystemLogRepository(AppDbContext dbContext) : ISystemLogRepository
{
    public Task<SystemLog?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        return dbContext.SystemLogs.AsNoTracking().SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
    }

    public async Task AddAsync(SystemLog log, CancellationToken cancellationToken)
    {
        dbContext.SystemLogs.Add(log);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<(List<SystemLog> Items, int Total)> ListAsync(SystemLogQueryRequest request, CancellationToken cancellationToken)
    {
        var query = dbContext.SystemLogs.AsNoTracking();

        if (request.UserId.HasValue)
        {
            query = query.Where(item => item.UserId == request.UserId);
        }

        if (request.StatusCode.HasValue)
        {
            query = query.Where(item => item.StatusCode == request.StatusCode);
        }

        if (request.From.HasValue)
        {
            var from = request.From.Value.UtcDateTime;
            query = query.Where(item => item.CreatedAt >= from);
        }

        if (request.To.HasValue)
        {
            var to = request.To.Value.UtcDateTime;
            query = query.Where(item => item.CreatedAt <= to);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(item => item.CreatedAt)
            .ThenByDescending(item => item.Id)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }
}