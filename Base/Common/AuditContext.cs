using System.Text.Json;

namespace Base.Common;

public class AuditContext
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public string? Action { get; private set; }
    public string? EntityType { get; private set; }
    public Guid? EntityId { get; private set; }
    public string? BeforeData { get; private set; }
    public string? AfterData { get; private set; }
    public string? ResponseData { get; private set; }

    // Chỉ truyền DTO an toàn, không truyền Entity chứa PasswordHash hoặc request chứa mật khẩu.
    public void Begin(string action, string entityType, Guid entityId, object? beforeData = null)
    {
        Action = action;
        EntityType = entityType;
        EntityId = entityId;
        BeforeData = Serialize(beforeData);
    }

    public void Complete(object? afterData)
    {
        AfterData = Serialize(afterData);
    }

    public void SetResponse(object? responseData)
    {
        ResponseData = Serialize(responseData);
    }

    private static string? Serialize(object? value)
    {
        return value is null ? null : JsonSerializer.Serialize(value, JsonOptions);
    }
}