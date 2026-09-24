using System.ComponentModel.DataAnnotations;

namespace Base.Models.Requests;

public class ResetPasswordRequest
{
    [Required(ErrorMessage = "Vui lòng cung cấp token đặt lại mật khẩu.")]
    [StringLength(512, ErrorMessage = "Token đặt lại mật khẩu không hợp lệ.")]
    public string Token { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới.")]
    [StringLength(128, MinimumLength = 12, ErrorMessage = "Mật khẩu mới phải có từ 12 đến 128 ký tự.")]
    public string NewPassword { get; set; } = string.Empty;
}
