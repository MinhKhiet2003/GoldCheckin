using System.ComponentModel.DataAnnotations;

namespace Base.Models.Requests;

public class ChangePasswordRequest
{
    [Required(ErrorMessage = "Vui lòng nhập mật khẩu hiện tại.")]
    [StringLength(128, ErrorMessage = "Mật khẩu hiện tại không hợp lệ.")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới.")]
    [StringLength(128, MinimumLength = 12, ErrorMessage = "Mật khẩu mới phải có từ 12 đến 128 ký tự.")]
    public string NewPassword { get; set; } = string.Empty;
}
