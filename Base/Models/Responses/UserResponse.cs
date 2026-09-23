using Base.Entities;

namespace Base.Models.Responses;

public class UserResponse
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? AvatarUrl { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public string? Address { get; set; }
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool EmailVerified { get; set; }
    public bool PhoneVerified { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public static UserResponse From(Account account)
    {
        return new UserResponse
        {
            Id = account.Id,
            Username = account.Username,
            FullName = account.FullName,
            Email = account.Email,
            PhoneNumber = account.PhoneNumber,
            AvatarUrl = account.AvatarUrl,
            DateOfBirth = account.DateOfBirth,
            Gender = account.Gender,
            Address = account.Address,
            Role = account.Role,
            IsActive = account.IsActive,
            EmailVerified = account.EmailVerified,
            PhoneVerified = account.PhoneVerified,
            LastLoginAt = account.LastLoginAt,
            CreatedAt = account.CreatedAt,
            UpdatedAt = account.UpdatedAt
        };
    }
}