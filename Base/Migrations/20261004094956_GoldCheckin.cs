using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Base.Migrations
{
    /// <inheritdoc />
    public partial class GoldCheckin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GoldDepartment",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoldDepartment", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GoldLocation",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Latitude = table.Column<double>(type: "double precision", nullable: false),
                    Longitude = table.Column<double>(type: "double precision", nullable: false),
                    RadiusMeters = table.Column<int>(type: "integer", nullable: false),
                    Active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoldLocation", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GoldPayrollLock",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    LockedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LockedBy = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoldPayrollLock", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GoldPeakDay",
                columns: table => new
                {
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Reason = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    BlockLeave = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoldPeakDay", x => x.Date);
                });

            migrationBuilder.CreateTable(
                name: "GoldPolicy",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RegularDays = table.Column<int>(type: "integer", nullable: false),
                    AnnualDays = table.Column<int>(type: "integer", nullable: false),
                    LateMinutes = table.Column<int>(type: "integer", nullable: false),
                    EarlyLeaveMinutes = table.Column<int>(type: "integer", nullable: false),
                    EarlyCheckinMinutes = table.Column<int>(type: "integer", nullable: false),
                    DeadlineDay = table.Column<int>(type: "integer", nullable: false),
                    AllowOffsite = table.Column<bool>(type: "boolean", nullable: false),
                    WeekendRestricted = table.Column<bool>(type: "boolean", nullable: false),
                    MaxGpsAccuracyMeters = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoldPolicy", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GoldShift",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Start = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    End = table.Column<TimeOnly>(type: "time without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoldShift", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GoldEmployee",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Position = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    GoldRole = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoldEmployee", x => x.Id);
                    table.CheckConstraint("CK_GoldRole", "\"GoldRole\" IN ('employee','manager','hr')");
                    table.ForeignKey(
                        name: "FK_GoldEmployee_GoldDepartment_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "GoldDepartment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GoldEmployee_User_Id",
                        column: x => x.Id,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GoldStaffingRule",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Position = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    WeekdayMinimum = table.Column<int>(type: "integer", nullable: false),
                    WeekendMinimum = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoldStaffingRule", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GoldStaffingRule_GoldDepartment_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "GoldDepartment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GoldAttendance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    ShiftId = table.Column<Guid>(type: "uuid", nullable: false),
                    LocationId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecordedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Type = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Latitude = table.Column<double>(type: "double precision", nullable: false),
                    Longitude = table.Column<double>(type: "double precision", nullable: false),
                    DistanceMeters = table.Column<double>(type: "double precision", nullable: false),
                    GpsAccuracy = table.Column<double>(type: "double precision", nullable: false),
                    FaceConfidence = table.Column<double>(type: "double precision", nullable: false),
                    VerificationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Device = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Ip = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Status = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    DeviationMinutes = table.Column<int>(type: "integer", nullable: false),
                    Reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoldAttendance", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GoldAttendance_GoldEmployee_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "GoldEmployee",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GoldAttendance_GoldLocation_LocationId",
                        column: x => x.LocationId,
                        principalTable: "GoldLocation",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GoldAttendance_GoldShift_ShiftId",
                        column: x => x.ShiftId,
                        principalTable: "GoldShift",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GoldSchedule",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    ShiftId = table.Column<Guid>(type: "uuid", nullable: true),
                    LocationId = table.Column<Guid>(type: "uuid", nullable: true),
                    Kind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Locked = table.Column<bool>(type: "boolean", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoldSchedule", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GoldSchedule_GoldEmployee_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "GoldEmployee",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GoldSchedule_GoldLocation_LocationId",
                        column: x => x.LocationId,
                        principalTable: "GoldLocation",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GoldSchedule_GoldShift_ShiftId",
                        column: x => x.ShiftId,
                        principalTable: "GoldShift",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GoldRequest",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    LeaveKind = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    From = table.Column<DateOnly>(type: "date", nullable: false),
                    To = table.Column<DateOnly>(type: "date", nullable: false),
                    PartnerId = table.Column<Guid>(type: "uuid", nullable: true),
                    PartnerDate = table.Column<DateOnly>(type: "date", nullable: true),
                    AttendanceId = table.Column<Guid>(type: "uuid", nullable: true),
                    ActualTime = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    AttendanceType = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    Reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Handover = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    HandoverEmployeeId = table.Column<Guid>(type: "uuid", nullable: true),
                    AttachmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ReviewNote = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReviewerId = table.Column<Guid>(type: "uuid", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoldRequest", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GoldRequest_GoldAttendance_AttendanceId",
                        column: x => x.AttendanceId,
                        principalTable: "GoldAttendance",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GoldRequest_GoldEmployee_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "GoldEmployee",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GoldRequest_GoldEmployee_HandoverEmployeeId",
                        column: x => x.HandoverEmployeeId,
                        principalTable: "GoldEmployee",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GoldRequest_GoldEmployee_PartnerId",
                        column: x => x.PartnerId,
                        principalTable: "GoldEmployee",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GoldRequest_UploadedFile_AttachmentId",
                        column: x => x.AttachmentId,
                        principalTable: "UploadedFile",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "GoldDepartment",
                columns: new[] { "Id", "Description", "Name" },
                values: new object[,]
                {
                    { new Guid("10000000-0000-0000-0000-000000000001"), "", "BP Da liễu thẩm mỹ" },
                    { new Guid("10000000-0000-0000-0000-000000000002"), "", "BP Phục hồi chức năng - Y học cổ truyền" },
                    { new Guid("10000000-0000-0000-0000-000000000003"), "", "BP Tiêm chủng" },
                    { new Guid("10000000-0000-0000-0000-000000000004"), "", "Nhà thuốc" },
                    { new Guid("10000000-0000-0000-0000-000000000005"), "Nhân sự, bảo vệ, vệ sinh", "Phòng Hành chính - tổng hợp" },
                    { new Guid("10000000-0000-0000-0000-000000000006"), "Bộ phận Kinh doanh và Marketing", "Phòng Kinh doanh và tăng trưởng" },
                    { new Guid("10000000-0000-0000-0000-000000000007"), "", "Phòng Tài chính - kế toán" }
                });

            migrationBuilder.InsertData(
                table: "GoldLocation",
                columns: new[] { "Id", "Active", "Address", "Latitude", "Longitude", "Name", "RadiusMeters" },
                values: new object[] { new Guid("30000000-0000-0000-0000-000000000001"), true, "Gold Ninh Bình · vị trí theo Google Maps do đơn vị cung cấp", 20.2511546, 105.96310320000001, "Trung tâm tiêm chủng quốc tế Gold", 100 });

            migrationBuilder.InsertData(
                table: "GoldPolicy",
                columns: new[] { "Id", "AllowOffsite", "AnnualDays", "DeadlineDay", "EarlyCheckinMinutes", "EarlyLeaveMinutes", "LateMinutes", "MaxGpsAccuracyMeters", "RegularDays", "WeekendRestricted" },
                values: new object[] { 1, false, 1, 25, 15, 5, 5, 50, 4, true });

            migrationBuilder.InsertData(
                table: "GoldShift",
                columns: new[] { "Id", "End", "Name", "Start" },
                values: new object[,]
                {
                    { new Guid("20000000-0000-0000-0000-000000000001"), new TimeOnly(17, 0, 0), "Ca hành chính", new TimeOnly(7, 30, 0) },
                    { new Guid("20000000-0000-0000-0000-000000000002"), new TimeOnly(12, 0, 0), "Ca sáng", new TimeOnly(7, 30, 0) },
                    { new Guid("20000000-0000-0000-0000-000000000003"), new TimeOnly(17, 0, 0), "Ca chiều", new TimeOnly(13, 0, 0) },
                    { new Guid("20000000-0000-0000-0000-000000000004"), new TimeOnly(21, 0, 0), "Ca tối", new TimeOnly(17, 0, 0) }
                });

            migrationBuilder.CreateIndex(
                name: "IX_GoldAttendance_EmployeeId_Date_Type",
                table: "GoldAttendance",
                columns: new[] { "EmployeeId", "Date", "Type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GoldAttendance_LocationId",
                table: "GoldAttendance",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_GoldAttendance_ShiftId",
                table: "GoldAttendance",
                column: "ShiftId");

            migrationBuilder.CreateIndex(
                name: "IX_GoldAttendance_VerificationId",
                table: "GoldAttendance",
                column: "VerificationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GoldEmployee_Code",
                table: "GoldEmployee",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GoldEmployee_DepartmentId",
                table: "GoldEmployee",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_GoldRequest_AttachmentId",
                table: "GoldRequest",
                column: "AttachmentId");

            migrationBuilder.CreateIndex(
                name: "IX_GoldRequest_AttendanceId",
                table: "GoldRequest",
                column: "AttendanceId");

            migrationBuilder.CreateIndex(
                name: "IX_GoldRequest_EmployeeId_From_Status",
                table: "GoldRequest",
                columns: new[] { "EmployeeId", "From", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_GoldRequest_HandoverEmployeeId",
                table: "GoldRequest",
                column: "HandoverEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_GoldRequest_PartnerId",
                table: "GoldRequest",
                column: "PartnerId");

            migrationBuilder.CreateIndex(
                name: "IX_GoldSchedule_EmployeeId_Date",
                table: "GoldSchedule",
                columns: new[] { "EmployeeId", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GoldSchedule_LocationId",
                table: "GoldSchedule",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_GoldSchedule_ShiftId",
                table: "GoldSchedule",
                column: "ShiftId");

            migrationBuilder.CreateIndex(
                name: "IX_GoldStaffingRule_DepartmentId_Position",
                table: "GoldStaffingRule",
                columns: new[] { "DepartmentId", "Position" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GoldPayrollLock");

            migrationBuilder.DropTable(
                name: "GoldPeakDay");

            migrationBuilder.DropTable(
                name: "GoldPolicy");

            migrationBuilder.DropTable(
                name: "GoldRequest");

            migrationBuilder.DropTable(
                name: "GoldSchedule");

            migrationBuilder.DropTable(
                name: "GoldStaffingRule");

            migrationBuilder.DropTable(
                name: "GoldAttendance");

            migrationBuilder.DropTable(
                name: "GoldEmployee");

            migrationBuilder.DropTable(
                name: "GoldLocation");

            migrationBuilder.DropTable(
                name: "GoldShift");

            migrationBuilder.DropTable(
                name: "GoldDepartment");
        }
    }
}
