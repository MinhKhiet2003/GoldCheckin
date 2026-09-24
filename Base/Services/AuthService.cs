using System.Security.Cryptography;
using System.Text;
using Base.Common;
using Base.Entities;
using Base.Exceptions;
using Base.Models.Requests;
using Base.Models.Responses;
using Base.Repositories.IRepositories;
using Base.Services.IServices;
using Microsoft.AspNetCore.Identity;

namespace Base.Services;

public class AuthService(
    IAccountRepository accountRepository,
    IAuthRepository authRepository,
    IEmailService emailService,
    IPasswordHasher<Account> passwordHasher,
    IJwtTokenService jwtTokenService,
    AuditContext audit) : IAuthService
{
    private static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(14);
    private static readonly TimeSpan ResetTokenLifetime = TimeSpan.FromMinutes(30);

    public async Task<LoginResponse> CreateLoginResponseAsync(Guid userId, string? deviceName, string? ipAddress, CancellationToken cancellationToken)
    {
        var account = await accountRepository.GetAsync(userId, cancellationToken)
            ?? throw new ApiException(401, "Phiên đăng nhập không hợp lệ.");

        if (!account.IsActive)
        {
            throw new ApiException(401, "Tài khoản đã bị khóa.");
        }

        return await CreateSessionAsync(account, deviceName, ipAddress, cancellationToken);
    }

    public async Task<LoginResponse> RefreshAsync(RefreshTokenRequest request, string? ipAddress, CancellationToken cancellationToken)
    {
        var token = await authRepository.GetRefreshTokenAsync(Hash(request.RefreshToken), cancellationToken);
        var now = DateTime.UtcNow;

        if (token is null || token.RevokedAt is not null || token.ExpiresAt <= now)
        {
            throw new ApiException(401, "Refresh token không hợp lệ hoặc đã hết hạn.");
        }

        var account = await accountRepository.GetAsync(token.UserId, cancellationToken);
        if (account is null || !account.IsActive)
        {
            throw new ApiException(401, "Tài khoản không tồn tại hoặc đã bị khóa.");
        }

        token.RevokedAt = now;
        await authRepository.SaveAsync(cancellationToken);
        audit.Begin("RefreshToken", "User", account.Id);
        var response = await CreateSessionAsync(account, request.DeviceName ?? token.DeviceName, ipAddress, cancellationToken);
        audit.Complete(new { response.Account, response.ExpiresAt, response.RefreshTokenExpiresAt });
        return response;
    }

    public async Task LogoutAsync(string refreshToken, CancellationToken cancellationToken)
    {
        var token = await authRepository.GetRefreshTokenAsync(Hash(refreshToken), cancellationToken);
        if (token is not null && token.RevokedAt is null)
        {
            token.RevokedAt = DateTime.UtcNow;
            await authRepository.SaveAsync(cancellationToken);
            audit.Begin("Logout", "User", token.UserId);
        }
    }

    public async Task LogoutAllAsync(Guid userId, CancellationToken cancellationToken)
    {
        await authRepository.RevokeAllRefreshTokensAsync(userId, DateTime.UtcNow, cancellationToken);
        audit.Begin("LogoutAll", "User", userId);
    }

    public async Task<ForgotPasswordResponse> CreatePasswordResetAsync(ForgotPasswordRequest request, bool includeToken, CancellationToken cancellationToken)
    {
        var account = await accountRepository.FindByEmailAsync(request.Email.Trim().ToLowerInvariant(), cancellationToken);
        if (account is null || !account.IsActive)
        {
            return new ForgotPasswordResponse();
        }

        var rawToken = CreateRandomToken();
        var expiresAt = DateTime.UtcNow.Add(ResetTokenLifetime);
        await authRepository.AddPasswordResetTokenAsync(new PasswordResetToken
        {
            UserId = account.Id,
            TokenHash = Hash(rawToken),
            ExpiresAt = expiresAt
        }, cancellationToken);
        await authRepository.SaveAsync(cancellationToken);
        await emailService.SendPasswordResetAsync(account.Email, account.FullName, rawToken, expiresAt, cancellationToken);

        audit.Begin("ForgotPassword", "User", account.Id);
        audit.Complete(new { expiresAt });
        return new ForgotPasswordResponse
        {
            ResetToken = includeToken ? rawToken : null,
            ExpiresAt = includeToken ? expiresAt : null
        };
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        var token = await authRepository.GetPasswordResetTokenAsync(Hash(request.Token), cancellationToken);
        var now = DateTime.UtcNow;
        if (token is null || token.UsedAt is not null || token.ExpiresAt <= now)
        {
            throw new ApiException(400, "Token đặt lại mật khẩu không hợp lệ hoặc đã hết hạn.");
        }

        var account = await accountRepository.GetAsync(token.UserId, cancellationToken)
            ?? throw new ApiException(400, "Token đặt lại mật khẩu không hợp lệ.");
        token.UsedAt = now;
        account.PasswordHash = passwordHasher.HashPassword(account, request.NewPassword);
        account.UpdatedAt = now;
        await authRepository.RevokeAllRefreshTokensAsync(account.Id, now, cancellationToken);
        await authRepository.SaveAsync(cancellationToken);
        audit.Begin("ResetPassword", "User", account.Id);
    }

    public async Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        var account = await accountRepository.GetAsync(userId, cancellationToken)
            ?? throw new ApiException(404, "Không tìm thấy tài khoản.");
        if (passwordHasher.VerifyHashedPassword(account, account.PasswordHash, request.CurrentPassword) == PasswordVerificationResult.Failed)
        {
            throw new ApiException(400, "Mật khẩu hiện tại không đúng.");
        }

        var now = DateTime.UtcNow;
        account.PasswordHash = passwordHasher.HashPassword(account, request.NewPassword);
        account.UpdatedAt = now;
        await authRepository.RevokeAllRefreshTokensAsync(account.Id, now, cancellationToken);
        await authRepository.SaveAsync(cancellationToken);
        audit.Begin("ChangePassword", "User", account.Id);
    }

    private async Task<LoginResponse> CreateSessionAsync(Account account, string? deviceName, string? ipAddress, CancellationToken cancellationToken)
    {
        var rawRefreshToken = CreateRandomToken();
        var refreshExpiresAt = DateTime.UtcNow.Add(RefreshTokenLifetime);
        var refreshSession = new RefreshToken
        {
            UserId = account.Id,
            TokenHash = Hash(rawRefreshToken),
            DeviceName = string.IsNullOrWhiteSpace(deviceName) ? null : deviceName.Trim(),
            IpAddress = ipAddress,
            ExpiresAt = refreshExpiresAt
        };
        await authRepository.AddRefreshTokenAsync(refreshSession, cancellationToken);
        await authRepository.SaveAsync(cancellationToken);

        var accessToken = jwtTokenService.Create(account, refreshSession.Id);
        return new LoginResponse
        {
            AccessToken = accessToken.Token,
            ExpiresAt = accessToken.ExpiresAt,
            RefreshToken = rawRefreshToken,
            RefreshTokenExpiresAt = refreshExpiresAt,
            Account = UserResponse.From(account)
        };
    }

    private static string CreateRandomToken()
    {
        return Microsoft.AspNetCore.WebUtilities.WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(64));
    }

    private static string Hash(string value)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    }
}
