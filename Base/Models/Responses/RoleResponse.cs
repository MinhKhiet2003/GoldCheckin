namespace Base.Models.Responses;

public class RoleResponse
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public IEnumerable<string> PermissionCodes { get; set; } = [];
}
