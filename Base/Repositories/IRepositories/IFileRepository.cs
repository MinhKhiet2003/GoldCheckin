using Base.Entities;

namespace Base.Repositories.IRepositories;

public interface IFileRepository
{
    Task AddAsync(UploadedFile file, CancellationToken cancellationToken);
}