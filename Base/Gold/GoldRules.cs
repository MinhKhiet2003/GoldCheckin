using Base.Exceptions;

namespace Base.Gold;

public static class GoldRules
{
    public static DateTime LocalNow(DateTime utc) => utc.AddHours(7);
    public static bool Weekend(DateOnly date) => date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
    public static double Distance(double lat1, double lon1, double lat2, double lon2)
    {
        var radians = Math.PI / 180;
        var a = Math.Pow(Math.Sin((lat2 - lat1) * radians / 2), 2)
            + Math.Cos(lat1 * radians) * Math.Cos(lat2 * radians) * Math.Pow(Math.Sin((lon2 - lon1) * radians / 2), 2);
        return 6371000 * 2 * Math.Atan2(Math.Sqrt(Math.Clamp(a, 0, 1)), Math.Sqrt(1 - Math.Clamp(a, 0, 1)));
    }
    public static (string Status, int Minutes) Classify(string type, DateTime local, WorkShift shift, AttendancePolicy policy)
    {
        var date = DateOnly.FromDateTime(local);
        var start = date.ToDateTime(shift.Start);
        var end = date.ToDateTime(shift.End);
        if (type == "in")
        {
            var minutes = (int)Math.Ceiling((local - start).TotalMinutes);
            if (minutes > policy.LateMinutes) return ("late", minutes);
            if (-minutes > policy.EarlyCheckinMinutes) return ("early-checkin", -minutes);
        }
        else
        {
            var minutes = (int)Math.Ceiling((end - local).TotalMinutes);
            if (minutes > policy.EarlyLeaveMinutes) return ("early-leave", minutes);
        }
        return ("normal", 0);
    }
    public static string CombineLocationStatus(string status, bool offsite) =>
        !offsite ? status : status == "normal" ? "offsite" : $"{status}-offsite";
    public static IEnumerable<DateOnly> Dates(DateOnly from, DateOnly to)
    {
        if (to < from || to.DayNumber - from.DayNumber > 365)
            throw new ApiException(422, "Khoảng ngày không hợp lệ hoặc vượt quá 366 ngày.");
        for (var date = from; date <= to; date = date.AddDays(1)) yield return date;
    }
}
