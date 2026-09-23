using Microsoft.AspNetCore.Authorization;

namespace Base.Authorization;

public class PermissionRequirement(string permission) : IAuthorizationRequirement
{
    public string Permission { get; } = permission;
}