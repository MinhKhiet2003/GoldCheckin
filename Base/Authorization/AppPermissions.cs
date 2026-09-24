namespace Base.Authorization;

public static class AppPermissions
{
    public const string ReadOwnProfile = "users.profile.read";
    public const string UpdateOwnProfile = "users.profile.update";
    public const string ListUsers = "users.list";
    public const string ReadUser = "users.read";
    public const string UpdateUser = "users.update";
    public const string DeleteUser = "users.delete";

    public const string UploadFiles = "files.upload";
    public const string ReadFiles = "files.read";
    public const string DeleteFiles = "files.delete";
    public const string ReadSystemLogs = "system-logs.read";
    public const string ManageRoles = "roles.manage";
    public const string ManagePermissions = "permissions.manage";
    public const string ReadNotifications = "notifications.read";
    public const string UpdateNotifications = "notifications.update";
    public const string ManageSettings = "settings.manage";
    public const string ReadDashboard = "dashboard.read";

    public static readonly string[] All =
    [
        ReadOwnProfile,
        UpdateOwnProfile,
        ListUsers,
        ReadUser,
        UpdateUser,
        DeleteUser,
        UploadFiles,
        ReadFiles,
        DeleteFiles,
        ReadSystemLogs,
        ManageRoles,
        ManagePermissions,
        ReadNotifications,
        UpdateNotifications,
        ManageSettings,
        ReadDashboard
    ];
}
