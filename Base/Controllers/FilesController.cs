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
    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(FileService.MaxFileSize + 65536)]
    [RequestFormLimits(MultipartBodyLengthLimit = FileService.MaxFileSize + 65536)]
    public async Task<ActionResult<ApiResponse<UploadFileResponse>>> Upload(
        [FromForm] UploadFileRequest request,
        CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(User.FindFirst("sub")!.Value);
        var result = await service.UploadAsync(request.File, userId, cancellationToken);

        return Ok(new ApiResponse<UploadFileResponse>
        {
            StatusCode = StatusCodes.Status200OK,
            Success = true,
            Message = "Tải file lên thành công.",
            Data = result
        });
    }
}