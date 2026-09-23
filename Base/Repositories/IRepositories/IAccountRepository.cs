using Base.Entities;

namespace Base.Repositories.IRepositories;

public interface IAccountRepository
{
    Task<Account?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<Account?> FindByUsernameAsync(string username, CancellationToken cancellationToken);
    Task<bool> ExistsAsync(string username, string email, Guid? exceptId, CancellationToken cancellationToken);
    Task<(List<Account> Items, int Total)> ListAsync(int pageNumber, int pageSize, CancellationToken cancellationToken);
    Task AddAsync(Account account, CancellationToken cancellationToken);
    void Delete(Account account);
    Task SaveAsync(CancellationToken cancellationToken);
}