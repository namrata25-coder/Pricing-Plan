using System.ComponentModel.DataAnnotations;

namespace CrmLeadManagement.Models;

public class EmployeeSalary
{
    public int Id { get; set; }
    public int AdminId { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = "";
    public decimal BasicSalary { get; set; }
    public decimal Allowances { get; set; }
    public decimal Deductions { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public string PaymentMethod { get; set; } = "Bank Transfer";
    public string? Notes { get; set; }
}

public class PayrollRecord
{
    public int Id { get; set; }
    public int AdminId { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = "";
    public int PeriodYear { get; set; }
    public int PeriodMonth { get; set; }
    public decimal BasicSalary { get; set; }
    public decimal Allowances { get; set; }
    public decimal OvertimePay { get; set; }
    public decimal Deductions { get; set; }
    public decimal GrossPay { get; set; }
    public decimal NetPay { get; set; }
    public string Status { get; set; } = "Processed";
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
    public DateTime? PaidAt { get; set; }
    public string? Notes { get; set; }
}

public class PayrollExpense
{
    public int Id { get; set; }
    public int AdminId { get; set; }
    public DateTime ExpenseDate { get; set; }
    public string Category { get; set; } = "";
    public string Description { get; set; } = "";
    public decimal Amount { get; set; }
    public int? EmployeeId { get; set; }
    public string? EmployeeName { get; set; }
    public string PaymentMethod { get; set; } = "Bank Transfer";
    public string Status { get; set; } = "Pending";
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class OvertimeRequest
{
    public int Id { get; set; }
    public int AdminId { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = "";
    public DateTime WorkDate { get; set; }
    public decimal Hours { get; set; }
    public decimal HourlyRate { get; set; }
    public decimal TotalAmount { get; set; }
    public string Reason { get; set; } = "";
    public string Status { get; set; } = "Pending";
    public int? PayrollRecordId { get; set; }
    public PayrollRecord? PayrollRecord { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class PayrollEmployeeOption
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}

public class EmployeeSalaryForm
{
    public int Id { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Select an employee.")]
    public int EmployeeId { get; set; }

    [Range(typeof(decimal), "0", "999999999999", ErrorMessage = "Basic salary must be zero or greater.")]
    public decimal BasicSalary { get; set; }

    [Range(typeof(decimal), "0", "999999999999", ErrorMessage = "Allowances must be zero or greater.")]
    public decimal Allowances { get; set; }

    [Range(typeof(decimal), "0", "999999999999", ErrorMessage = "Deductions must be zero or greater.")]
    public decimal Deductions { get; set; }

    [DataType(DataType.Date)]
    public DateTime EffectiveFrom { get; set; } = DateTime.Today;

    [StringLength(40)]
    public string PaymentMethod { get; set; } = "Bank Transfer";

    [StringLength(1000)]
    public string? Notes { get; set; }
}

public class PayrollExpenseForm
{
    public int Id { get; set; }

    [DataType(DataType.Date)]
    public DateTime ExpenseDate { get; set; } = DateTime.Today;

    [Required, StringLength(80)]
    public string Category { get; set; } = "";

    [Required, StringLength(250)]
    public string Description { get; set; } = "";

    [Range(typeof(decimal), "0.01", "999999999999", ErrorMessage = "Amount must be greater than zero.")]
    public decimal Amount { get; set; }

    public int? EmployeeId { get; set; }

    [StringLength(40)]
    public string PaymentMethod { get; set; } = "Bank Transfer";

    [StringLength(20)]
    public string Status { get; set; } = "Pending";

    [StringLength(1000)]
    public string? Notes { get; set; }
}

public class OvertimeRequestForm
{
    public int Id { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Select an employee.")]
    public int EmployeeId { get; set; }

    [DataType(DataType.Date)]
    public DateTime WorkDate { get; set; } = DateTime.Today;

    [Range(typeof(decimal), "0.01", "24", ErrorMessage = "Hours must be greater than zero and at most 24.")]
    public decimal Hours { get; set; }

    [Range(typeof(decimal), "0.01", "999999999", ErrorMessage = "Hourly rate must be greater than zero.")]
    public decimal HourlyRate { get; set; }

    [Required, StringLength(500)]
    public string Reason { get; set; } = "";
}

public class PayrollPageViewModel
{
    public List<PayrollEmployeeOption> Employees { get; set; } = new();
    public List<EmployeeSalary> Salaries { get; set; } = new();
    public List<PayrollRecord> PayrollRecords { get; set; } = new();
    public List<PayrollExpense> Expenses { get; set; } = new();
    public List<OvertimeRequest> OvertimeRequests { get; set; } = new();
    public EmployeeSalaryForm SalaryForm { get; set; } = new();
    public PayrollExpenseForm ExpenseForm { get; set; } = new();
    public OvertimeRequestForm OvertimeForm { get; set; } = new();
    public int Month { get; set; } = DateTime.Today.Month;
    public int Year { get; set; } = DateTime.Today.Year;
    public decimal TotalGrossPay { get; set; }
    public decimal TotalNetPay { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal TotalOvertime { get; set; }
}
