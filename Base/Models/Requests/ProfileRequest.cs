using System.ComponentModel.DataAnnotations;

namespace Base.Models.Requests;

public class ProfileRequest : IValidatableObject
{
    [Required(ErrorMessage = "Vui lòng nhập họ và tên.")]
    [StringLength(150, ErrorMessage = "Họ và tên không được vượt quá 150 ký tự.")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập email.")]
    [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
    [StringLength(254, ErrorMessage = "Email không được vượt quá 254 ký tự.")]
    public string Email { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Số điện thoại không đúng định dạng.")]
    [StringLength(20, ErrorMessage = "Số điện thoại không được vượt quá 20 ký tự.")]
    public string? PhoneNumber { get; set; }

    [StringLength(2048, ErrorMessage = "Đường dẫn ảnh đại diện không được vượt quá 2048 ký tự.")]
    public string? AvatarUrl { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    [RegularExpression("^(Male|Female|Other|PreferNotToSay)$", ErrorMessage = "Giới tính phải là Male, Female, Other hoặc PreferNotToSay.")]
    public string? Gender { get; set; }

    [StringLength(500, ErrorMessage = "Địa chỉ không được vượt quá 500 ký tự.")]
    public string? Address { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (DateOfBirth > DateOnly.FromDateTime(DateTime.UtcNow))
        {
            yield return new ValidationResult(
                "Ngày sinh không được lớn hơn ngày hiện tại.",
                [nameof(DateOfBirth)]);
        }

        if (AvatarUrl is not null)
        {
            var validUrl = AvatarUrl.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase)
                || AvatarUrl.StartsWith("uploads/", StringComparison.OrdinalIgnoreCase)
                || AvatarUrl.StartsWith("/api/accounts/me/avatar/", StringComparison.OrdinalIgnoreCase)
                || (Uri.TryCreate(AvatarUrl, UriKind.Absolute, out var uri)
                    && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps));

            if (!validUrl)
            {
                yield return new ValidationResult(
                "Đường dẫn ảnh đại diện không hợp lệ.",
                    [nameof(AvatarUrl)]);
            }
        }
    }
}
