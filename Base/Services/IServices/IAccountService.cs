using Base.Models.Requests;
using Base.Models.Responses;

namespace Base.Services.IServices;

public interface IAccountService
{
    Task<UserResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken);
    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken);
    Task<UserResponse> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<PagedResult<UserResponse>> ListAsync(int pageNumber, int pageSize, CancellationToken cancellationToken);
    Task<UserResponse> UpdateAsync(Guid id, ProfileRequest request, CancellationToken cancellationToken);
    Task<UserResponse> AdminUpdateAsync(Guid id, AdminUpdateRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}