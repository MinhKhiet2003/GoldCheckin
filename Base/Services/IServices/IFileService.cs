using Base.Models.Responses;

namespace Base.Services.IServices;

public interface IFileService
{
    Task<UploadFileResponse> UploadAsync(IFormFile file, Guid userId, CancellationToken cancellationToken);
}