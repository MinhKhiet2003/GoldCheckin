using System.Text.Json;

namespace Base.Models.Responses;

public class SystemLogDetailResponse : SystemLogResponse
{
    public JsonElement? ResponseData { get; set; }
    public JsonElement? BeforeData { get; set; }
    public JsonElement? AfterData { get; set; }
}