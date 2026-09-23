using Base.Data;
using Base.Entities;
using Base.Repositories.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace Base.Repositories;

public class AccountRepository(AppDbContext dbContext) : IAccountRepository
{
    public Task<Account?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        return dbContext.Accounts.SingleOrDefaultAsync(account => account.Id == id, cancellationToken);
    }

    public Task<Account?> FindByUsernameAsync(string username, CancellationToken cancellationToken)
    {
        return dbContext.Accounts.SingleOrDefaultAsync(account => account.Username == username, cancellationToken);
    }

    public Task<bool> ExistsAsync(string username, string email, Guid? exceptId, CancellationToken cancellationToken)
    {
        return dbContext.Accounts.AnyAsync(
            account => (exceptId == null || account.Id != exceptId)
                && (account.Username == username || account.Email == email),
            cancellationToken);
    }

    public async Task<(List<Account> Items, int Total)> ListAsync(int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var total = await dbContext.Accounts.CountAsync(cancellationToken);
        var items = await dbContext.Accounts
            .AsNoTracking()
            .OrderByDescending(account => account.CreatedAt)
            .ThenBy(account => account.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task AddAsync(Account account, CancellationToken cancellationToken)
    {
        await dbContext.Accounts.AddAsync(account, cancellationToken);
    }

    public void Delete(Account account)
    {
        dbContext.Accounts.Remove(account);
    }

    public async Task SaveAsync(CancellationToken cancellationToken)
    {
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}