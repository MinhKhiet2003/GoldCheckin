using System.ComponentModel.DataAnnotations;

namespace Base.Models.Requests;

public class LogoutRequest
{
    [Required(ErrorMessage = "Vui lòng cung cấp refresh token.")]
    [StringLength(512, ErrorMessage = "Refresh token không hợp lệ.")]
    public string RefreshToken { get; set; } = string.Empty;
}
