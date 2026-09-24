using System.ComponentModel.DataAnnotations;

namespace Base.Models.Requests;

public class NotificationQueryRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "Số trang phải lớn hơn 0.")]
    public int PageNumber { get; set; } = 1;

    [Range(1, 100, ErrorMessage = "Kích thước trang phải từ 1 đến 100.")]
    public int PageSize { get; set; } = 20;

    public bool? IsRead { get; set; }
}
