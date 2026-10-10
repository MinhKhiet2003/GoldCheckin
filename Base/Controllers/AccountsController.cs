using Microsoft.Extensions.Caching.Memory;
using Base.Authorization;
using Base.Models.Requests;
using Base.Models.Responses;
using Base.Services;
using Base.Services.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Http.Metadata;

namespace Base.Controllers;

[ApiController]
[Route("api/accounts")]
[Produces("application/json")]
[Authorize]
public class AccountsController(IAccountService accountService, IMemoryCache cache) : ControllerBase
{
    [Authorize(Roles = AppRoles.Admin)]
    [EndpointSummary("Nhập tài khoản từ Excel")]
    [HttpPost("import")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(AccountService.MaxImportFileSize + 65536)]
    public async Task<ActionResult<ApiResponse<AccountImportResponse>>> Import(
        [FromForm] UploadFileRequest request, CancellationToken cancellationToken)
    {
        var result = await accountService.ImportAsync(request.File, cancellationToken);
        if (result.DuplicateCount > 0)
        {
            var reportId = Guid.NewGuid();
            cache.Set($"account-import:{reportId}", result.Duplicates.ToArray(), TimeSpan.FromMinutes(30));
            result.DuplicateDownloadUrl = Url.Action(nameof(DownloadImportDuplicates), new { reportId });
        }
        return Ok(new ApiResponse<AccountImportResponse>
        {
            StatusCode = 200,
            Success = true,
            Message = $"Đã thêm {result.ImportedCount} tài khoản; bỏ qua {result.DuplicateCount} tài khoản đã có/trùng; {result.InvalidCount} dòng không hợp lệ.",
            Data = result
        });
    }

    [Authorize(Roles = AppRoles.Admin)]
    [EndpointSummary("Tải file Excel mẫu nhập tài khoản")]
    [HttpGet("import/template")]
    [Produces("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    public IActionResult ImportTemplate() => File(
        AccountService.CreateImportTemplate(),
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        "accounts-template.xlsx");

    [Authorize(Roles = AppRoles.Admin)]
    [EndpointSummary("Tải danh sách tài khoản bị trùng sau import (hiệu lực 30 phút)")]
    [HttpGet("import/duplicates/{reportId:guid}")]
    [Produces("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    public IActionResult DownloadImportDuplicates(Guid reportId)
    {
        if (!cache.TryGetValue<AccountImportDuplicate[]>($"account-import:{reportId}", out var duplicates) || duplicates is null)
            throw new Base.Exceptions.ApiException(404, "Danh sách trùng không tồn tại hoặc đã hết hạn. Vui lòng import lại.");
        return File(AccountService.CreateDuplicateReport(duplicates),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "accounts-duplicates.xlsx");
    }

    private Guid CurrentId
    {
        get
        {
            return Guid.Parse(User.FindFirst("sub")!.Value);
        }
    }

    [AllowAnonymous]
    [EndpointSummary("Đăng ký tài khoản mới")]
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
    [EndpointSummary("Đăng nhập và nhận token")]
    [HttpPost("login")]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> Login(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var login = await accountService.LoginAsync(
            request,
            Request.Headers.UserAgent.ToString(),
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            cancellationToken);
        if (login is null)
        {
            return Unauthorized(new ApiResponse<LoginResponse>
            {
                StatusCode = StatusCodes.Status401Unauthorized,
                Success = false,
                Message = "Tên đăng nhập hoặc mật khẩu không đúng, hoặc tài khoản đã bị khóa."
            });
        }
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
    [EndpointSummary("Xem thông tin cá nhân")]
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

    [HttpGet("me/avatar/{id:guid}")]
    public async Task<IActionResult> Avatar(Guid id, [FromServices] IFileService fileService, CancellationToken cancellationToken)
    {
        var file = await fileService.GetAsync(id, CurrentId, false, cancellationToken);
        var path = Path.Combine(HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>().WebRootPath, "uploads", file.StoredName);
        if (!System.IO.File.Exists(path)) return NotFound();
        Response.Headers["Cache-Control"] = "private, max-age=300";
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        return PhysicalFile(path, file.ContentType);
    }

    [Authorize(Policy = AppPermissions.UpdateOwnProfile)]
    [EndpointSummary("Cập nhật thông tin cá nhân")]
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

    [Authorize(Policy = AppPermissions.UpdateOwnProfile)]
    [EndpointSummary("Tải và cập nhật ảnh đại diện")]
    [HttpPost("me/avatar")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(FileService.MaxFileSize + 65536)]
    public async Task<ActionResult<ApiResponse<UserResponse>>> UploadAvatar(
        [FromForm] UploadFileRequest request,
        [FromServices] IFileService fileService,
        CancellationToken cancellationToken)
    {
        var extension = Path.GetExtension(request.File.FileName).ToLowerInvariant();
        if (extension is not ".jpg" and not ".jpeg" and not ".png")
        {
            throw new Base.Exceptions.ApiException(400, "Ảnh đại diện phải là file JPG, JPEG hoặc PNG.");
        }

        var file = await fileService.UploadAsync(request.File, CurrentId, cancellationToken);
        var account = await accountService.UpdateAvatarAsync(CurrentId, $"/api/accounts/me/avatar/{file.Id}", cancellationToken);
        return Ok(new ApiResponse<UserResponse>
        {
            StatusCode = 200,
            Success = true,
            Message = "Cập nhật ảnh đại diện thành công.",
            Data = account
        });
    }

    [Authorize(Policy = AppPermissions.ListUsers)]
    [EndpointSummary("Lấy danh sách tài khoản")]
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<UserResponse>>>> List(
        [FromQuery] AccountQueryRequest request,
        CancellationToken cancellationToken)
    {
        var accounts = await accountService.ListAsync(request, cancellationToken);

        return Ok(new ApiResponse<PagedResult<UserResponse>>
        {
            StatusCode = StatusCodes.Status200OK,
            Success = true,
            Message = "Lấy danh sách tài khoản thành công.",
            Data = accounts
        });
    }

    [Authorize(Policy = AppPermissions.ReadUser)]
    [EndpointSummary("Xem chi tiết tài khoản")]
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
    [EndpointSummary("Cập nhật tài khoản bởi quản trị viên")]
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

    [Authorize(Roles = AppRoles.Admin)]
    [Authorize(Policy = AppPermissions.UpdateUser)]
    [EndpointSummary("Cấp lại mật khẩu cho nhân viên")]
    [HttpPut("{id:guid}/password")]
    public async Task<ActionResult<ApiResponse<object>>> AdminResetPassword(
        Guid id,
        AdminResetPasswordRequest request,
        CancellationToken cancellationToken)
    {
        await accountService.AdminResetPasswordAsync(id, request, cancellationToken);

        return Ok(new ApiResponse<object>
        {
            StatusCode = StatusCodes.Status200OK,
            Success = true,
            Message = "Đã cấp lại mật khẩu. Nhân viên cần đăng nhập lại trên các thiết bị."
        });
    }

    [Authorize(Policy = AppPermissions.DeleteUser)]
    [EndpointSummary("Xóa tài khoản")]
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

    [Authorize(Roles = AppRoles.Admin)]
    [Authorize(Policy = AppPermissions.ManageRoles)]
    [EndpointSummary("Gán vai trò cho tài khoản")]
    [HttpPut("{id:guid}/role")]
    public async Task<ActionResult<ApiResponse<UserResponse>>> AssignRole(
        Guid id,
        AssignRoleRequest request,
        CancellationToken cancellationToken)
    {
        var result = await accountService.AssignRoleAsync(CurrentId, id, request.Role, cancellationToken);
        return Ok(new ApiResponse<UserResponse>
        {
            StatusCode = 200,
            Success = true,
            Message = "Gán vai trò cho tài khoản thành công.",
            Data = result
        });
    }
}
