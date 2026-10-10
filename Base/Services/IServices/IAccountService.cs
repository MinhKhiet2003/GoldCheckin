using Base.Models.Requests;
using Base.Models.Responses;

namespace Base.Services.IServices;

public interface IAccountService
{
    Task<AccountImportResponse> ImportAsync(IFormFile file, CancellationToken cancellationToken);
    Task<UserResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken);
    Task<LoginResponse?> LoginAsync(LoginRequest request, string? deviceName, string? ipAddress, CancellationToken cancellationToken);
    Task<UserResponse> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<PagedResult<UserResponse>> ListAsync(AccountQueryRequest request, CancellationToken cancellationToken);
    Task<UserResponse> UpdateAsync(Guid id, ProfileRequest request, CancellationToken cancellationToken);
    Task<UserResponse> UpdateAvatarAsync(Guid id, string avatarUrl, CancellationToken cancellationToken);
    Task<UserResponse> AdminUpdateAsync(Guid id, AdminUpdateRequest request, CancellationToken cancellationToken);
    Task AdminResetPasswordAsync(Guid id, AdminResetPasswordRequest request, CancellationToken cancellationToken);
    Task<UserResponse> AssignRoleAsync(Guid actorId, Guid accountId, string role, CancellationToken cancellationToken);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}
