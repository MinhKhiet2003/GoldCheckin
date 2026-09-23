using System.ComponentModel.DataAnnotations;

namespace Base.Models.Requests;

public class UploadFileRequest
{
    [Required(ErrorMessage = "Vui lòng chọn file cần tải lên.")]
    public IFormFile File { get; set; } = null!;
}