using CrmLeadManagement.Models;
using CrmLeadManagement.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CrmLeadManagement.Controllers;

[Authorize]
public class PayrollController : Controller
{
    private static readonly string[] ExpenseCategories =
    {
        "Employee Benefits", "Training", "Recruitment", "Travel", "Meals",
        "Office Supplies", "Utilities", "Software", "Other"
    };

    private static readonly string[] PaymentMethods =
    {
        "Bank Transfer", "UPI", "Cash", "Cheque"
    };

    private readonly AppDbContext _db;

    public PayrollController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int? month = null, int? year = null)
    {
        if (!TryGetAdminId(out var adminId)) return RedirectToAction("List", "Lead");
        var (selectedMonth, selectedYear) = GetPeriod(month, year);
        var model = new PayrollPageViewModel
        {
            Month = selectedMonth,
            Year = selectedYear,
            PayrollRecords = await _db.PayrollRecords.AsNoTracking()
                .Where(r => r.AdminId == adminId && r.PeriodMonth == selectedMonth && r.PeriodYear == selectedYear)
                .OrderBy(r => r.EmployeeName)
                .ToListAsync()
        };
        SetPage("Payroll", "PayrollDashboard");
        return View("Index", model);
    }

    [HttpGet]
    public async Task<IActionResult> EmployeeSalary(int? editId = null)
    {
        if (!TryGetAdminId(out var adminId)) return RedirectToAction("List", "Lead");
        var model = new PayrollPageViewModel
        {
            Employees = await GetEmployees(adminId),
            Salaries = await _db.EmployeeSalaries.AsNoTracking()
                .Where(s => s.AdminId == adminId)
                .OrderBy(s => s.EmployeeName)
                .ToListAsync()
        };

        if (editId.HasValue)
        {
            var salary = model.Salaries.FirstOrDefault(s => s.Id == editId.Value);
            if (salary == null)
            {
                TempData["ErrorMessage"] = "Salary record not found.";
                return RedirectToAction(nameof(EmployeeSalary));
            }

            model.SalaryForm = new EmployeeSalaryForm
            {
                Id = salary.Id,
                EmployeeId = salary.EmployeeId,
                BasicSalary = salary.BasicSalary,
                Allowances = salary.Allowances,
                Deductions = salary.Deductions,
                EffectiveFrom = salary.EffectiveFrom,
                PaymentMethod = salary.PaymentMethod,
                Notes = salary.Notes
            };
        }

        SetPage("Employee Salary", "PayrollEmployeeSalary");
        return View("Index", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveSalary([Bind(Prefix = "SalaryForm")] EmployeeSalaryForm form)
    {
        if (!TryGetAdminId(out var adminId)) return RedirectToAction("List", "Lead");
        if (!ModelState.IsValid ||
            !IsAllowed(PaymentMethods, form.PaymentMethod) ||
            !IsValidInputDate(form.EffectiveFrom))
        {
            TempData["ErrorMessage"] = "Enter valid salary details and select a supported payment method.";
            return RedirectToAction(nameof(EmployeeSalary), new { editId = form.Id == 0 ? (int?)null : form.Id });
        }

        var employee = await GetEmployee(adminId, form.EmployeeId);
        if (employee == null)
        {
            TempData["ErrorMessage"] = "Select an employee from your organization.";
            return RedirectToAction(nameof(EmployeeSalary));
        }

        var salary = form.Id == 0
            ? null
            : await _db.EmployeeSalaries.FirstOrDefaultAsync(s => s.Id == form.Id && s.AdminId == adminId);
        if (form.Id != 0 && salary == null)
        {
            TempData["ErrorMessage"] = "Salary record not found.";
            return RedirectToAction(nameof(EmployeeSalary));
        }

        var duplicate = await _db.EmployeeSalaries.AnyAsync(s =>
            s.AdminId == adminId && s.EmployeeId == form.EmployeeId && s.Id != form.Id);
        if (duplicate)
        {
            TempData["ErrorMessage"] = "A salary profile already exists for this employee. Edit the existing record.";
            return RedirectToAction(nameof(EmployeeSalary));
        }

        salary ??= new EmployeeSalary { AdminId = adminId };
        salary.EmployeeId = employee.Id;
        salary.EmployeeName = employee.Name ?? "";
        salary.BasicSalary = form.BasicSalary;
        salary.Allowances = form.Allowances;
        salary.Deductions = form.Deductions;
        salary.EffectiveFrom = form.EffectiveFrom.Date;
        salary.PaymentMethod = form.PaymentMethod.Trim();
        salary.Notes = form.Notes?.Trim();
        if (form.Id == 0) _db.EmployeeSalaries.Add(salary);
        await _db.SaveChangesAsync();

        TempData["SuccessMessage"] = "Employee salary saved.";
        return RedirectToAction(nameof(EmployeeSalary));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteSalary(int id)
    {
        if (!TryGetAdminId(out var adminId)) return RedirectToAction("List", "Lead");
        var salary = await _db.EmployeeSalaries.FirstOrDefaultAsync(s => s.Id == id && s.AdminId == adminId);
        if (salary == null)
        {
            TempData["ErrorMessage"] = "Salary record not found.";
            return RedirectToAction(nameof(EmployeeSalary));
        }

        _db.EmployeeSalaries.Remove(salary);
        await _db.SaveChangesAsync();
        TempData["SuccessMessage"] = "Employee salary deleted.";
        return RedirectToAction(nameof(EmployeeSalary));
    }

    [HttpGet]
    public async Task<IActionResult> Expenses(int? editId = null)
    {
        if (!TryGetAdminId(out var adminId)) return RedirectToAction("List", "Lead");
        var model = new PayrollPageViewModel
        {
            Employees = await GetEmployees(adminId),
            Expenses = await _db.PayrollExpenses.AsNoTracking()
                .Where(e => e.AdminId == adminId)
                .OrderByDescending(e => e.ExpenseDate)
                .ToListAsync()
        };

        if (editId.HasValue)
        {
            var expense = model.Expenses.FirstOrDefault(e => e.Id == editId.Value);
            if (expense == null)
            {
                TempData["ErrorMessage"] = "Payroll expense not found.";
                return RedirectToAction(nameof(Expenses));
            }

            model.ExpenseForm = new PayrollExpenseForm
            {
                Id = expense.Id,
                ExpenseDate = expense.ExpenseDate,
                Category = expense.Category,
                Description = expense.Description,
                Amount = expense.Amount,
                EmployeeId = expense.EmployeeId,
                PaymentMethod = expense.PaymentMethod,
                Status = expense.Status,
                Notes = expense.Notes
            };
        }

        ViewBag.ExpenseCategories = ExpenseCategories;
        ViewBag.PaymentMethods = PaymentMethods;
        SetPage("Payroll Expenses", "PayrollExpenses");
        return View("Index", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveExpense([Bind(Prefix = "ExpenseForm")] PayrollExpenseForm form)
    {
        if (!TryGetAdminId(out var adminId)) return RedirectToAction("List", "Lead");
        if (!ModelState.IsValid ||
            !IsAllowed(ExpenseCategories, form.Category) ||
            !IsAllowed(PaymentMethods, form.PaymentMethod) ||
            (form.Status != "Pending" && form.Status != "Paid") ||
            !IsValidInputDate(form.ExpenseDate))
        {
            TempData["ErrorMessage"] = "Enter valid expense details and select supported category, payment method, and status values.";
            return RedirectToAction(nameof(Expenses), new { editId = form.Id == 0 ? (int?)null : form.Id });
        }

        UserRoleEntry? employee = null;
        if (form.EmployeeId.HasValue)
        {
            employee = await GetEmployee(adminId, form.EmployeeId.Value);
            if (employee == null)
            {
                TempData["ErrorMessage"] = "Select an employee from your organization or leave the employee blank.";
                return RedirectToAction(nameof(Expenses));
            }
        }

        var expense = form.Id == 0
            ? null
            : await _db.PayrollExpenses.FirstOrDefaultAsync(e => e.Id == form.Id && e.AdminId == adminId);
        if (form.Id != 0 && expense == null)
        {
            TempData["ErrorMessage"] = "Payroll expense not found.";
            return RedirectToAction(nameof(Expenses));
        }

        expense ??= new PayrollExpense { AdminId = adminId };
        expense.ExpenseDate = form.ExpenseDate.Date;
        expense.Category = form.Category.Trim();
        expense.Description = form.Description.Trim();
        expense.Amount = form.Amount;
        expense.EmployeeId = employee?.Id;
        expense.EmployeeName = employee?.Name;
        expense.PaymentMethod = form.PaymentMethod.Trim();
        expense.Status = form.Status;
        expense.Notes = form.Notes?.Trim();
        if (form.Id == 0) _db.PayrollExpenses.Add(expense);
        await _db.SaveChangesAsync();

        TempData["SuccessMessage"] = "Payroll expense saved.";
        return RedirectToAction(nameof(Expenses));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteExpense(int id)
    {
        if (!TryGetAdminId(out var adminId)) return RedirectToAction("List", "Lead");
        var expense = await _db.PayrollExpenses.FirstOrDefaultAsync(e => e.Id == id && e.AdminId == adminId);
        if (expense == null)
        {
            TempData["ErrorMessage"] = "Payroll expense not found.";
            return RedirectToAction(nameof(Expenses));
        }

        _db.PayrollExpenses.Remove(expense);
        await _db.SaveChangesAsync();
        TempData["SuccessMessage"] = "Payroll expense deleted.";
        return RedirectToAction(nameof(Expenses));
    }

    [HttpGet]
    public async Task<IActionResult> OvertimeRequest(int? editId = null)
    {
        if (!TryGetAdminId(out var adminId)) return RedirectToAction("List", "Lead");
        var model = new PayrollPageViewModel
        {
            Employees = await GetEmployees(adminId),
            OvertimeRequests = await _db.OvertimeRequests.AsNoTracking()
                .Where(o => o.AdminId == adminId)
                .OrderByDescending(o => o.WorkDate)
                .ToListAsync()
        };

        if (editId.HasValue)
        {
            var overtime = model.OvertimeRequests.FirstOrDefault(o => o.Id == editId.Value);
            if (overtime == null)
            {
                TempData["ErrorMessage"] = "Overtime request not found.";
                return RedirectToAction(nameof(OvertimeRequest));
            }

            model.OvertimeForm = new OvertimeRequestForm
            {
                Id = overtime.Id,
                EmployeeId = overtime.EmployeeId,
                WorkDate = overtime.WorkDate,
                Hours = overtime.Hours,
                HourlyRate = overtime.HourlyRate,
                Reason = overtime.Reason
            };
        }

        SetPage("Overtime Request", "PayrollOvertimeRequest");
        return View("Index", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveOvertime([Bind(Prefix = "OvertimeForm")] OvertimeRequestForm form)
    {
        if (!TryGetAdminId(out var adminId)) return RedirectToAction("List", "Lead");
        if (!ModelState.IsValid || !IsValidInputDate(form.WorkDate))
        {
            TempData["ErrorMessage"] = "Enter a valid employee, date, hours, hourly rate, and reason.";
            return RedirectToAction(nameof(OvertimeRequest), new { editId = form.Id == 0 ? (int?)null : form.Id });
        }

        var employee = await GetEmployee(adminId, form.EmployeeId);
        if (employee == null)
        {
            TempData["ErrorMessage"] = "Select an employee from your organization.";
            return RedirectToAction(nameof(OvertimeRequest));
        }

        var overtime = form.Id == 0
            ? null
            : await _db.OvertimeRequests.FirstOrDefaultAsync(o => o.Id == form.Id && o.AdminId == adminId);
        if (form.Id != 0 && (overtime == null || overtime.PayrollRecordId.HasValue))
        {
            TempData["ErrorMessage"] = "This overtime request cannot be edited.";
            return RedirectToAction(nameof(OvertimeRequest));
        }

        overtime ??= new OvertimeRequest { AdminId = adminId, Status = "Pending" };
        overtime.EmployeeId = employee.Id;
        overtime.EmployeeName = employee.Name ?? "";
        overtime.WorkDate = form.WorkDate.Date;
        overtime.Hours = form.Hours;
        overtime.HourlyRate = form.HourlyRate;
        overtime.TotalAmount = decimal.Round(form.Hours * form.HourlyRate, 2, MidpointRounding.AwayFromZero);
        overtime.Reason = form.Reason.Trim();
        if (form.Id == 0) _db.OvertimeRequests.Add(overtime);
        await _db.SaveChangesAsync();

        TempData["SuccessMessage"] = "Overtime request saved.";
        return RedirectToAction(nameof(OvertimeRequest));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetOvertimeStatus(int id, string status)
    {
        if (!TryGetAdminId(out var adminId)) return RedirectToAction("List", "Lead");
        if (status != "Approved" && status != "Rejected")
        {
            TempData["ErrorMessage"] = "Choose a valid overtime decision.";
            return RedirectToAction(nameof(OvertimeRequest));
        }

        var overtime = await _db.OvertimeRequests.FirstOrDefaultAsync(o => o.Id == id && o.AdminId == adminId);
        if (overtime == null || overtime.PayrollRecordId.HasValue || overtime.Status == "Paid")
        {
            TempData["ErrorMessage"] = "This overtime request cannot be changed.";
            return RedirectToAction(nameof(OvertimeRequest));
        }

        if (status == "Approved" && await _db.PayrollRecords.AnyAsync(r =>
                r.AdminId == adminId &&
                r.EmployeeId == overtime.EmployeeId &&
                r.PeriodYear == overtime.WorkDate.Year &&
                r.PeriodMonth == overtime.WorkDate.Month))
        {
            TempData["ErrorMessage"] = "Payroll has already been processed for this employee and month. This overtime cannot be added retroactively.";
            return RedirectToAction(nameof(OvertimeRequest));
        }

        overtime.Status = status;
        overtime.ReviewedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        TempData["SuccessMessage"] = $"Overtime request {status.ToLowerInvariant()}.";
        return RedirectToAction(nameof(OvertimeRequest));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteOvertime(int id)
    {
        if (!TryGetAdminId(out var adminId)) return RedirectToAction("List", "Lead");
        var overtime = await _db.OvertimeRequests.FirstOrDefaultAsync(o => o.Id == id && o.AdminId == adminId);
        if (overtime == null || overtime.PayrollRecordId.HasValue)
        {
            TempData["ErrorMessage"] = "This overtime request cannot be deleted.";
            return RedirectToAction(nameof(OvertimeRequest));
        }

        _db.OvertimeRequests.Remove(overtime);
        await _db.SaveChangesAsync();
        TempData["SuccessMessage"] = "Overtime request deleted.";
        return RedirectToAction(nameof(OvertimeRequest));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ProcessPayroll(int month, int year)
    {
        if (!TryGetAdminId(out var adminId)) return RedirectToAction("List", "Lead");
        if (!IsValidPeriod(month, year))
        {
            TempData["ErrorMessage"] = "Select a valid payroll month and year.";
            return RedirectToAction(nameof(Index));
        }

        var salaries = await _db.EmployeeSalaries.AsNoTracking()
            .Where(s => s.AdminId == adminId)
            .OrderBy(s => s.EmployeeName)
            .ToListAsync();
        var existingEmployeeIds = await _db.PayrollRecords.AsNoTracking()
            .Where(r => r.AdminId == adminId && r.PeriodMonth == month && r.PeriodYear == year)
            .Select(r => r.EmployeeId)
            .ToListAsync();
        var existingSet = existingEmployeeIds.ToHashSet();
        var periodStart = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var periodEnd = periodStart.AddMonths(1);
        var overtimeRequests = await _db.OvertimeRequests
            .Where(o => o.AdminId == adminId &&
                        o.Status == "Approved" &&
                        o.PayrollRecordId == null &&
                        o.WorkDate >= periodStart &&
                        o.WorkDate < periodEnd)
            .ToListAsync();
        var overtimeByEmployee = overtimeRequests.GroupBy(o => o.EmployeeId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var records = new List<PayrollRecord>();
        var daysInMonth = DateTime.DaysInMonth(year, month);
        foreach (var salary in salaries.Where(s => !existingSet.Contains(s.EmployeeId)))
        {
            var salaryStart = salary.EffectiveFrom.Date > periodStart.Date
                ? salary.EffectiveFrom.Date
                : periodStart.Date;
            if (salaryStart >= periodEnd.Date) continue;

            var payableDays = (periodEnd.Date - salaryStart).Days;
            var basicSalary = decimal.Round(salary.BasicSalary * payableDays / daysInMonth, 2, MidpointRounding.AwayFromZero);
            var allowances = decimal.Round(salary.Allowances * payableDays / daysInMonth, 2, MidpointRounding.AwayFromZero);
            var deductions = decimal.Round(salary.Deductions * payableDays / daysInMonth, 2, MidpointRounding.AwayFromZero);
            var approvedOvertime = overtimeByEmployee.TryGetValue(salary.EmployeeId, out var requests)
                ? requests
                : new List<OvertimeRequest>();
            var overtimePay = approvedOvertime.Sum(o => o.TotalAmount);
            var grossPay = decimal.Round(basicSalary + allowances + overtimePay, 2, MidpointRounding.AwayFromZero);
            var netPay = decimal.Round(grossPay - deductions, 2, MidpointRounding.AwayFromZero);
            if (netPay < 0)
            {
                TempData["ErrorMessage"] = $"Deductions exceed gross pay for {salary.EmployeeName}; update the salary profile first.";
                return RedirectToAction(nameof(Index), new { month, year });
            }

            var record = new PayrollRecord
            {
                AdminId = adminId,
                EmployeeId = salary.EmployeeId,
                EmployeeName = salary.EmployeeName,
                PeriodYear = year,
                PeriodMonth = month,
                BasicSalary = basicSalary,
                Allowances = allowances,
                OvertimePay = overtimePay,
                Deductions = deductions,
                GrossPay = grossPay,
                NetPay = netPay,
                Status = "Processed",
                ProcessedAt = DateTime.UtcNow
            };
            records.Add(record);
            _db.PayrollRecords.Add(record);
            foreach (var request in approvedOvertime)
            {
                request.PayrollRecord = record;
            }
        }

        if (records.Count == 0)
        {
            TempData["ErrorMessage"] = salaries.Count == 0
                ? "Add employee salary profiles before processing payroll."
                : "There are no new employee salary records to process for this month.";
            return RedirectToAction(nameof(Index), new { month, year });
        }

        await _db.SaveChangesAsync();
        TempData["SuccessMessage"] = $"Payroll processed for {records.Count} employee(s). Review the totals and mark records paid after disbursement.";
        return RedirectToAction(nameof(Index), new { month, year });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkPayrollPaid(int id, int month, int year)
    {
        if (!TryGetAdminId(out var adminId)) return RedirectToAction("List", "Lead");
        var record = await _db.PayrollRecords.FirstOrDefaultAsync(r => r.Id == id && r.AdminId == adminId);
        if (record == null || record.Status == "Paid")
        {
            TempData["ErrorMessage"] = "Payroll record not found or already paid.";
            return RedirectToAction(nameof(Index), new { month, year });
        }

        record.Status = "Paid";
        record.PaidAt = DateTime.UtcNow;
        var overtimeRequests = await _db.OvertimeRequests
            .Where(o => o.AdminId == adminId && o.PayrollRecordId == record.Id)
            .ToListAsync();
        foreach (var request in overtimeRequests) request.Status = "Paid";
        await _db.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Payroll marked paid for {record.EmployeeName}.";
        return RedirectToAction(nameof(Index), new { month = record.PeriodMonth, year = record.PeriodYear });
    }

    [HttpGet]
    public async Task<IActionResult> Reports(int? month = null, int? year = null)
    {
        if (!TryGetAdminId(out var adminId)) return RedirectToAction("List", "Lead");
        var (selectedMonth, selectedYear) = GetPeriod(month, year);
        var periodStart = new DateTime(selectedYear, selectedMonth, 1, 0, 0, 0, DateTimeKind.Utc);
        var periodEnd = periodStart.AddMonths(1);
        var model = new PayrollPageViewModel
        {
            Month = selectedMonth,
            Year = selectedYear,
            PayrollRecords = await _db.PayrollRecords.AsNoTracking()
                .Where(r => r.AdminId == adminId && r.PeriodMonth == selectedMonth && r.PeriodYear == selectedYear)
                .OrderBy(r => r.EmployeeName)
                .ToListAsync(),
            Expenses = await _db.PayrollExpenses.AsNoTracking()
                .Where(e => e.AdminId == adminId && e.ExpenseDate >= periodStart && e.ExpenseDate < periodEnd)
                .OrderBy(e => e.ExpenseDate)
                .ToListAsync(),
            OvertimeRequests = await _db.OvertimeRequests.AsNoTracking()
                .Where(o => o.AdminId == adminId && o.WorkDate >= periodStart && o.WorkDate < periodEnd)
                .OrderBy(o => o.WorkDate)
                .ToListAsync()
        };
        model.TotalGrossPay = model.PayrollRecords.Sum(r => r.GrossPay);
        model.TotalNetPay = model.PayrollRecords.Sum(r => r.NetPay);
        model.TotalExpenses = model.Expenses.Sum(e => e.Amount);
        model.TotalOvertime = model.OvertimeRequests.Where(o => o.Status == "Approved" || o.Status == "Paid").Sum(o => o.TotalAmount);
        SetPage("Payroll Reports", "PayrollReports");
        return View("Index", model);
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
        TempData["ErrorMessage"] = "Access denied. Payroll is available to tenant administrators only.";
        return false;
    }

    private async Task<List<PayrollEmployeeOption>> GetEmployees(int adminId)
    {
        return await _db.Users.AsNoTracking()
            .Where(u => u.AdminId == adminId &&
                        u.Role != null &&
                        u.Role != "Admin" &&
                        u.Role != "Super Admin")
            .OrderBy(u => u.Name)
            .Select(u => new PayrollEmployeeOption { Id = u.Id, Name = u.Name ?? "" })
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

    private static (int Month, int Year) GetPeriod(int? month, int? year)
    {
        var current = DateTime.Today;
        var selectedMonth = month.GetValueOrDefault(current.Month);
        var selectedYear = year.GetValueOrDefault(current.Year);
        return IsValidPeriod(selectedMonth, selectedYear)
            ? (selectedMonth, selectedYear)
            : (current.Month, current.Year);
    }

    private static bool IsValidPeriod(int month, int year) =>
        month >= 1 && month <= 12 && year >= 2000 && year <= DateTime.Today.Year + 5;

    private static bool IsValidInputDate(DateTime value) =>
        value.Year >= 2000 && value.Year <= DateTime.Today.Year + 5;

    private void SetPage(string title, string activePage)
    {
        ViewData["Title"] = title;
        ViewData["ActivePage"] = activePage;
        ViewData["PayrollSectionTitle"] = title;
    }
}
