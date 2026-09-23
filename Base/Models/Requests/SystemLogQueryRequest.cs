using System.ComponentModel.DataAnnotations;

namespace Base.Models.Requests;

public class SystemLogQueryRequest : IValidatableObject
{
    [Range(1, int.MaxValue, ErrorMessage = "Số trang phải lớn hơn 0.")]
    public int PageNumber { get; set; } = 1;

    [Range(1, 100, ErrorMessage = "Kích thước trang phải từ 1 đến 100.")]
    public int PageSize { get; set; } = 20;

    public Guid? UserId { get; set; }

    [Range(100, 599, ErrorMessage = "Mã trạng thái HTTP không hợp lệ.")]
    public int? StatusCode { get; set; }

    public DateTimeOffset? From { get; set; }
    public DateTimeOffset? To { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (From > To)
        {
            yield return new ValidationResult("Thời gian bắt đầu không được lớn hơn thời gian kết thúc.", [nameof(From)]);
        }
    }
}