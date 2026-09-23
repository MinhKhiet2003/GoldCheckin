using System.ComponentModel.DataAnnotations;

namespace Base.Models.Requests;

public class AdminUpdateRequest : ProfileRequest
{
    [StringLength(5, MinimumLength = 4, ErrorMessage = "Vai trò phải là admin hoặc user.")]
    [RegularExpression("^(admin|user)$", ErrorMessage = "Vai trò phải là admin hoặc user.")]
    public string? Role { get; set; }

    public bool IsActive { get; set; } = true;
}