using Base.Models.Requests;
using Base.Models.Responses;
using Base.Services.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Base.Controllers;

[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public class AuthenticationController(IAuthService service, IWebHostEnvironment environment) : ControllerBase
{
    private Guid CurrentUserId => Guid.Parse(User.FindFirst("sub")!.Value);

    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [HttpPost("refresh")]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> Refresh(RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var result = await service.RefreshAsync(request, HttpContext.Connection.RemoteIpAddress?.ToString(), cancellationToken);
        return Ok(new ApiResponse<LoginResponse> { StatusCode = 200, Success = true, Message = "Làm mới phiên đăng nhập thành công.", Data = result });
    }

    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [HttpPost("forgot-password")]
    public async Task<ActionResult<ApiResponse<ForgotPasswordResponse>>> ForgotPassword(ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        var result = await service.CreatePasswordResetAsync(request, environment.IsDevelopment(), cancellationToken);
        return Ok(new ApiResponse<ForgotPasswordResponse>
        {
            StatusCode = 200,
            Success = true,
            Message = "Nếu email tồn tại, hướng dẫn đặt lại mật khẩu đã được tạo.",
            Data = result
        });
    }

    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [HttpPost("reset-password")]
    public async Task<ActionResult<ApiResponse<object>>> ResetPassword(ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        await service.ResetPasswordAsync(request, cancellationToken);
        return Ok(new ApiResponse<object> { StatusCode = 200, Success = true, Message = "Đặt lại mật khẩu thành công. Vui lòng đăng nhập lại." });
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<ActionResult<ApiResponse<object>>> Logout(LogoutRequest request, CancellationToken cancellationToken)
    {
        await service.LogoutAsync(request.RefreshToken, cancellationToken);
        return Ok(new ApiResponse<object> { StatusCode = 200, Success = true, Message = "Đăng xuất thiết bị thành công." });
    }

    [Authorize]
    [HttpPost("logout-all")]
    public async Task<ActionResult<ApiResponse<object>>> LogoutAll(CancellationToken cancellationToken)
    {
        await service.LogoutAllAsync(CurrentUserId, cancellationToken);
        return Ok(new ApiResponse<object> { StatusCode = 200, Success = true, Message = "Đã đăng xuất tất cả thiết bị." });
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<ActionResult<ApiResponse<object>>> ChangePassword(ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        await service.ChangePasswordAsync(CurrentUserId, request, cancellationToken);
        return Ok(new ApiResponse<object> { StatusCode = 200, Success = true, Message = "Đổi mật khẩu thành công. Vui lòng đăng nhập lại trên các thiết bị." });
    }
}
