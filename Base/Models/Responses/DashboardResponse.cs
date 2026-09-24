namespace Base.Models.Responses;

public class DashboardResponse
{
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int LockedUsers { get; set; }
    public int LoginsLast7Days { get; set; }
    public IEnumerable<UserResponse> RecentLogins { get; set; } = [];
}
