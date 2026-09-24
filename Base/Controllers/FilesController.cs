using Base.Authorization;
using Base.Models.Requests;
using Base.Models.Responses;
using Base.Services;
using Base.Services.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Base.Controllers;

[ApiController]
[Route("api/files")]
[Produces("application/json")]
[Authorize(Policy = AppPermissions.UploadFiles)]
public class FilesController(IFileService service) : ControllerBase
{
    private Guid CurrentUserId => Guid.Parse(User.FindFirst("sub")!.Value);

    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(FileService.MaxFileSize + 65536)]
    [RequestFormLimits(MultipartBodyLengthLimit = FileService.MaxFileSize + 65536)]
    public async Task<ActionResult<ApiResponse<UploadFileResponse>>> Upload(
        [FromForm] UploadFileRequest request,
        CancellationToken cancellationToken)
    {
        var result = await service.UploadAsync(request.File, CurrentUserId, cancellationToken);

        return Ok(new ApiResponse<UploadFileResponse>
        {
            StatusCode = StatusCodes.Status200OK,
            Success = true,
            Message = "Tải file lên thành công.",
            Data = result
        });
    }

    [Authorize(Policy = AppPermissions.ReadFiles)]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> View(Guid id, [FromQuery] bool download, CancellationToken cancellationToken)
    {
        var file = await service.GetAsync(id, CurrentUserId, User.IsInRole(AppRoles.Admin), cancellationToken);
        var path = Path.Combine(HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>().WebRootPath, "uploads", file.StoredName);
        if (!System.IO.File.Exists(path))
        {
            return NotFound(new ApiResponse<object> { StatusCode = 404, Success = false, Message = "Không tìm thấy file trên máy chủ." });
        }

        return PhysicalFile(path, file.ContentType, download ? file.OriginalName : null, enableRangeProcessing: true);
    }

    [Authorize(Policy = AppPermissions.DeleteFiles)]
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id, CancellationToken cancellationToken)
    {
        await service.DeleteAsync(id, CurrentUserId, User.IsInRole(AppRoles.Admin), cancellationToken);
        return Ok(new ApiResponse<object> { StatusCode = 200, Success = true, Message = "Xóa file thành công." });
    }
}
