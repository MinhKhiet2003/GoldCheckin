using System.ComponentModel.DataAnnotations;

namespace Base.Models.Requests;

public class RefreshTokenRequest
{
    [Required(ErrorMessage = "Vui lòng cung cấp refresh token.")]
    [StringLength(512, ErrorMessage = "Refresh token không hợp lệ.")]
    public string RefreshToken { get; set; } = string.Empty;

    [StringLength(200, ErrorMessage = "Tên thiết bị không được vượt quá 200 ký tự.")]
    public string? DeviceName { get; set; }
}
