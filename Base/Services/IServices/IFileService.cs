using Base.Entities;
using Base.Models.Responses;

namespace Base.Services.IServices;

public interface IFileService
{
    Task<UploadFileResponse> UploadAsync(IFormFile file, Guid userId, CancellationToken cancellationToken);
    Task<UploadedFile> GetAsync(Guid id, Guid userId, bool isAdmin, CancellationToken cancellationToken);
    Task DeleteAsync(Guid id, Guid userId, bool isAdmin, CancellationToken cancellationToken);
}
