using Base.Common;
using Base.Authorization;
using Base.Entities;
using Base.Exceptions;
using Base.Models.Requests;
using Base.Models.Responses;
using Base.Repositories.IRepositories;
using Base.Services.IServices;
using Microsoft.AspNetCore.Identity;

namespace Base.Services;

public partial class AccountService(
    IAccountRepository repository,
    IPasswordHasher<Account> passwordHasher,
    IAuthService authService,
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

    public async Task<LoginResponse?> LoginAsync(LoginRequest request, string? deviceName, string? ipAddress, CancellationToken cancellationToken)
    {
        var username = request.Username.Trim().ToLowerInvariant();
        var account = await repository.FindByUsernameAsync(username, cancellationToken);

        if (account is null || !account.IsActive)
        {
            return null;
        }

        var verificationResult = passwordHasher.VerifyHashedPassword(account, account.PasswordHash, request.Password);

        if (verificationResult == PasswordVerificationResult.Failed)
        {
            return null;
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
        audit.Complete(UserResponse.From(account));
        return await authService.CreateLoginResponseAsync(account.Id, deviceName, ipAddress, cancellationToken);
    }

    public async Task<UserResponse> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var account = await RequireAsync(id, cancellationToken);
        return UserResponse.From(account);
    }

    public async Task<PagedResult<UserResponse>> ListAsync(AccountQueryRequest request, CancellationToken cancellationToken)
    {
        if (request.PageNumber < 1 || request.PageSize < 1 || request.PageSize > 100 || request.PageNumber > int.MaxValue / request.PageSize)
        {
            throw new ApiException(400, "Số trang phải lớn hơn 0 và nằm trong giới hạn cho phép; kích thước trang phải từ 1 đến 100.");
        }

        var (items, total) = await repository.ListAsync(request, cancellationToken);

        return new PagedResult<UserResponse>
        {
            Items = items.Select(UserResponse.From).ToList(),
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            TotalItems = total,
            TotalPages = (int)Math.Ceiling(total / (double)request.PageSize)
        };
    }

    public async Task<UserResponse> UpdateAsync(Guid id, ProfileRequest request, CancellationToken cancellationToken)
    {
        var account = await UpdateProfileAsync(id, request, cancellationToken);
        await repository.SaveAsync(cancellationToken);

        audit.Complete(UserResponse.From(account));
        return UserResponse.From(account);
    }

    public async Task<UserResponse> UpdateAvatarAsync(Guid id, string avatarUrl, CancellationToken cancellationToken)
    {
        var account = await RequireAsync(id, cancellationToken);
        audit.Begin("Update", "User", account.Id, UserResponse.From(account));
        account.AvatarUrl = avatarUrl;
        account.UpdatedAt = DateTime.UtcNow;
        await repository.SaveAsync(cancellationToken);
        var response = UserResponse.From(account);
        audit.Complete(response);
        return response;
    }

    public async Task<UserResponse> AdminUpdateAsync(Guid id, AdminUpdateRequest request, CancellationToken cancellationToken)
    {
        var account = await UpdateProfileAsync(id, request, cancellationToken);
        account.IsActive = request.IsActive;

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

    public async Task<UserResponse> AssignRoleAsync(Guid actorId, Guid accountId, string role, CancellationToken cancellationToken)
    {
        if (role is not AppRoles.Admin and not AppRoles.User)
        {
            throw new ApiException(400, "Vai trò phải là admin hoặc user.");
        }

        var account = await RequireAsync(accountId, cancellationToken);
        if (account.Role == role)
        {
            return UserResponse.From(account);
        }

        if (account.Role == AppRoles.Admin && role == AppRoles.User)
        {
            if (actorId == accountId)
            {
                throw new ApiException(400, "Bạn không thể tự hạ quyền quản trị của mình.");
            }

            if (await repository.CountActiveAdminsAsync(cancellationToken) <= 1)
            {
                throw new ApiException(400, "Hệ thống phải luôn có ít nhất một quản trị viên đang hoạt động.");
            }
        }

        audit.Begin("AssignRole", "User", account.Id, UserResponse.From(account));
        account.Role = role;
        account.UpdatedAt = DateTime.UtcNow;
        await repository.SaveAsync(cancellationToken);
        var response = UserResponse.From(account);
        audit.Complete(response);
        return response;
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
        var phone = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim();
        if (phone is not null && await repository.PhoneExistsAsync(phone, account.Id, cancellationToken))
        {
            throw new ApiException(409, "Số điện thoại đã được sử dụng.");
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
        if (request.AvatarUrl is not null)
        {
            account.AvatarUrl = request.AvatarUrl;
        }
        account.DateOfBirth = request.DateOfBirth;
        account.Gender = request.Gender;
        account.Address = request.Address?.Trim();
    }
}
