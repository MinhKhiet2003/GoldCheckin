using Base.Models.Requests;
using Base.Models.Responses;

namespace Base.Services.IServices;

public interface IAuthService
{
    Task<LoginResponse> CreateLoginResponseAsync(Guid userId, string? deviceName, string? ipAddress, CancellationToken cancellationToken);
    Task<LoginResponse> RefreshAsync(RefreshTokenRequest request, string? ipAddress, CancellationToken cancellationToken);
    Task LogoutAsync(string refreshToken, CancellationToken cancellationToken);
    Task LogoutAllAsync(Guid userId, CancellationToken cancellationToken);
    Task<ForgotPasswordResponse> CreatePasswordResetAsync(ForgotPasswordRequest request, bool includeToken, CancellationToken cancellationToken);
    Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken);
    Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken cancellationToken);
}
