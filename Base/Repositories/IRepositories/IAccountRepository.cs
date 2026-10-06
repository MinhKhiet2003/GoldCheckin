using Base.Entities;
using Base.Models.Requests;

namespace Base.Repositories.IRepositories;

public interface IAccountRepository
{
    Task<Account?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<Account?> FindByUsernameAsync(string username, CancellationToken cancellationToken);
    Task<Account?> FindByEmailAsync(string email, CancellationToken cancellationToken);
    Task<bool> ExistsForImportAsync(string username, string email, string? phoneNumber, CancellationToken cancellationToken);
    Task<bool> ExistsAsync(string username, string email, Guid? exceptId, CancellationToken cancellationToken);
    Task<bool> PhoneExistsAsync(string phoneNumber, Guid exceptId, CancellationToken cancellationToken);
    Task<(List<Account> Items, int Total)> ListAsync(AccountQueryRequest request, CancellationToken cancellationToken);
    Task AddAsync(Account account, CancellationToken cancellationToken);
    Task<int> CountActiveAdminsAsync(CancellationToken cancellationToken);
    void Delete(Account account);
    Task SaveAsync(CancellationToken cancellationToken);
}
