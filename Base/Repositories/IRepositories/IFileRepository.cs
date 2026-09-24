using Base.Entities;

namespace Base.Repositories.IRepositories;

public interface IFileRepository
{
    Task AddAsync(UploadedFile file, CancellationToken cancellationToken);
    Task<UploadedFile?> GetAsync(Guid id, CancellationToken cancellationToken);
    void Delete(UploadedFile file);
    Task SaveAsync(CancellationToken cancellationToken);
}
