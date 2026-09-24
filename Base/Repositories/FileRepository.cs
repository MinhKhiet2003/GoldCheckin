using Base.Data;
using Base.Entities;
using Base.Repositories.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace Base.Repositories;

public class FileRepository(AppDbContext dbContext) : IFileRepository
{
    public async Task AddAsync(UploadedFile file, CancellationToken cancellationToken)
    {
        dbContext.UploadedFiles.Add(file);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<UploadedFile?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        return dbContext.UploadedFiles.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
    }

    public void Delete(UploadedFile file)
    {
        dbContext.UploadedFiles.Remove(file);
    }

    public Task SaveAsync(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
