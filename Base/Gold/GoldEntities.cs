using System.ComponentModel.DataAnnotations;

namespace Base.Gold;

public class Department
{
    public Guid Id { get; set; }
    [MaxLength(150)] public string Name { get; set; } = "";
    [MaxLength(500)] public string Description { get; set; } = "";
}
public class Employee
{
    public Guid Id { get; set; }
    public Guid DepartmentId { get; set; }
    public DateOnly? EmploymentStartDate { get; set; }
    [MaxLength(40)] public string Code { get; set; } = "";
    [MaxLength(100)] public string Position { get; set; } = "";
    [MaxLength(20)] public string GoldRole { get; set; } = "employee";
    public decimal AnnualLeaveBalance { get; set; }
}
public class WorkLocation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    [MaxLength(150)] public string Name { get; set; } = "";
    [MaxLength(500)] public string Address { get; set; } = "";
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public int RadiusMeters { get; set; } = 100;
    public bool Active { get; set; } = true;
}
public class WorkShift
{
    public Guid Id { get; set; } = Guid.NewGuid();
    [MaxLength(100)] public string Name { get; set; } = "";
    public TimeOnly Start { get; set; }
    public TimeOnly End { get; set; }
}
public class WorkSchedule
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EmployeeId { get; set; }
    public DateOnly Date { get; set; }
    public Guid? ShiftId { get; set; }
    public Guid? LocationId { get; set; }
    [MaxLength(20)] public string Kind { get; set; } = "work";
    public bool Locked { get; set; }
    public int Version { get; set; }
}
public class AttendancePolicy
{
    public int Id { get; set; } = 1;
    public int RegularDays { get; set; } = 4;
    public int AnnualDays { get; set; } = 1;
    public int LateMinutes { get; set; } = 5;
    public int EarlyLeaveMinutes { get; set; } = 5;
    public int EarlyCheckinMinutes { get; set; } = 15;
    public int DeadlineDay { get; set; } = 25;
    public bool AllowOffsite { get; set; }
    public bool WeekendRestricted { get; set; } = true;
    public int MaxGpsAccuracyMeters { get; set; } = 50;
    public int OvertimeAfterMinutes { get; set; } = 0;
}
public class StaffingRule
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DepartmentId { get; set; }
    [MaxLength(100)] public string Position { get; set; } = "";
    public int WeekdayMinimum { get; set; }
    public int WeekendMinimum { get; set; }
}
public class PeakDay
{
    public DateOnly Date { get; set; }
    [MaxLength(200)] public string Reason { get; set; } = "";
    public bool BlockLeave { get; set; }
}
public class AttendanceRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EmployeeId { get; set; }
    public DateOnly Date { get; set; }
    public Guid ShiftId { get; set; }
    public Guid LocationId { get; set; }
    public DateTime RecordedAt { get; set; }
    [MaxLength(10)] public string Type { get; set; } = "in";
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double DistanceMeters { get; set; }
    public double GpsAccuracy { get; set; }
    public double FaceConfidence { get; set; }
    [MaxLength(100)] public string VerificationId { get; set; } = "";
    [MaxLength(200)] public string Device { get; set; } = "";
    [MaxLength(64)] public string Ip { get; set; } = "";
    [MaxLength(80)] public string Status { get; set; } = "normal";
    public int DeviationMinutes { get; set; }
    [MaxLength(2000)] public string Reason { get; set; } = "";
}
public class GoldRequest
{
    public Guid? ReplacesRequestId { get; set; }
    public bool IsCancellation { get; set; }
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EmployeeId { get; set; }
    [MaxLength(30)] public string Type { get; set; } = "leave";
    [MaxLength(30)] public string LeaveKind { get; set; } = "annual";
    public DateOnly From { get; set; }
    public DateOnly To { get; set; }
    [MaxLength(20)] public string LeaveSession { get; set; } = "full";
    public TimeOnly LeaveStart { get; set; } = new(7, 30);
    public TimeOnly LeaveEnd { get; set; } = new(17, 0);
    public Guid? PartnerId { get; set; }
    public DateOnly? PartnerDate { get; set; }
    public Guid? AttendanceId { get; set; }
    public TimeOnly? ActualTime { get; set; }
    [MaxLength(10)] public string? AttendanceType { get; set; }
    [MaxLength(2000)] public string Reason { get; set; } = "";
    [MaxLength(2000)] public string Handover { get; set; } = "";
    public Guid? HandoverEmployeeId { get; set; }
    public Guid? AttachmentId { get; set; }
    [MaxLength(30)] public string Status { get; set; } = "manager";
    [MaxLength(2000)] public string ReviewNote { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedAt { get; set; }
    public Guid? ReviewerId { get; set; }
    public int Version { get; set; }
    public decimal PaidLeaveDays { get; set; }
    public decimal UnpaidLeaveDays { get; set; }
}
