using Base.Entities;
using Microsoft.EntityFrameworkCore;

namespace Base.Gold;

public static class GoldModel
{
    public static readonly Guid[] DepartmentIds = Enumerable.Range(1, 7)
        .Select(i => Guid.Parse($"10000000-0000-0000-0000-{i:000000000000}")).ToArray();
    public static void Configure(ModelBuilder b)
    {
        b.Entity<Department>().ToTable("GoldDepartment");
        b.Entity<Department>().HasData(new[] {
            "BP Da liễu thẩm mỹ", "BP Phục hồi chức năng - Y học cổ truyền", "BP Tiêm chủng", "Nhà thuốc",
            "Phòng Hành chính - tổng hợp", "Phòng Kinh doanh và tăng trưởng", "Phòng Tài chính - kế toán"
        }.Select((name, i) => new Department
        {
            Id = DepartmentIds[i],
            Name = name,
            Description = i == 4 ? "Nhân sự, bảo vệ, vệ sinh" : i == 5 ? "Bộ phận Kinh doanh và Marketing" : ""
        }));
        b.Entity<Employee>().ToTable("GoldEmployee");
        b.Entity<Employee>().Property(e => e.AnnualLeaveBalance).HasPrecision(6, 2);
        b.Entity<Employee>().HasOne<Account>().WithOne().HasForeignKey<Employee>(e => e.Id).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Employee>().HasOne<Department>().WithMany().HasForeignKey(e => e.DepartmentId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Employee>().HasIndex(e => e.Code).IsUnique();
        b.Entity<Employee>().ToTable(t => t.HasCheckConstraint("CK_GoldRole", "\"GoldRole\" IN ('employee','manager','hr')"));
        b.Entity<WorkLocation>().ToTable("GoldLocation");
        b.Entity<WorkLocation>().HasData(new WorkLocation
        {
            Id = Guid.Parse("30000000-0000-0000-0000-000000000001"),
            Name = "Trung tâm tiêm chủng quốc tế Gold",
            Address = "Gold Ninh Bình · vị trí theo Google Maps do đơn vị cung cấp",
            Latitude = 20.2511546,
            Longitude = 105.9631032,
            RadiusMeters = 100,
            Active = true
        });
        b.Entity<WorkShift>().ToTable("GoldShift");
        b.Entity<WorkShift>().HasData(
            new WorkShift { Id = Guid.Parse("20000000-0000-0000-0000-000000000001"), Name = "Ca hành chính", Start = new(7, 30), End = new(17, 0) },
            new WorkShift { Id = Guid.Parse("20000000-0000-0000-0000-000000000002"), Name = "Ca sáng", Start = new(7, 30), End = new(12, 0) },
            new WorkShift { Id = Guid.Parse("20000000-0000-0000-0000-000000000003"), Name = "Ca chiều", Start = new(13, 0), End = new(17, 0) },
            new WorkShift { Id = Guid.Parse("20000000-0000-0000-0000-000000000004"), Name = "Ca tối", Start = new(17, 0), End = new(21, 0) });
        b.Entity<AttendancePolicy>().ToTable("GoldPolicy");
        b.Entity<AttendancePolicy>().HasData(new AttendancePolicy());
        b.Entity<PayrollLock>().ToTable("GoldPayrollLock");
        b.Entity<ScheduleLock>().ToTable("GoldScheduleLock");
        b.Entity<WorkSchedule>().ToTable("GoldSchedule");
        b.Entity<WorkSchedule>().HasIndex(s => new { s.EmployeeId, s.Date }).IsUnique();
        b.Entity<WorkSchedule>().Property(s => s.Version).IsConcurrencyToken();
        b.Entity<WorkSchedule>().HasOne<Employee>().WithMany().HasForeignKey(s => s.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<WorkSchedule>().HasOne<WorkShift>().WithMany().HasForeignKey(s => s.ShiftId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<WorkSchedule>().HasOne<WorkLocation>().WithMany().HasForeignKey(s => s.LocationId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<StaffingRule>().ToTable("GoldStaffingRule");
        b.Entity<StaffingRule>().HasIndex(s => new { s.DepartmentId, s.Position }).IsUnique();
        b.Entity<StaffingRule>().HasOne<Department>().WithMany().HasForeignKey(s => s.DepartmentId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<PeakDay>().ToTable("GoldPeakDay").HasKey(p => p.Date);
        b.Entity<AttendanceRecord>().ToTable("GoldAttendance");
        b.Entity<AttendanceRecord>().HasIndex(r => new { r.EmployeeId, r.Date, r.Type }).IsUnique();
        b.Entity<AttendanceRecord>().HasIndex(r => r.VerificationId).IsUnique();
        b.Entity<AttendanceRecord>().HasOne<Employee>().WithMany().HasForeignKey(r => r.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<AttendanceRecord>().HasOne<WorkShift>().WithMany().HasForeignKey(r => r.ShiftId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<AttendanceRecord>().HasOne<WorkLocation>().WithMany().HasForeignKey(r => r.LocationId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<GoldRequest>().ToTable("GoldRequest");
        b.Entity<GoldRequest>().Property(r => r.PaidLeaveDays).HasPrecision(6, 2);
        b.Entity<GoldRequest>().Property(r => r.UnpaidLeaveDays).HasPrecision(6, 2);
        b.Entity<GoldRequest>().HasIndex(r => new { r.EmployeeId, r.From, r.Status });
        b.Entity<GoldRequest>().Property(r => r.Version).IsConcurrencyToken();
        b.Entity<GoldRequest>().HasOne<Employee>().WithMany().HasForeignKey(r => r.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<GoldRequest>().HasOne<Employee>().WithMany().HasForeignKey(r => r.PartnerId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<GoldRequest>().HasOne<Employee>().WithMany().HasForeignKey(r => r.HandoverEmployeeId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<GoldRequest>().HasOne<AttendanceRecord>().WithMany().HasForeignKey(r => r.AttendanceId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<GoldRequest>().HasOne<UploadedFile>().WithMany().HasForeignKey(r => r.AttachmentId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<GoldRequest>().HasOne<GoldRequest>().WithMany().HasForeignKey(r => r.ReplacesRequestId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<GoldRequest>().ToTable(t => t.HasCheckConstraint("CK_GoldLeaveSession", "\"LeaveSession\" IN ('morning','afternoon','full')"));
    }
}
