using Base.Data;
using Base.Entities;
using Base.Models.Requests;
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

    public Task<Account?> FindByEmailAsync(string email, CancellationToken cancellationToken)
    {
        return dbContext.Accounts.SingleOrDefaultAsync(account => account.Email == email, cancellationToken);
    }

    public Task<bool> ExistsAsync(string username, string email, Guid? exceptId, CancellationToken cancellationToken)
    {
        return dbContext.Accounts.AnyAsync(
            account => (exceptId == null || account.Id != exceptId)
                && (account.Username == username || account.Email == email),
            cancellationToken);
    }

    public async Task<(List<Account> Items, int Total)> ListAsync(AccountQueryRequest request, CancellationToken cancellationToken)
    {
        var query = dbContext.Accounts.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var keyword = request.Keyword.Trim().ToLower();
            query = query.Where(item => item.Username.Contains(keyword)
                || item.FullName.ToLower().Contains(keyword)
                || item.Email.Contains(keyword));
        }

        if (!string.IsNullOrWhiteSpace(request.Role))
        {
            query = query.Where(item => item.Role == request.Role);
        }

        if (request.IsActive.HasValue)
        {
            query = query.Where(item => item.IsActive == request.IsActive.Value);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .AsNoTracking()
            .OrderByDescending(account => account.CreatedAt)
            .ThenBy(account => account.Id)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task AddAsync(Account account, CancellationToken cancellationToken)
    {
        await dbContext.Accounts.AddAsync(account, cancellationToken);
    }

    public Task<int> CountActiveAdminsAsync(CancellationToken cancellationToken)
    {
        return dbContext.Accounts.CountAsync(
            account => account.Role == Authorization.AppRoles.Admin && account.IsActive,
            cancellationToken);
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
