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
    public const string ReadSystemLogs = "system-logs.read";

    public static readonly string[] All =
    [
        ReadOwnProfile,
        UpdateOwnProfile,
        ListUsers,
        ReadUser,
        UpdateUser,
        DeleteUser,
        UploadFiles,
        ReadSystemLogs
    ];
}