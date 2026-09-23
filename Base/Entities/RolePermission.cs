namespace Base.Entities;

public class RolePermission
{
    public string Role { get; set; } = string.Empty;
    public string PermissionCode { get; set; } = string.Empty;
    public Permission Permission { get; set; } = null!;
}