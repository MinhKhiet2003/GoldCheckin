using System.ComponentModel.DataAnnotations;

namespace Base.Models.Requests;

public class SystemSettingRequest
{
    [Required(ErrorMessage = "Vui lòng nhập giá trị cấu hình.")]
    [StringLength(4000, ErrorMessage = "Giá trị cấu hình không được vượt quá 4000 ký tự.")]
    public string Value { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Mô tả không được vượt quá 500 ký tự.")]
    public string? Description { get; set; }
}
