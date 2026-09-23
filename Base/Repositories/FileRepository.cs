using Base.Data;
using Base.Entities;
using Base.Repositories.IRepositories;

namespace Base.Repositories;

public class FileRepository(AppDbContext dbContext) : IFileRepository
{
    public async Task AddAsync(UploadedFile file, CancellationToken cancellationToken)
    {
        dbContext.UploadedFiles.Add(file);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}