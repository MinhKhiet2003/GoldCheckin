namespace Base.Common;

public static class SystemLogActions
{
    public static readonly string[] DataChanges =
    [
        "Create", "Update", "Edit", "Delete", "AssignRole", "RegisterFace",
        "Lock", "BatchSchedule", "Review", "Upload", "Import", "ChangePassword", "ResetPassword"
    ];

    public static bool IsDataChange(string? action) => action is not null && DataChanges.Contains(action, StringComparer.Ordinal);
}
