using System.ComponentModel.DataAnnotations;

namespace Base.Models.Requests;

public class ForgotPasswordRequest
{
    [Required(ErrorMessage = "Vui lòng nhập email.")]
    [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
    [StringLength(254, ErrorMessage = "Email không được vượt quá 254 ký tự.")]
    public string Email { get; set; } = string.Empty;
}
