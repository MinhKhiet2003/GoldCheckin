using System.ComponentModel.DataAnnotations;

namespace Base.Models.Requests;

public class RegisterRequest : ProfileRequest
{
    [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập.")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Tên đăng nhập phải có từ 3 đến 50 ký tự.")]
    [RegularExpression("^[a-zA-Z0-9_.-]+$", ErrorMessage = "Tên đăng nhập chỉ được chứa chữ Latin, số, dấu gạch dưới, dấu chấm và dấu gạch ngang.")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
    [StringLength(128, MinimumLength = 12, ErrorMessage = "Mật khẩu phải có từ 12 đến 128 ký tự.")]
    public string Password { get; set; } = string.Empty;
}