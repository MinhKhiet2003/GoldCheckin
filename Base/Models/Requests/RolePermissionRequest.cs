using System.ComponentModel.DataAnnotations;

namespace Base.Models.Requests;

public class RolePermissionRequest
{
    [Required(ErrorMessage = "Vui lòng cung cấp danh sách quyền.")]
    public ICollection<string> PermissionCodes { get; set; } = [];
}
