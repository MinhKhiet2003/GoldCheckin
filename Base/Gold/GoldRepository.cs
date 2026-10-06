using Base.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data;

namespace Base.Gold;

public interface IGoldRepository
{
    IQueryable<T> Query<T>() where T : class;
    void Add<T>(T entity) where T : class;
    void Remove<T>(T entity) where T : class;
    Task Save(CancellationToken ct);
    Task<IDbContextTransaction> Transaction(CancellationToken ct);
}
public class GoldRepository(AppDbContext db) : IGoldRepository
{
    public IQueryable<T> Query<T>() where T : class => db.Set<T>();
    public void Add<T>(T entity) where T : class => db.Set<T>().Add(entity);
    public void Remove<T>(T entity) where T : class => db.Set<T>().Remove(entity);
    public Task Save(CancellationToken ct) => db.SaveChangesAsync(ct);
    public Task<IDbContextTransaction> Transaction(CancellationToken ct) =>
        db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
}
