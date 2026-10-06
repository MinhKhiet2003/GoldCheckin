using Base.Data;
using Base.Entities;
using Microsoft.EntityFrameworkCore;

namespace Base.Gold;

public class AttendanceReminderWorker(IServiceScopeFactory scopes, ILogger<AttendanceReminderWorker> logger, TimeProvider clock) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (!await timer.WaitForNextTickAsync(stoppingToken)) break;
                await using var scope = scopes.CreateAsyncScope(); var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var now = GoldRules.LocalNow(clock.GetUtcNow().UtcDateTime); var date = DateOnly.FromDateTime(now);
                var schedules = await (from s in db.Set<WorkSchedule>()
                                       join shift in db.Set<WorkShift>() on s.ShiftId equals shift.Id
                                       join a in db.Accounts on s.EmployeeId equals a.Id
                                       where s.Date == date && s.Kind == "work" && a.IsActive
                                       select new { s.EmployeeId, shift.Start, shift.End }).ToListAsync(stoppingToken);
                var punches = await db.Set<AttendanceRecord>().Where(r => r.Date == date).Select(r => new { r.EmployeeId, r.Type }).ToListAsync(stoppingToken);
                foreach (var s in schedules)
                {
                    var start = date.ToDateTime(s.Start); var end = date.ToDateTime(s.End);
                    var entered = punches.Any(r => r.EmployeeId == s.EmployeeId && r.Type == "in"); var exited = punches.Any(r => r.EmployeeId == s.EmployeeId && r.Type == "out");
                    string? key = null; string? text = null;
                    if (!entered && now >= start.AddMinutes(-10) && now < start) { key = "Sắp đến giờ làm"; text = $"Ca làm bắt đầu lúc {s.Start:HH:mm}."; }
                    else if (!entered && now >= start.AddMinutes(15) && now < end) { key = "Bạn chưa chấm công vào"; text = "Vui lòng chấm công hoặc gửi đơn bổ sung nếu quên."; }
                    else if (entered && !exited && now >= end.AddMinutes(15)) { key = "Bạn chưa chấm công ra"; text = "Vui lòng kiểm tra giờ ra hoặc gửi đơn bổ sung."; }
                    if (key is null) continue;
                    var title = $"{key} · {date:dd/MM/yyyy}";
                    if (await db.Notifications.AnyAsync(n => n.UserId == s.EmployeeId && n.Title == title, stoppingToken)) continue;
                    db.Notifications.Add(new Notification { UserId = s.EmployeeId, Title = title, Content = text!, Link = "/#checkin" });
                }
                await db.SaveChangesAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex) { logger.LogWarning(ex, "Không tạo được nhắc chấm công."); }
        }
    }
}
