using Base.Common;
using Base.Entities;
using Base.Exceptions;
using Base.Models.Requests;
using Base.Models.Responses;
using Base.Repositories.IRepositories;
using Base.Services.IServices;
using Microsoft.AspNetCore.Identity;

namespace Base.Services;

public class AccountService(
    IAccountRepository repository,
    IPasswordHasher<Account> passwordHasher,
    IJwtTokenService tokenService,
    AuditContext audit) : IAccountService
{
    public async Task<UserResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        var username = request.Username.Trim().ToLowerInvariant();
        var email = request.Email.Trim().ToLowerInvariant();
        var exists = await repository.ExistsAsync(username, email, null, cancellationToken);

        if (exists)
        {
            throw new ApiException(409, "Tên đăng nhập hoặc email đã được sử dụng.");
        }

        var account = new Account
        {
            Username = username
        };

        ApplyProfile(account, request);
        account.PasswordHash = passwordHasher.HashPassword(account, request.Password);

        await repository.AddAsync(account, cancellationToken);
        await repository.SaveAsync(cancellationToken);

        audit.Begin("Create", "User", account.Id);
        audit.Complete(UserResponse.From(account));

        return UserResponse.From(account);
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var username = request.Username.Trim().ToLowerInvariant();
        var account = await repository.FindByUsernameAsync(username, cancellationToken);

        if (account is null || !account.IsActive)
        {
            throw new ApiException(401, "Tên đăng nhập hoặc mật khẩu không đúng, hoặc tài khoản đã bị khóa.");
        }

        var verificationResult = passwordHasher.VerifyHashedPassword(account, account.PasswordHash, request.Password);

        if (verificationResult == PasswordVerificationResult.Failed)
        {
            throw new ApiException(401, "Tên đăng nhập hoặc mật khẩu không đúng, hoặc tài khoản đã bị khóa.");
        }

        if (verificationResult == PasswordVerificationResult.SuccessRehashNeeded)
        {
            account.PasswordHash = passwordHasher.HashPassword(account, request.Password);
        }

        audit.Begin("Login", "User", account.Id, UserResponse.From(account));
        var now = DateTime.UtcNow;
        account.LastLoginAt = now;
        account.UpdatedAt = now;

        await repository.SaveAsync(cancellationToken);
        var token = tokenService.Create(account);
        audit.Complete(UserResponse.From(account));

        return new LoginResponse
        {
            AccessToken = token.Token,
            ExpiresAt = token.ExpiresAt,
            Account = UserResponse.From(account)
        };
    }

    public async Task<UserResponse> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var account = await RequireAsync(id, cancellationToken);
        return UserResponse.From(account);
    }

    public async Task<PagedResult<UserResponse>> ListAsync(int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        if (pageNumber < 1 || pageSize < 1 || pageSize > 100 || pageNumber > int.MaxValue / pageSize)
        {
            throw new ApiException(400, "Số trang phải lớn hơn 0 và nằm trong giới hạn cho phép; kích thước trang phải từ 1 đến 100.");
        }

        var (items, total) = await repository.ListAsync(pageNumber, pageSize, cancellationToken);

        return new PagedResult<UserResponse>
        {
            Items = items.Select(UserResponse.From).ToList(),
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalItems = total,
            TotalPages = (int)Math.Ceiling(total / (double)pageSize)
        };
    }

    public async Task<UserResponse> UpdateAsync(Guid id, ProfileRequest request, CancellationToken cancellationToken)
    {
        var account = await UpdateProfileAsync(id, request, cancellationToken);
        await repository.SaveAsync(cancellationToken);

        audit.Complete(UserResponse.From(account));
        return UserResponse.From(account);
    }

    public async Task<UserResponse> AdminUpdateAsync(Guid id, AdminUpdateRequest request, CancellationToken cancellationToken)
    {
        var account = await UpdateProfileAsync(id, request, cancellationToken);
        account.IsActive = request.IsActive;

        if (request.Role is not null)
        {
            account.Role = request.Role;
        }

        await repository.SaveAsync(cancellationToken);
        audit.Complete(UserResponse.From(account));
        return UserResponse.From(account);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var account = await RequireAsync(id, cancellationToken);
        audit.Begin("Delete", "User", account.Id, UserResponse.From(account));
        repository.Delete(account);

        await repository.SaveAsync(cancellationToken);
    }

    private async Task<Account> RequireAsync(Guid id, CancellationToken cancellationToken)
    {
        var account = await repository.GetAsync(id, cancellationToken);

        if (account is null)
        {
            throw new ApiException(404, "Không tìm thấy tài khoản.");
        }

        return account;
    }

    private async Task<Account> UpdateProfileAsync(Guid id, ProfileRequest request, CancellationToken cancellationToken)
    {
        var account = await RequireAsync(id, cancellationToken);
        var email = request.Email.Trim().ToLowerInvariant();
        var exists = await repository.ExistsAsync(account.Username, email, id, cancellationToken);

        if (exists)
        {
            throw new ApiException(409, "Email đã được sử dụng.");
        }

        audit.Begin("Update", "User", account.Id, UserResponse.From(account));
        ApplyProfile(account, request);
        account.UpdatedAt = DateTime.UtcNow;

        return account;
    }

    private static void ApplyProfile(Account account, ProfileRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var phone = string.IsNullOrWhiteSpace(request.PhoneNumber)
            ? null
            : request.PhoneNumber.Trim();

        if (account.Email != email)
        {
            account.EmailVerified = false;
        }

        if (account.PhoneNumber != phone)
        {
            account.PhoneVerified = false;
        }

        account.FullName = request.FullName.Trim();
        account.Email = email;
        account.PhoneNumber = phone;
        account.AvatarUrl = request.AvatarUrl;
        account.DateOfBirth = request.DateOfBirth;
        account.Gender = request.Gender;
        account.Address = request.Address?.Trim();
    }
}