using Base.Entities;

namespace Base.Repositories.IRepositories;

public interface IAuthRepository
{
    Task<RefreshToken?> GetRefreshTokenAsync(string tokenHash, CancellationToken cancellationToken);
    Task<PasswordResetToken?> GetPasswordResetTokenAsync(string tokenHash, CancellationToken cancellationToken);
    Task<bool> IsRefreshSessionActiveAsync(Guid sessionId, Guid userId, CancellationToken cancellationToken);
    Task AddRefreshTokenAsync(RefreshToken token, CancellationToken cancellationToken);
    Task AddPasswordResetTokenAsync(PasswordResetToken token, CancellationToken cancellationToken);
    Task RevokeAllRefreshTokensAsync(Guid userId, DateTime revokedAt, CancellationToken cancellationToken);
    Task SaveAsync(CancellationToken cancellationToken);
}
