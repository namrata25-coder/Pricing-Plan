using CrmLeadManagement.Models;
using CrmLeadManagement.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CrmLeadManagement.Controllers;

[Authorize]
public class IndustryController : Controller
{
    private readonly AppDbContext _db;
    private readonly ILogger<IndustryController> _logger;

    public IndustryController(AppDbContext db, ILogger<IndustryController> logger)
    {
        _db = db;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? category)
    {
        if (!HasPermission("View")) return AccessDenied();

        var selectedCategory = ResolveCategory(category);
        if (!string.IsNullOrWhiteSpace(category) && selectedCategory == null)
            return BadRequest();

        SetPageState();
        return View(await BuildPageModelAsync(new IndustryDemoInputModel
        {
            Category = selectedCategory ?? ""
        }, selectedCategory));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(IndustryDemoInputModel input)
    {
        if (!HasPermission("Create")) return AccessDenied();

        if (!ModelState.IsValid)
        {
            SetPageState();
            return View("Index", await BuildPageModelAsync(input, ResolveCategory(input.Category)));
        }

        var user = PermissionChecker.GetCurrentUser(_db, User);
        var demoLink = new IndustryDemoLink
        {
            AdminId = PermissionChecker.IsSuperAdmin(user) ? null : user?.GetEffectiveAdminId(),
            Category = ResolveCategory(input.Category)!,
            DemoLink = input.DemoLink.Trim()
        };
        _db.IndustryDemoLinks.Add(demoLink);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            _db.Entry(demoLink).State = EntityState.Detached;
            _logger.LogError(ex, "Failed to save industry demo link for category {Category}", demoLink.Category);
            ModelState.AddModelError(string.Empty, "Demo link could not be saved. Please try again or contact support.");
            SetPageState();
            return View("Index", await BuildPageModelAsync(input, demoLink.Category));
        }

        TempData["SuccessMessage"] = "Industry demo link saved successfully.";
        return RedirectToAction(nameof(Index), new { category = demoLink.Category });
    }

    private async Task<IndustryPageViewModel> BuildPageModelAsync(IndustryDemoInputModel input, string? selectedCategory)
    {
        var demoLinks = selectedCategory == null
            ? new List<IndustryDemoLink>()
            : await IndustryLinksForCurrentUser()
                .AsNoTracking()
                .Where(link => link.Category == selectedCategory)
                .OrderByDescending(link => link.Id)
                .ToListAsync();

        return new IndustryPageViewModel
        {
            Input = input,
            SelectedCategory = selectedCategory,
            DemoLinks = demoLinks
        };
    }

    private IQueryable<IndustryDemoLink> IndustryLinksForCurrentUser()
    {
        var user = PermissionChecker.GetCurrentUser(_db, User);
        var query = _db.IndustryDemoLinks.AsQueryable();
        if (user == null) return query.Where(_ => false);
        if (PermissionChecker.IsSuperAdmin(user)) return query;

        var adminId = user.GetEffectiveAdminId();
        return adminId.HasValue
            ? query.Where(link => link.AdminId == adminId.Value)
            : query.Where(_ => false);
    }

    private bool HasPermission(string action) =>
        PermissionChecker.CanPerformAction(_db, User, "Product", action);

    private IActionResult AccessDenied()
    {
        TempData["ErrorMessage"] = "Access Denied: You do not have permission to manage Industries.";
        return RedirectToAction("List", "Lead");
    }

    private void SetPageState()
    {
        ViewData["Title"] = "Industry Management";
        ViewData["ActivePage"] = "PortfolioIndustry";
        ViewData["CanCreateIndustry"] = HasPermission("Create");
    }

    private static string? ResolveCategory(string? category) =>
        string.IsNullOrWhiteSpace(category)
            ? null
            : IndustryCategories.All.FirstOrDefault(value =>
                string.Equals(value, category.Trim(), StringComparison.OrdinalIgnoreCase));
}
