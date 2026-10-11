using System.ComponentModel.DataAnnotations;

namespace Base.Gold;

public class EmployeeCreateInput : Base.Models.Requests.RegisterRequest
{
    [Required, MaxLength(40)] public string Code { get; set; } = "";
    public Guid DepartmentId { get; set; }
    [Required, MaxLength(100)] public string Position { get; set; } = "";
    [RegularExpression("employee|manager|hr")] public string GoldRole { get; set; } = "employee";
    [Range(0, 365)] public decimal AnnualLeaveBalance { get; set; }
    public bool IsActive { get; set; } = true;
    [Required] public DateOnly? EmploymentStartDate { get; set; }
}

public record EmployeeInput([Required, MaxLength(40)] string Code, Guid DepartmentId,
    [Required, MaxLength(100)] string Position, [RegularExpression("employee|manager|hr")] string GoldRole,
    [Range(0, 365)] decimal? AnnualLeaveBalance = null, bool? IsActive = null,
    DateOnly? DateOfBirth = null, [Required] DateOnly? EmploymentStartDate = null);
public record FaceEnrollmentInput([Required, MaxLength(7_000_000)] string ImageBase64);
public record LocationInput([Required, MaxLength(150)] string Name, [Required, MaxLength(500)] string Address,
    [Range(-90, 90)] double Latitude, [Range(-180, 180)] double Longitude, [Range(10, 1000)] int RadiusMeters, bool Active);
public record ShiftInput([Required, MaxLength(100)] string Name, TimeOnly Start, TimeOnly End,
    [RegularExpression("morning|afternoon|evening")] string? Period = null);
public record DepartmentShiftInput([MaxLength(10)] Guid[]? MorningShiftIds, [MaxLength(10)] Guid[]? AfternoonShiftIds,
    [MaxLength(10)] Guid[]? EveningShiftIds);
public record DepartmentInput([Required, MaxLength(150)] string Name, [MaxLength(500)] string Description = "");
public record ScheduleInput(Guid EmployeeId, DateOnly Date, Guid? ShiftId, Guid? LocationId,
    [RegularExpression("work|off")] string Kind);
public record ScheduleBatchInput(Guid EmployeeId, DateOnly From, DateOnly To, Guid ShiftId, Guid LocationId,
    [Required, MinLength(1), MaxLength(7)] DayOfWeek[] WorkingDays);
public record PolicyInput([Range(0, 15)] int RegularDays, [Range(0, 31)] int AnnualDays,
    [Range(0, 120)] int LateMinutes, [Range(0, 120)] int EarlyLeaveMinutes,
    [Range(0, 120)] int EarlyCheckinMinutes, [Range(1, 28)] int DeadlineDay,
    bool AllowOffsite, bool WeekendRestricted, [Range(1, 200)] int MaxGpsAccuracyMeters,
    [Range(0, 240)] int OvertimeAfterMinutes = 0);
public record PunchInput([RegularExpression("in|out")] string Type, [Range(-90, 90)] double Latitude,
    [Range(-180, 180)] double Longitude, [Range(0, 10000)] double GpsAccuracy,
    [Required, MaxLength(200)] string Device, [Required, MaxLength(7_000_000)] string ImageBase64,
    [MaxLength(2000)] string Reason = "");
public record SupplementPunchInput(TimeOnly Time, [Required, RegularExpression("in|out")] string Type);
public record SupplementAttendanceCorrectionInput(DateOnly Date,
    [Required, MinLength(1), MaxLength(4)] SupplementPunchInput[] Punches, int Version);
public record RequestInput([Required, RegularExpression("leave|regular|swap|explanation|supplement")] string Type,
    [Required, RegularExpression("annual|unpaid|sick|personal|marriage|bereavement|maternity|other")] string LeaveKind,
    DateOnly From, DateOnly To, [MaxLength(2000)] string Reason = "",
    Guid? PartnerId = null, DateOnly? PartnerDate = null, Guid? AttendanceId = null,
    TimeOnly? ActualTime = null, [RegularExpression("in|out")] string? AttendanceType = null,
    Guid? HandoverEmployeeId = null, [MaxLength(2000)] string Handover = "", Guid? AttachmentId = null,
    [RegularExpression("morning|afternoon|full")] string LeaveSession = "full",
    [RegularExpression("morning|afternoon|full")] string EndLeaveSession = "full",
    [MaxLength(4)] SupplementPunchInput[]? SupplementPunches = null);
public record LeaveRequestEditInput(DateOnly From, DateOnly To,
    [RegularExpression("annual|unpaid|sick|personal|marriage|bereavement|maternity|other")] string LeaveKind,
    [RegularExpression("morning|afternoon|full")] string LeaveSession,
    [RegularExpression("morning|afternoon|full")] string EndLeaveSession,
    [Required, MinLength(5), MaxLength(2000)] string Reason,
    Guid? HandoverEmployeeId, [MaxLength(2000)] string Handover, int Version);
public record RegularLeaveInput([Required, MinLength(1), MaxLength(31)] DateOnly[] Dates,
    [RegularExpression("morning|afternoon|full")] string LeaveSession = "full",
    [MaxLength(2000)] string Reason = "", Guid? HandoverEmployeeId = null, [MaxLength(2000)] string Handover = "");
public record RegularLeaveEditInput(DateOnly From, DateOnly To, int Version,
    [RegularExpression("morning|afternoon|full")] string LeaveSession = "full",
    [MaxLength(2000)] string Reason = "", Guid? HandoverEmployeeId = null, [MaxLength(2000)] string Handover = "");
public record ReviewInput([RegularExpression("approve|reject|cancel|accept")] string Action,
    [MaxLength(2000)] string Note = "", DateOnly? ActualDate = null, TimeOnly? ActualTime = null);
public record StaffingInput(Guid DepartmentId, [Required, MaxLength(100)] string Position,
    [Range(0, 500)] int WeekdayMinimum, [Range(0, 500)] int WeekendMinimum);
public record PeakInput(DateOnly Date, [Required, MaxLength(200)] string Reason, bool BlockLeave);
public record EmployeeResponse(Guid Id, string Name, string Code, Guid DepartmentId, string Department,
    string Position, string GoldRole, decimal AnnualLeaveBalance, DateOnly? DateOfBirth, bool IsActive,
    DateOnly? EmploymentStartDate, bool HasAvatar = false, string? AvatarVersion = null,
    bool FaceEnrolled = false, DateTime? FaceRegisteredAt = null, string? Username = null);
public sealed class EmployeeImportResult
{
    public int TotalRows { get; set; }
    public int ImportedCount { get; set; }
    public List<EmployeeImportIssue> Errors { get; } = [];
    public List<EmployeeImportIssue> Duplicates { get; } = [];
    public int InvalidCount => Errors.Count;
    public int DuplicateCount => Duplicates.Count;
}
public record EmployeeImportIssue(int Row, string Message);
public record ScheduleResponse(Guid Id, Guid EmployeeId, string Name, DateOnly Date, Guid? ShiftId,
    string? Shift, string? Start, string? End, Guid? LocationId, string? Location, string Kind, bool Locked);
public record AttendanceResponse(Guid Id, Guid EmployeeId, string Name, string Department, DateOnly Date,
    DateTime RecordedAt, string Type, string Status, int DeviationMinutes, double DistanceMeters,
    double GpsAccuracy, string Reason);
public record RequestResponse(Guid Id, Guid EmployeeId, string Name, string Department, string Type,
    string LeaveKind, DateOnly From, DateOnly To, string Reason, string Handover, string Status,
    Guid? PartnerId, string? PartnerName, string ReviewNote, DateTime CreatedAt, Guid? ReplacesRequestId, bool IsCancellation, Guid DepartmentId,
    string LeaveSession, string LeaveStart, string LeaveEnd, decimal PaidLeaveDays, decimal UnpaidLeaveDays);
public record CapacityWarning(DateOnly Date, string Position, int Available, int Minimum);
