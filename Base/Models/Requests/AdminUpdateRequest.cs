using System.ComponentModel.DataAnnotations;

namespace Base.Models.Requests;

public class AdminUpdateRequest : ProfileRequest
{
    public bool IsActive { get; set; } = true;
}
