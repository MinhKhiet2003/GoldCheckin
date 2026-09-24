using System.ComponentModel.DataAnnotations;

namespace Base.Models.Requests;

public class AssignRoleRequest
{
    [Required(ErrorMessage = "Vui lòng chọn vai trò.")]
    [RegularExpression("^(admin|user)$", ErrorMessage = "Vai trò phải là admin hoặc user.")]
    public string Role { get; set; } = string.Empty;
}
