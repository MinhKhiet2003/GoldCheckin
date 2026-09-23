using Base.Repositories.IRepositories;
using Microsoft.AspNetCore.Authorization;

namespace Base.Authorization;

public class PermissionAuthorizationHandler(
    IAccountRepository accountRepository,
    IPermissionRepository permissionRepository,
    IHttpContextAccessor httpContextAccessor) : AuthorizationHandler<PermissionRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (!Guid.TryParse(context.User.FindFirst("sub")?.Value, out var id))
        {
            return;
        }

        var cancellationToken = httpContextAccessor.HttpContext?.RequestAborted ?? CancellationToken.None;
        var account = await accountRepository.GetAsync(id, cancellationToken);

        if (account is null || !account.IsActive)
        {
            return;
        }

        var permissions = await permissionRepository.GetByRoleAsync(account.Role, cancellationToken);

        if (permissions.Contains(requirement.Permission))
        {
            context.Succeed(requirement);
        }
    }
}