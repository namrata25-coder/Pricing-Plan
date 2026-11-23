using System.ComponentModel.DataAnnotations;

namespace CrmLeadManagement.Models;

public class HRLeaveRequest
{
    public int Id { get; set; }
    public int AdminId { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = "";
    public string LeaveType { get; set; } = "";
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public string Reason { get; set; } = "";
    public string Status { get; set; } = "Pending";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedAt { get; set; }
}

public class HRAttendanceRecord
{
    public int Id { get; set; }
    public int AdminId { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = "";
    public DateOnly WorkDate { get; set; }
    public string Status { get; set; } = "Present";
    public TimeOnly? CheckIn { get; set; }
    public TimeOnly? CheckOut { get; set; }
    public string? Notes { get; set; }
}

public class HRShiftRosterEntry
{
    public int Id { get; set; }
    public int AdminId { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = "";
    public DateOnly ShiftDate { get; set; }
    public string ShiftName { get; set; } = "";
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public int BreakMinutes { get; set; }
    public string? Notes { get; set; }
}

public class HREmployeeOption
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}

public class HRLeaveForm
{
    public int Id { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Select an employee.")]
    public int EmployeeId { get; set; }

    [Required, StringLength(40)]
    public string LeaveType { get; set; } = "Annual";

    [DataType(DataType.Date)]
    public DateOnly StartDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [DataType(DataType.Date)]
    public DateOnly EndDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [Required, StringLength(500)]
    public string Reason { get; set; } = "";
}

public class HRAttendanceForm
{
    public int Id { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Select an employee.")]
    public int EmployeeId { get; set; }

    [DataType(DataType.Date)]
    public DateOnly WorkDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [Required, StringLength(20)]
    public string Status { get; set; } = "Present";

    [DataType(DataType.Time)]
    public TimeOnly? CheckIn { get; set; }

    [DataType(DataType.Time)]
    public TimeOnly? CheckOut { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }
}

public class HRShiftRosterForm
{
    public int Id { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Select an employee.")]
    public int EmployeeId { get; set; }

    [DataType(DataType.Date)]
    public DateOnly ShiftDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [Required, StringLength(80)]
    public string ShiftName { get; set; } = "Regular";

    [DataType(DataType.Time)]
    public TimeOnly StartTime { get; set; } = new(9, 0);

    [DataType(DataType.Time)]
    public TimeOnly EndTime { get; set; } = new(17, 0);

    [Range(0, 480)]
    public int BreakMinutes { get; set; } = 60;

    [StringLength(500)]
    public string? Notes { get; set; }
}

public class HRModulePageViewModel
{
    public string Title { get; set; } = "";
    public string ActivePage { get; set; } = "";
    public string Icon { get; set; } = "bi-person-badge";
    public List<HREmployeeOption> Employees { get; set; } = new();
    public List<HRLeaveRequest> LeaveRequests { get; set; } = new();
    public List<HRAttendanceRecord> AttendanceRecords { get; set; } = new();
    public List<HRShiftRosterEntry> ShiftRosterEntries { get; set; } = new();
    public HRLeaveForm LeaveForm { get; set; } = new();
    public HRAttendanceForm AttendanceForm { get; set; } = new();
    public HRShiftRosterForm ShiftRosterForm { get; set; } = new();
}
