using System.ComponentModel.DataAnnotations;

namespace Base.Models.Requests;

public class AdminResetPasswordRequest
{
    [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới.")]
    [StringLength(128, MinimumLength = 12, ErrorMessage = "Mật khẩu mới phải có từ 12 đến 128 ký tự.")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập lại mật khẩu mới.")]
    [StringLength(128, MinimumLength = 12, ErrorMessage = "Mật khẩu nhập lại phải có từ 12 đến 128 ký tự.")]
    [Compare(nameof(NewPassword), ErrorMessage = "Mật khẩu nhập lại không khớp.")]
    public string ConfirmNewPassword { get; set; } = string.Empty;
}
