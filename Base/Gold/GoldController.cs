using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Security.Claims;
using Base.Models.Responses;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Base.Gold;

[ApiController]
[Route("api/gold")]
[Authorize]
[DebuggerNonUserCode]
public class GoldController(
    GoldService service,
    Base.Services.IServices.IAccountService accounts,
    IFaceEnrollment enrollment)
    : ControllerBase
{
    private Guid UserId =>
        Guid.Parse(User.FindFirstValue("sub")!);

    private bool Admin =>
        User.IsInRole("admin");

    private ActionResult Success(object? data = null)
    {
        return Ok(new ApiResponse<object>
        {
            Success = true,
            StatusCode = 200,
            Message = "Thành công.",
            Data = data
        });
    }

    [HttpGet("bootstrap")]
    public async Task<ActionResult> Bootstrap(CancellationToken ct)
    {
        return Success(
            await service.Bootstrap(UserId, Admin, ct));
    }

    [HttpGet("{kind:regex(^(schedule|attendance|requests)$)}")]
    public async Task<ActionResult> List(
        string kind,
        DateOnly from,
        DateOnly to,
        CancellationToken ct)
    {
        return Success(
            await service.List(UserId, Admin, kind, from, to, ct));
    }

    [HttpPut("employees/{id:guid}")]
    public async Task<ActionResult> Employee(
        Guid id,
        EmployeeInput x,
        CancellationToken ct)
    {
        return Success(
            await service.AssignEmployee(UserId, Admin, id, x, ct));
    }

    [HttpPost("employees")]
    public async Task<ActionResult> CreateEmployee(
        EmployeeCreateInput x,
        CancellationToken ct)
    {
        return Success(
            await service.CreateEmployee(
                UserId,
                Admin,
                x,
                accounts,
                ct));
    }

    [HttpGet("employees/import/template")]
    public IActionResult EmployeeImportTemplate()
    {
        return File(
            GoldService.CreateEmployeeImportTemplate(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "Mau-nhap-nhan-vien.xlsx");
    }

    [HttpPost("employees/import")]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<ActionResult> ImportEmployees(
        [FromForm] IFormFile file,
        CancellationToken ct)
    {
        return Success(
            await service.ImportEmployees(
                UserId,
                Admin,
                file,
                accounts,
                ct));
    }

    [HttpGet("employees/export.xlsx")]
    public async Task<IActionResult> ExportEmployees(
        CancellationToken ct)
    {
        return File(
            await service.ExportEmployees(UserId, Admin, ct),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"Danh-sach-nhan-vien-{DateOnly.FromDateTime(DateTime.Now):yyyy-MM-dd}.xlsx");
    }

    [HttpGet("employees/{id:guid}/avatar")]
    public async Task<IActionResult> EmployeeAvatar(
        Guid id,
        [FromServices] Base.Services.IServices.IFileService files,
        CancellationToken ct)
    {
        var fileId = await service.GetEmployeeAvatarFileId(
            UserId,
            Admin,
            id,
            ct);

        if (fileId is null)
            return NotFound();

        var file = await files.GetAsync(
            fileId.Value,
            id,
            false,
            ct);

        var environment =
            HttpContext.RequestServices
                .GetRequiredService<IWebHostEnvironment>();

        var path = Path.Combine(
            environment.WebRootPath,
            "uploads",
            file.StoredName);

        if (!System.IO.File.Exists(path))
            return NotFound();

        Response.Headers["Cache-Control"] =
            "no-store, no-cache, must-revalidate, private";

        Response.Headers["Pragma"] = "no-cache";

        Response.Headers["X-Content-Type-Options"] =
            "nosniff";

        return PhysicalFile(
            path,
            file.ContentType);
    }

    [HttpPost("employees/{id:guid}/face")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult> RegisterFace(
        Guid id,
        FaceEnrollmentInput x,
        CancellationToken ct)
    {
        return Success(
            await service.RegisterFace(
                UserId,
                Admin,
                id,
                x,
                enrollment,
                ct));
    }

    [HttpPost("locations")]
    public async Task<ActionResult> Location(
        LocationInput x,
        CancellationToken ct)
    {
        return Success(
            await service.SaveLocation(
                UserId,
                Admin,
                null,
                x,
                ct));
    }

    [HttpPut("locations/{id:guid}")]
    public async Task<ActionResult> Location(
        Guid id,
        LocationInput x,
        CancellationToken ct)
    {
        return Success(
            await service.SaveLocation(
                UserId,
                Admin,
                id,
                x,
                ct));
    }

    [HttpPost("shifts")]
    public async Task<ActionResult> Shift(
        ShiftInput x,
        CancellationToken ct)
    {
        return Success(
            await service.SaveShift(
                UserId,
                Admin,
                null,
                x,
                ct));
    }

    [HttpPut("shifts/{id:guid}")]
    public async Task<ActionResult> Shift(
        Guid id,
        ShiftInput x,
        CancellationToken ct)
    {
        return Success(
            await service.SaveShift(
                UserId,
                Admin,
                id,
                x,
                ct));
    }

    [HttpPut("policy")]
    public async Task<ActionResult> Policy(
        PolicyInput x,
        CancellationToken ct)
    {
        await service.SavePolicy(
            UserId,
            Admin,
            x,
            ct);

        return Success();
    }

    [HttpPost("schedule")]
    public async Task<ActionResult> Schedule(
        ScheduleInput x,
        CancellationToken ct)
    {
        return Success(
            await service.Schedule(
                UserId,
                Admin,
                x,
                ct));
    }

    [HttpPost("schedule/batch")]
    public async Task<ActionResult> BatchSchedule(
        ScheduleBatchInput x,
        CancellationToken ct)
    {
        await service.ScheduleBatch(
            UserId,
            Admin,
            x,
            ct);

        return Success();
    }

    [HttpPost("lock/{kind:regex(^(schedule|attendance)$)}")]
    public async Task<ActionResult> Lock(
        string kind,
        [Range(2000, 2100)] int year,
        [Range(1, 12)] int month,
        CancellationToken ct)
    {
        await service.Lock(
            UserId,
            Admin,
            year,
            month,
            kind,
            ct);

        return Success();
    }

    [HttpPost("punch")]
    public async Task<ActionResult> Punch(
        PunchInput x,
        CancellationToken ct)
    {
        var ipAddress =
            HttpContext.Connection.RemoteIpAddress?.ToString()
            ?? string.Empty;

        return Success(
            await service.Punch(
                UserId,
                Admin,
                x,
                ipAddress,
                ct));
    }

    [HttpPost("requests")]
    public async Task<ActionResult> CreateRequest(
        RequestInput x,
        CancellationToken ct)
    {
        return Success(
            await service.CreateRequest(
                UserId,
                Admin,
                x,
                ct));
    }

    [HttpPut("requests/{id:guid}")]
    public async Task<ActionResult> EditLeaveRequest(
        Guid id,
        LeaveRequestEditInput x,
        CancellationToken ct)
    {
        await service.EditLeaveRequest(
            UserId,
            Admin,
            id,
            x,
            ct);

        return Success();
    }

    [HttpGet("regular-leave")]
    public async Task<ActionResult> RegularCalendar(
        CancellationToken ct,
        string? month = null)
    {
        return Success(
            await service.RegularCalendar(
                UserId,
                Admin,
                ct,
                month));
    }

    [HttpPost("regular-leave")]
    public async Task<ActionResult> RegisterRegular(
        RegularLeaveInput x,
        CancellationToken ct)
    {
        return Success(
            await service.RegisterRegular(
                UserId,
                Admin,
                x,
                ct));
    }

    [HttpPut("regular-leave/{id:guid}")]
    public async Task<ActionResult> EditRegular(
        Guid id,
        RegularLeaveEditInput x,
        CancellationToken ct)
    {
        await service.EditRegular(
            UserId,
            Admin,
            id,
            x,
            ct);

        return Success();
    }

    [HttpGet("requests/{id:guid}")]
    public async Task<ActionResult> RequestDetail(
        Guid id,
        CancellationToken ct)
    {
        return Success(
            await service.RequestDetail(
                UserId,
                Admin,
                id,
                ct));
    }

    [HttpGet("requests/{id:guid}/attachment")]
    public async Task<ActionResult> Attachment(
        Guid id,
        CancellationToken ct)
    {
        var file = await service.Attachment(
            UserId,
            Admin,
            id,
            ct);

        var environment =
            HttpContext.RequestServices
                .GetRequiredService<IWebHostEnvironment>();

        var path = Path.Combine(
            environment.WebRootPath,
            "uploads",
            file.StoredName);

        if (!System.IO.File.Exists(path))
            return NotFound();

        Response.Headers["X-Content-Type-Options"] =
            "nosniff";

        return PhysicalFile(
            path,
            file.ContentType,
            file.OriginalName);
    }

    [HttpPost("requests/{id:guid}/review")]
    public async Task<ActionResult> Review(
        Guid id,
        ReviewInput x,
        CancellationToken ct)
    {
        await service.Review(
            UserId,
            Admin,
            id,
            x,
            ct);

        return Success();
    }

    [HttpPost("staffing-rules")]
    public async Task<ActionResult> Staffing(
        StaffingInput x,
        CancellationToken ct)
    {
        return Success(
            await service.SaveStaffing(
                UserId,
                Admin,
                x,
                ct));
    }

    [HttpPost("peak-days")]
    public async Task<ActionResult> Peak(
        PeakInput x,
        CancellationToken ct)
    {
        await service.SavePeak(
            UserId,
            Admin,
            x,
            ct);

        return Success();
    }

    [HttpGet("capacity")]
    public async Task<ActionResult> Capacity(
        Guid departmentId,
        DateOnly from,
        DateOnly to,
        CancellationToken ct)
    {
        var actor = await service.Actor(
            UserId,
            Admin,
            ct);

        if (!Admin &&
            actor.GoldRole != "hr" &&
            actor.DepartmentId != departmentId)
        {
            return Forbid();
        }

        return Success(
            await service.Capacity(
                departmentId,
                from,
                to,
                null,
                ct));
    }

    [HttpGet("report.xlsx")]
    public async Task<ActionResult> Report(
        DateOnly from,
        DateOnly to,
        CancellationToken ct)
    {
        var rows =
            (IEnumerable<AttendanceResponse>)
            await service.List(
                UserId,
                Admin,
                "attendance",
                from,
                to,
                ct);

        using var book = new XLWorkbook();

        var sheet =
            book.AddWorksheet("Bảng công Gold");

        string[] headers =
        [
            "Nhân viên",
            "Phòng ban",
            "Ngày",
            "Giờ",
            "Loại",
            "Trạng thái",
            "Phút bất thường",
            "Khoảng cách (m)"
        ];

        for (var i = 0; i < headers.Length; i++)
        {
            sheet.Cell(1, i + 1).Value =
                headers[i];
        }

        var rowNumber = 2;

        foreach (var row in rows)
        {
            sheet.Cell(rowNumber, 1).Value =
                row.Name;

            sheet.Cell(rowNumber, 2).Value =
                row.Department;

            sheet.Cell(rowNumber, 3).Value =
                row.Date.ToString("dd/MM/yyyy");

            sheet.Cell(rowNumber, 4).Value =
                GoldRules.LocalNow(row.RecordedAt)
                    .ToString("HH:mm:ss");

            sheet.Cell(rowNumber, 5).Value =
                row.Type == "in"
                    ? "Vào"
                    : "Ra";

            sheet.Cell(rowNumber, 6).Value =
                row.Status switch
                {
                    "normal" => "Đúng giờ",
                    "late" => "Đi muộn",
                    "early-leave" => "Về sớm",
                    "early-checkin" => "Chấm công sớm",
                    "offsite" => "Ngoài cơ sở",
                    "late-offsite" => "Đi muộn · ngoài cơ sở",
                    "early-leave-offsite" => "Về sớm · ngoài cơ sở",
                    "early-checkin-offsite" => "Chấm công sớm · ngoài cơ sở",
                    "explained" => "Đã giải trình",
                    "supplemented" => "Đã bổ sung",
                    "approved" => "Đã duyệt",
                    "rejected" => "Từ chối",
                    "cancelled" => "Đã hủy",
                    "pending" => "Chờ xử lý",
                    "partner" => "Chờ đồng nghiệp",
                    "manager" => "Chờ trưởng phòng",
                    "hr" => "Chờ hành chính nhân sự",
                    _ => "Chưa xác định"
                };

            sheet.Cell(rowNumber, 7).Value =
                row.DeviationMinutes;

            sheet.Cell(rowNumber, 8).Value =
                Math.Round(row.DistanceMeters);

            rowNumber++;
        }

        sheet.Row(1).Style.Font.Bold = true;

        sheet.Row(1)
            .Style
            .Fill
            .BackgroundColor =
            XLColor.FromHtml("#087c50");

        sheet.Row(1)
            .Style
            .Font
            .FontColor =
            XLColor.White;

        sheet.Columns()
            .AdjustToContents();

        sheet.SheetView
            .FreezeRows(1);

        using var stream =
            new MemoryStream();

        book.SaveAs(stream);

        return File(
            stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"Gold-bang-cong-{from:yyyy-MM}.xlsx");
    }
}