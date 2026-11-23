using CrmLeadManagement.Models;
using CrmLeadManagement.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CrmLeadManagement.Controllers;

[Authorize]
public class HRController : Controller
{
    private static readonly string[] LeaveTypes = { "Annual", "Sick", "Casual", "Unpaid", "Other" };
    private static readonly string[] AttendanceStatuses = { "Present", "Absent", "Half Day", "On Leave" };

    private readonly AppDbContext _db;

    public HRController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public IActionResult Employees() => ShowModule("Employees", "HREmployees", "bi-people");

    [HttpGet]
    public async Task<IActionResult> Leaves(int? editId = null)
    {
        if (!TryGetAdminId(out var adminId)) return RedirectToAction("List", "Lead");

        var model = CreateModulePage("Leaves", "HRLeaves", "bi-calendar-check");
        model.Employees = await GetEmployees(adminId);
        model.LeaveRequests = await _db.HRLeaveRequests.AsNoTracking()
            .Where(r => r.AdminId == adminId)
            .OrderByDescending(r => r.StartDate)
            .ThenBy(r => r.EmployeeName)
            .ToListAsync();

        if (editId.HasValue)
        {
            var leave = model.LeaveRequests.FirstOrDefault(r => r.Id == editId.Value);
            if (leave == null)
            {
                TempData["ErrorMessage"] = "Leave request not found.";
                return RedirectToAction(nameof(Leaves));
            }

            model.LeaveForm = new HRLeaveForm
            {
                Id = leave.Id,
                EmployeeId = leave.EmployeeId,
                LeaveType = leave.LeaveType,
                StartDate = leave.StartDate,
                EndDate = leave.EndDate,
                Reason = leave.Reason
            };
        }

        return View("Module", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveLeave([Bind(Prefix = "LeaveForm")] HRLeaveForm form)
    {
        if (!TryGetAdminId(out var adminId)) return RedirectToAction("List", "Lead");
        if (!ModelState.IsValid ||
            !IsAllowed(LeaveTypes, form.LeaveType) ||
            form.EndDate < form.StartDate)
        {
            TempData["ErrorMessage"] = "Enter valid leave details and ensure the end date is not before the start date.";
            return RedirectToAction(nameof(Leaves), new { editId = form.Id == 0 ? (int?)null : form.Id });
        }

        var employee = await GetEmployee(adminId, form.EmployeeId);
        if (employee == null)
        {
            TempData["ErrorMessage"] = "Select an employee from your organization.";
            return RedirectToAction(nameof(Leaves));
        }

        var leave = form.Id == 0
            ? null
            : await _db.HRLeaveRequests.FirstOrDefaultAsync(r => r.Id == form.Id && r.AdminId == adminId);
        if (form.Id != 0 && leave == null)
        {
            TempData["ErrorMessage"] = "Leave request not found.";
            return RedirectToAction(nameof(Leaves));
        }

        leave ??= new HRLeaveRequest { AdminId = adminId, Status = "Pending" };
        leave.EmployeeId = employee.Id;
        leave.EmployeeName = employee.Name ?? "";
        leave.LeaveType = form.LeaveType.Trim();
        leave.StartDate = form.StartDate;
        leave.EndDate = form.EndDate;
        leave.Reason = form.Reason.Trim();
        if (form.Id == 0) _db.HRLeaveRequests.Add(leave);
        await _db.SaveChangesAsync();

        TempData["SuccessMessage"] = "Leave request saved.";
        return RedirectToAction(nameof(Leaves));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReviewLeave(int id, string status)
    {
        if (!TryGetAdminId(out var adminId)) return RedirectToAction("List", "Lead");
        if (status != "Approved" && status != "Rejected")
        {
            TempData["ErrorMessage"] = "Choose a valid leave decision.";
            return RedirectToAction(nameof(Leaves));
        }

        var leave = await _db.HRLeaveRequests.FirstOrDefaultAsync(r => r.Id == id && r.AdminId == adminId);
        if (leave == null || leave.Status != "Pending")
        {
            TempData["ErrorMessage"] = "The pending leave request was not found.";
            return RedirectToAction(nameof(Leaves));
        }

        leave.Status = status;
        leave.ReviewedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        TempData["SuccessMessage"] = $"Leave request {status.ToLowerInvariant()}.";
        return RedirectToAction(nameof(Leaves));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteLeave(int id)
    {
        if (!TryGetAdminId(out var adminId)) return RedirectToAction("List", "Lead");
        var leave = await _db.HRLeaveRequests.FirstOrDefaultAsync(r => r.Id == id && r.AdminId == adminId);
        if (leave == null)
        {
            TempData["ErrorMessage"] = "Leave request not found.";
            return RedirectToAction(nameof(Leaves));
        }

        _db.HRLeaveRequests.Remove(leave);
        await _db.SaveChangesAsync();
        TempData["SuccessMessage"] = "Leave request deleted.";
        return RedirectToAction(nameof(Leaves));
    }

    [HttpGet]
    public Task<IActionResult> ShiftRoster(int? editId = null) =>
        LoadShiftRoster(editId);

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveShiftRoster([Bind(Prefix = "ShiftRosterForm")] HRShiftRosterForm form)
    {
        if (!TryGetAdminId(out var adminId)) return RedirectToAction("List", "Lead");
        if (!ModelState.IsValid || form.StartTime == form.EndTime)
        {
            TempData["ErrorMessage"] = "Enter valid shift details. Start and end times must be different.";
            return RedirectToAction(nameof(ShiftRoster), new { editId = form.Id == 0 ? (int?)null : form.Id });
        }

        var employee = await GetEmployee(adminId, form.EmployeeId);
        if (employee == null)
        {
            TempData["ErrorMessage"] = "Select an employee from your organization.";
            return RedirectToAction(nameof(ShiftRoster));
        }

        var duplicate = await _db.HRShiftRosterEntries.AnyAsync(r =>
            r.AdminId == adminId && r.EmployeeId == form.EmployeeId &&
            r.ShiftDate == form.ShiftDate && r.Id != form.Id);
        if (duplicate)
        {
            TempData["ErrorMessage"] = "This employee already has a shift assigned for that date.";
            return RedirectToAction(nameof(ShiftRoster));
        }

        var shift = form.Id == 0
            ? null
            : await _db.HRShiftRosterEntries.FirstOrDefaultAsync(r => r.Id == form.Id && r.AdminId == adminId);
        if (form.Id != 0 && shift == null)
        {
            TempData["ErrorMessage"] = "Shift assignment not found.";
            return RedirectToAction(nameof(ShiftRoster));
        }

        shift ??= new HRShiftRosterEntry { AdminId = adminId };
        shift.EmployeeId = employee.Id;
        shift.EmployeeName = employee.Name ?? "";
        shift.ShiftDate = form.ShiftDate;
        shift.ShiftName = form.ShiftName.Trim();
        shift.StartTime = form.StartTime;
        shift.EndTime = form.EndTime;
        shift.BreakMinutes = form.BreakMinutes;
        shift.Notes = form.Notes?.Trim();
        if (form.Id == 0) _db.HRShiftRosterEntries.Add(shift);
        await _db.SaveChangesAsync();

        TempData["SuccessMessage"] = "Shift assignment saved.";
        return RedirectToAction(nameof(ShiftRoster));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteShiftRoster(int id)
    {
        if (!TryGetAdminId(out var adminId)) return RedirectToAction("List", "Lead");
        var shift = await _db.HRShiftRosterEntries.FirstOrDefaultAsync(r => r.Id == id && r.AdminId == adminId);
        if (shift == null)
        {
            TempData["ErrorMessage"] = "Shift assignment not found.";
            return RedirectToAction(nameof(ShiftRoster));
        }

        _db.HRShiftRosterEntries.Remove(shift);
        await _db.SaveChangesAsync();
        TempData["SuccessMessage"] = "Shift assignment deleted.";
        return RedirectToAction(nameof(ShiftRoster));
    }

    [HttpGet]
    public async Task<IActionResult> Attendance(int? editId = null)
    {
        if (!TryGetAdminId(out var adminId)) return RedirectToAction("List", "Lead");

        var model = CreateModulePage("Attendance", "HRAttendance", "bi-clock-history");
        model.Employees = await GetEmployees(adminId);
        model.AttendanceRecords = await _db.HRAttendanceRecords.AsNoTracking()
            .Where(r => r.AdminId == adminId)
            .OrderByDescending(r => r.WorkDate)
            .ThenBy(r => r.EmployeeName)
            .ToListAsync();

        if (editId.HasValue)
        {
            var record = model.AttendanceRecords.FirstOrDefault(r => r.Id == editId.Value);
            if (record == null)
            {
                TempData["ErrorMessage"] = "Attendance record not found.";
                return RedirectToAction(nameof(Attendance));
            }

            model.AttendanceForm = new HRAttendanceForm
            {
                Id = record.Id,
                EmployeeId = record.EmployeeId,
                WorkDate = record.WorkDate,
                Status = record.Status,
                CheckIn = record.CheckIn,
                CheckOut = record.CheckOut,
                Notes = record.Notes
            };
        }

        return View("Module", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveAttendance([Bind(Prefix = "AttendanceForm")] HRAttendanceForm form)
    {
        if (!TryGetAdminId(out var adminId)) return RedirectToAction("List", "Lead");
        if (!ModelState.IsValid ||
            !IsAllowed(AttendanceStatuses, form.Status) ||
            (form.CheckIn.HasValue && form.CheckOut.HasValue && form.CheckOut < form.CheckIn))
        {
            TempData["ErrorMessage"] = "Enter valid attendance details and ensure check-out is not before check-in.";
            return RedirectToAction(nameof(Attendance), new { editId = form.Id == 0 ? (int?)null : form.Id });
        }

        var employee = await GetEmployee(adminId, form.EmployeeId);
        if (employee == null)
        {
            TempData["ErrorMessage"] = "Select an employee from your organization.";
            return RedirectToAction(nameof(Attendance));
        }

        var duplicate = await _db.HRAttendanceRecords.AnyAsync(r =>
            r.AdminId == adminId && r.EmployeeId == form.EmployeeId &&
            r.WorkDate == form.WorkDate && r.Id != form.Id);
        if (duplicate)
        {
            TempData["ErrorMessage"] = "Attendance has already been recorded for this employee and date.";
            return RedirectToAction(nameof(Attendance));
        }

        var record = form.Id == 0
            ? null
            : await _db.HRAttendanceRecords.FirstOrDefaultAsync(r => r.Id == form.Id && r.AdminId == adminId);
        if (form.Id != 0 && record == null)
        {
            TempData["ErrorMessage"] = "Attendance record not found.";
            return RedirectToAction(nameof(Attendance));
        }

        record ??= new HRAttendanceRecord { AdminId = adminId };
        record.EmployeeId = employee.Id;
        record.EmployeeName = employee.Name ?? "";
        record.WorkDate = form.WorkDate;
        record.Status = form.Status.Trim();
        record.CheckIn = form.CheckIn;
        record.CheckOut = form.CheckOut;
        record.Notes = form.Notes?.Trim();
        if (form.Id == 0) _db.HRAttendanceRecords.Add(record);
        await _db.SaveChangesAsync();

        TempData["SuccessMessage"] = "Attendance record saved.";
        return RedirectToAction(nameof(Attendance));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAttendance(int id)
    {
        if (!TryGetAdminId(out var adminId)) return RedirectToAction("List", "Lead");
        var record = await _db.HRAttendanceRecords.FirstOrDefaultAsync(r => r.Id == id && r.AdminId == adminId);
        if (record == null)
        {
            TempData["ErrorMessage"] = "Attendance record not found.";
            return RedirectToAction(nameof(Attendance));
        }

        _db.HRAttendanceRecords.Remove(record);
        await _db.SaveChangesAsync();
        TempData["SuccessMessage"] = "Attendance record deleted.";
        return RedirectToAction(nameof(Attendance));
    }

    [HttpGet]
    public IActionResult Holiday() => ShowModule("Holiday", "HRHoliday", "bi-calendar-event");

    [HttpGet]
    public IActionResult Designation() => ShowModule("Designation", "HRDesignation", "bi-person-vcard");

    [HttpGet]
    public IActionResult Department() => ShowModule("Department", "HRDepartment", "bi-diagram-3");

    [HttpGet]
    public IActionResult Appreciation() => ShowModule("Appreciation", "HRAppreciation", "bi-award");

    private async Task<IActionResult> LoadShiftRoster(int? editId)
    {
        if (!TryGetAdminId(out var adminId)) return RedirectToAction("List", "Lead");

        var model = CreateModulePage("Shift Roster", "HRShiftRoster", "bi-calendar-week");
        model.Employees = await GetEmployees(adminId);
        model.ShiftRosterEntries = await _db.HRShiftRosterEntries.AsNoTracking()
            .Where(r => r.AdminId == adminId)
            .OrderByDescending(r => r.ShiftDate)
            .ThenBy(r => r.StartTime)
            .ThenBy(r => r.EmployeeName)
            .ToListAsync();

        if (editId.HasValue)
        {
            var shift = model.ShiftRosterEntries.FirstOrDefault(r => r.Id == editId.Value);
            if (shift == null)
            {
                TempData["ErrorMessage"] = "Shift assignment not found.";
                return RedirectToAction(nameof(ShiftRoster));
            }

            model.ShiftRosterForm = new HRShiftRosterForm
            {
                Id = shift.Id,
                EmployeeId = shift.EmployeeId,
                ShiftDate = shift.ShiftDate,
                ShiftName = shift.ShiftName,
                StartTime = shift.StartTime,
                EndTime = shift.EndTime,
                BreakMinutes = shift.BreakMinutes,
                Notes = shift.Notes
            };
        }

        return View("Module", model);
    }

    private IActionResult ShowModule(string title, string activePage, string icon)
    {
        var model = CreateModulePage(title, activePage, icon);
        return View("Module", model);
    }

    private HRModulePageViewModel CreateModulePage(string title, string activePage, string icon)
    {
        ViewData["Title"] = title;
        ViewData["ActivePage"] = activePage;
        return new HRModulePageViewModel
        {
            Title = title,
            ActivePage = activePage,
            Icon = icon
        };
    }

    private bool TryGetAdminId(out int adminId)
    {
        var currentUser = PermissionChecker.GetCurrentUser(_db, User);
        if (currentUser != null &&
            string.Equals(currentUser.Role, "Admin", StringComparison.OrdinalIgnoreCase) &&
            currentUser.GetEffectiveAdminId() is int effectiveAdminId &&
            effectiveAdminId > 0)
        {
            adminId = effectiveAdminId;
            return true;
        }

        adminId = 0;
        TempData["ErrorMessage"] = "Access denied. HR modules are available to tenant administrators only.";
        return false;
    }

    private async Task<List<HREmployeeOption>> GetEmployees(int adminId)
    {
        return await _db.Users.AsNoTracking()
            .Where(u => u.AdminId == adminId &&
                        u.Role != null &&
                        u.Role != "Admin" &&
                        u.Role != "Super Admin")
            .OrderBy(u => u.Name)
            .Select(u => new HREmployeeOption { Id = u.Id, Name = u.Name ?? "" })
            .ToListAsync();
    }

    private async Task<UserRoleEntry?> GetEmployee(int adminId, int employeeId)
    {
        return await _db.Users.FirstOrDefaultAsync(u =>
            u.Id == employeeId &&
            u.AdminId == adminId &&
            u.Role != null &&
            u.Role != "Admin" &&
            u.Role != "Super Admin");
    }

    private static bool IsAllowed(IEnumerable<string> allowedValues, string? value) =>
        !string.IsNullOrWhiteSpace(value) && allowedValues.Contains(value.Trim(), StringComparer.OrdinalIgnoreCase);
}
