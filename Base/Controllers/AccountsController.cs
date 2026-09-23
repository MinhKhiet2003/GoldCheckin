using Base.Authorization;
using Base.Models.Requests;
using Base.Models.Responses;
using Base.Services.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Base.Controllers;

[ApiController]
[Route("api/accounts")]
[Produces("application/json")]
[Authorize]
public class AccountsController(IAccountService accountService) : ControllerBase
{
    private Guid CurrentId
    {
        get
        {
            return Guid.Parse(User.FindFirst("sub")!.Value);
        }
    }

    [AllowAnonymous]
    [HttpPost("register")]
    [EnableRateLimiting("auth")]
    [ProducesResponseType<ApiResponse<UserResponse>>(StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<UserResponse>>> Register(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var account = await accountService.RegisterAsync(request, cancellationToken);
        HttpContext.Items["AuthenticatedUserId"] = account.Id;

        return StatusCode(StatusCodes.Status201Created, new ApiResponse<UserResponse>
        {
            StatusCode = StatusCodes.Status201Created,
            Success = true,
            Message = "Đăng ký tài khoản thành công.",
            Data = account
        });
    }

    [AllowAnonymous]
    [HttpPost("login")]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> Login(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var login = await accountService.LoginAsync(request, cancellationToken);
        HttpContext.Items["AuthenticatedUserId"] = login.Account.Id;

        return Ok(new ApiResponse<LoginResponse>
        {
            StatusCode = StatusCodes.Status200OK,
            Success = true,
            Message = "Đăng nhập thành công.",
            Data = login
        });
    }

    [Authorize(Policy = AppPermissions.ReadOwnProfile)]
    [HttpGet("me")]
    public async Task<ActionResult<ApiResponse<UserResponse>>> Me(CancellationToken cancellationToken)
    {
        var account = await accountService.GetAsync(CurrentId, cancellationToken);

        return Ok(new ApiResponse<UserResponse>
        {
            StatusCode = StatusCodes.Status200OK,
            Success = true,
            Message = "Lấy thông tin tài khoản thành công.",
            Data = account
        });
    }

    [Authorize(Policy = AppPermissions.UpdateOwnProfile)]
    [HttpPut("me")]
    public async Task<ActionResult<ApiResponse<UserResponse>>> UpdateMe(
        ProfileRequest request,
        CancellationToken cancellationToken)
    {
        var account = await accountService.UpdateAsync(CurrentId, request, cancellationToken);

        return Ok(new ApiResponse<UserResponse>
        {
            StatusCode = StatusCodes.Status200OK,
            Success = true,
            Message = "Cập nhật thông tin cá nhân thành công.",
            Data = account
        });
    }

    [Authorize(Policy = AppPermissions.ListUsers)]
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<UserResponse>>>> List(
        CancellationToken cancellationToken,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        var accounts = await accountService.ListAsync(pageNumber, pageSize, cancellationToken);

        return Ok(new ApiResponse<PagedResult<UserResponse>>
        {
            StatusCode = StatusCodes.Status200OK,
            Success = true,
            Message = "Lấy danh sách tài khoản thành công.",
            Data = accounts
        });
    }

    [Authorize(Policy = AppPermissions.ReadUser)]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<UserResponse>>> Get(Guid id, CancellationToken cancellationToken)
    {
        var account = await accountService.GetAsync(id, cancellationToken);

        return Ok(new ApiResponse<UserResponse>
        {
            StatusCode = StatusCodes.Status200OK,
            Success = true,
            Message = "Lấy thông tin tài khoản thành công.",
            Data = account
        });
    }

    [Authorize(Roles = AppRoles.Admin)]
    [Authorize(Policy = AppPermissions.UpdateUser)]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<UserResponse>>> Update(
        Guid id,
        AdminUpdateRequest request,
        CancellationToken cancellationToken)
    {
        var account = await accountService.AdminUpdateAsync(id, request, cancellationToken);

        return Ok(new ApiResponse<UserResponse>
        {
            StatusCode = StatusCodes.Status200OK,
            Success = true,
            Message = "Cập nhật tài khoản thành công.",
            Data = account
        });
    }

    [Authorize(Policy = AppPermissions.DeleteUser)]
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id, CancellationToken cancellationToken)
    {
        await accountService.DeleteAsync(id, cancellationToken);

        return Ok(new ApiResponse<object>
        {
            StatusCode = StatusCodes.Status200OK,
            Success = true,
            Message = "Xóa tài khoản thành công."
        });
    }
}