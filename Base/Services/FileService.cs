using Base.Common;
using Base.Entities;
using Base.Exceptions;
using Base.Models.Responses;
using Base.Repositories.IRepositories;
using Base.Services.IServices;

namespace Base.Services;

public class FileService(IWebHostEnvironment environment, IFileRepository repository, AuditContext audit) : IFileService
{
    public const long MaxFileSize = 10 * 1024 * 1024;

    public async Task<UploadFileResponse> UploadAsync(IFormFile file, Guid userId, CancellationToken cancellationToken)
    {
        if (file.Length == 0 || file.Length > MaxFileSize)
        {
            throw new ApiException(400, "File phải có dữ liệu và không vượt quá 10 MB.");
        }

        var originalName = Path.GetFileName(file.FileName.Replace('\\', '/'));
        var extension = Path.GetExtension(originalName).ToLowerInvariant();
        var contentType = extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".pdf" => "application/pdf",
            ".txt" => "text/plain",
            _ => throw new ApiException(400, "Chỉ hỗ trợ file JPG, JPEG, PNG, PDF và TXT.")
        };

        if (originalName.Length > 255)
        {
            throw new ApiException(400, "Tên file không được vượt quá 255 ký tự.");
        }

        await using var input = file.OpenReadStream();
        var header = new byte[8];
        var headerLength = await input.ReadAtLeastAsync(header, header.Length, false, cancellationToken);
        var validHeader = extension switch
        {
            ".png" => headerLength == 8 && header.AsSpan().SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }),
            ".jpg" or ".jpeg" => headerLength >= 3 && header[0] == 255 && header[1] == 216 && header[2] == 255,
            ".pdf" => headerLength >= 5 && header.AsSpan(0, 5).SequenceEqual("%PDF-"u8),
            ".txt" => true,
            _ => false
        };

        if (!validHeader)
        {
            throw new ApiException(400, "Nội dung file không khớp với định dạng file.");
        }

        var id = Guid.NewGuid();
        var storedName = id.ToString("N") + extension;
        var directory = Path.Combine(environment.WebRootPath, "uploads");
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, storedName);
        var created = false;

        try
        {
            await using (var output = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, true))
            {
                created = true;
                await output.WriteAsync(header.AsMemory(0, headerLength), cancellationToken);
                var buffer = new byte[81920];
                long total = headerLength;
                int count;

                while ((count = await input.ReadAsync(buffer, cancellationToken)) > 0)
                {
                    total += count;

                    if (total > MaxFileSize)
                    {
                        throw new ApiException(400, "File không được vượt quá 10 MB.");
                    }

                    await output.WriteAsync(buffer.AsMemory(0, count), cancellationToken);
                }
            }

            var entity = new UploadedFile
            {
                Id = id,
                UploadedBy = userId,
                OriginalName = originalName,
                StoredName = storedName,
                Url = "/uploads/" + storedName,
                ContentType = contentType,
                Size = new FileInfo(path).Length,
                CreatedAt = DateTime.UtcNow
            };

            await repository.AddAsync(entity, cancellationToken);

            var response = new UploadFileResponse
            {
                Id = entity.Id,
                OriginalName = entity.OriginalName,
                Url = entity.Url,
                ContentType = entity.ContentType,
                Size = entity.Size,
                CreatedAt = entity.CreatedAt
            };

            audit.Begin("Upload", "UploadedFile", entity.Id);
            audit.Complete(response);
            return response;
        }
        catch
        {
            if (created)
            {
                File.Delete(path);
            }

            throw;
        }
    }
}