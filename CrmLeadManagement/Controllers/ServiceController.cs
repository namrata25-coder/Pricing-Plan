using CrmLeadManagement.Models;
using CrmLeadManagement.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CrmLeadManagement.Controllers;

[Authorize]
public class ServiceController : Controller
{
    private readonly AppDbContext _db;
    private readonly ILogger<ServiceController> _logger;

    public ServiceController(AppDbContext db, ILogger<ServiceController> logger)
    {
        _db = db;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int? editId)
    {
        if (!HasPermission("View")) return AccessDenied();
        if (editId.HasValue && !HasPermission("Edit")) return AccessDenied();

        var input = new PortfolioServiceInputModel();
        if (editId.HasValue)
        {
            var service = await ServicesForCurrentUser()
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == editId.Value);
            if (service == null) return NotFound();

            input = new PortfolioServiceInputModel
            {
                ProjectName = service.ProjectName,
                Type = service.Type,
                Description = service.Description,
                Features = service.Features,
                Price = service.Price,
                DemoLink = service.DemoLink
            };
        }

        SetPageState();
        SetPermissions();
        return View(await BuildPageModelAsync(input, editId));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PortfolioServicePageViewModel model)
    {
        if (!HasPermission("Create")) return AccessDenied();

        if (!ModelState.IsValid)
        {
            SetPageState();
            SetPermissions();
            model.EditingServiceId = null;
            model.Services = await LoadServicesAsync();
            return View("Index", model);
        }

        var input = model.Input;
        var user = CurrentUser();
        var service = new PortfolioService
        {
            AdminId = PermissionChecker.IsSuperAdmin(user) ? null : user?.GetEffectiveAdminId(),
            ProjectName = input.ProjectName.Trim(),
            Category = "Other",
            Type = input.Type,
            Description = input.Description.Trim(),
            Features = input.Features.Trim(),
            Price = input.Price,
            DemoLink = string.IsNullOrWhiteSpace(input.DemoLink) ? null : NormalizeUrl(input.DemoLink)
        };
        _db.PortfolioServices.Add(service);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            _db.Entry(service).State = EntityState.Detached;
            _logger.LogError(ex, "Failed to save portfolio service '{ProjectName}'", service.ProjectName);
            ModelState.AddModelError(string.Empty, "Service could not be saved. Please try again or contact support.");
            SetPageState();
            SetPermissions();
            model.EditingServiceId = null;
            model.Services = await LoadServicesAsync();
            return View("Index", model);
        }

        TempData["SuccessMessage"] = "Service saved successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(int id, PortfolioServicePageViewModel model)
    {
        if (!HasPermission("Edit")) return AccessDenied();

        var service = await ServicesForCurrentUser()
            .FirstOrDefaultAsync(item => item.Id == id);
        if (service == null) return NotFound();

        if (!ModelState.IsValid)
        {
            SetPageState();
            SetPermissions();
            model.EditingServiceId = id;
            model.Services = await LoadServicesAsync();
            return View("Index", model);
        }

        var input = model.Input;
        service.ProjectName = input.ProjectName.Trim();
        service.Type = input.Type;
        service.Description = input.Description.Trim();
        service.Features = input.Features.Trim();
        service.Price = input.Price;
        service.DemoLink = string.IsNullOrWhiteSpace(input.DemoLink) ? null : NormalizeUrl(input.DemoLink);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Failed to update portfolio service {ServiceId}", id);
            ModelState.AddModelError(string.Empty, "Service changes could not be saved. Please try again or contact support.");
            SetPageState();
            SetPermissions();
            model.EditingServiceId = id;
            model.Services = await LoadServicesAsync();
            return View("Index", model);
        }

        TempData["SuccessMessage"] = "Service updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        if (!HasPermission("Delete")) return AccessDenied();

        var service = await ServicesForCurrentUser()
            .FirstOrDefaultAsync(item => item.Id == id);
        if (service == null) return NotFound();

        _db.PortfolioServices.Remove(service);
        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Failed to delete portfolio service {ServiceId}", id);
            TempData["ErrorMessage"] = "Service could not be deleted. Please try again or contact support.";
            return RedirectToAction(nameof(Index));
        }

        TempData["SuccessMessage"] = "Service deleted successfully.";
        return RedirectToAction(nameof(Index));
    }

    private UserRoleEntry? CurrentUser() => PermissionChecker.GetCurrentUser(_db, User);

    private bool HasPermission(string action) =>
        PermissionChecker.CanPerformAction(_db, User, "Product", action);

    private IQueryable<PortfolioService> ServicesForCurrentUser()
    {
        var user = CurrentUser();
        var query = _db.PortfolioServices.AsQueryable();
        if (user == null) return query.Where(_ => false);
        if (PermissionChecker.IsSuperAdmin(user)) return query;

        var adminId = user.GetEffectiveAdminId();
        return adminId.HasValue
            ? query.Where(service => service.AdminId == adminId.Value)
            : query.Where(_ => false);
    }

    private async Task<PortfolioServicePageViewModel> BuildPageModelAsync(PortfolioServiceInputModel input, int? editingServiceId)
    {
        var services = await LoadServicesAsync();
        return new PortfolioServicePageViewModel
        {
            Input = input,
            Services = services,
            EditingServiceId = editingServiceId
        };
    }

    private Task<List<PortfolioService>> LoadServicesAsync() =>
        ServicesForCurrentUser()
            .AsNoTracking()
            .OrderByDescending(service => service.Id)
            .ToListAsync();

    private void SetPermissions()
    {
        ViewData["CanCreateService"] = HasPermission("Create");
        ViewData["CanEditService"] = HasPermission("Edit");
        ViewData["CanDeleteService"] = HasPermission("Delete");
    }

    private IActionResult AccessDenied()
    {
        TempData["ErrorMessage"] = "Access Denied: You do not have permission to manage Services.";
        return RedirectToAction("List", "Lead");
    }

    private void SetPageState()
    {
        ViewData["Title"] = "Service Management";
        ViewData["ActivePage"] = "PortfolioService";
    }

    private static string NormalizeUrl(string url) =>
        url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
        url.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            ? url.Trim()
            : $"https://{url.Trim()}";
}
