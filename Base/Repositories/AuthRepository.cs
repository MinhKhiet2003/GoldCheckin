using Base.Data;
using Base.Entities;
using Base.Repositories.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace Base.Repositories;

public class AuthRepository(AppDbContext dbContext) : IAuthRepository
{
    public Task<RefreshToken?> GetRefreshTokenAsync(string tokenHash, CancellationToken cancellationToken)
    {
        return dbContext.RefreshTokens.SingleOrDefaultAsync(item => item.TokenHash == tokenHash, cancellationToken);
    }

    public Task<PasswordResetToken?> GetPasswordResetTokenAsync(string tokenHash, CancellationToken cancellationToken)
    {
        return dbContext.PasswordResetTokens.SingleOrDefaultAsync(item => item.TokenHash == tokenHash, cancellationToken);
    }

    public Task<bool> IsRefreshSessionActiveAsync(Guid sessionId, Guid userId, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        return dbContext.RefreshTokens.AnyAsync(
            item => item.Id == sessionId
                && item.UserId == userId
                && item.RevokedAt == null
                && item.ExpiresAt > now,
            cancellationToken);
    }

    public Task AddRefreshTokenAsync(RefreshToken token, CancellationToken cancellationToken)
    {
        return dbContext.RefreshTokens.AddAsync(token, cancellationToken).AsTask();
    }

    public Task AddPasswordResetTokenAsync(PasswordResetToken token, CancellationToken cancellationToken)
    {
        return dbContext.PasswordResetTokens.AddAsync(token, cancellationToken).AsTask();
    }

    public Task RevokeAllRefreshTokensAsync(Guid userId, DateTime revokedAt, CancellationToken cancellationToken)
    {
        return dbContext.RefreshTokens
            .Where(item => item.UserId == userId && item.RevokedAt == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.RevokedAt, revokedAt), cancellationToken);
    }

    public Task SaveAsync(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
