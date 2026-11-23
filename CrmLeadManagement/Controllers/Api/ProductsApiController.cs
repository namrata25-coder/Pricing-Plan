using CrmLeadManagement.Models;
using CrmLeadManagement.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CrmLeadManagement.Controllers;

[Authorize]
public class ProductController : Controller
{
    private const int DefaultPageSize = 10;
    private static readonly string[] AllowedImageExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
    private const long MaxImageSize = 2 * 1024 * 1024; // 2 MB

    private readonly AppDbContext _db;
    private readonly ILogger<ProductController> _logger;
    private readonly IWebHostEnvironment _env;

    public ProductController(AppDbContext db, ILogger<ProductController> logger, IWebHostEnvironment env)
    {
        _db = db;
        _logger = logger;
        _env = env;
    }

    public async Task<IActionResult> Index(string? search, int page = 1)
    {
        if (!HasPermission("View")) return AccessDenied();

        page = Math.Max(page, 1);
        var query = ProductDataScope.Products(_db, CurrentUser()).AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(p => p.Name.Contains(term) ||
                                     p.Description.Contains(term) ||
                                     p.Features.Contains(term));
        }

        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)DefaultPageSize);
        page = totalPages == 0 ? 1 : Math.Min(page, totalPages);

        ViewData["Title"] = "Product Management";
        ViewData["ActivePage"] = "PortfolioProduct";
        ViewData["CanCreateProduct"] = HasPermission("Create");
        ViewData["CanEditProduct"] = HasPermission("Edit");
        ViewData["CanDeleteProduct"] = HasPermission("Delete");
        return View(new ProductListViewModel
        {
            Products = await query.OrderByDescending(p => p.Id)
                .Skip((page - 1) * DefaultPageSize)
                .Take(DefaultPageSize)
                .ToListAsync(),
            Search = search?.Trim() ?? "",
            Page = page,
            PageSize = DefaultPageSize,
            TotalCount = totalCount,
            TotalPages = totalPages
        });
    }

    [HttpGet]
    public IActionResult Create()
    {
        if (!HasPermission("Create")) return AccessDenied();
        SetPageState();
        return View(new ProductInputModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProductInputModel input)
    {
        if (!HasPermission("Create")) return AccessDenied();

        var imageError = ValidateImage(input.Image);
        if (imageError != null) ModelState.AddModelError(nameof(input.Image), imageError);

        if (!ModelState.IsValid)
        {
            SetPageState();
            return View(input);
        }

        string? imagePath = input.Image is { Length: > 0 }
            ? await SaveImageAsync(input.Image)
            : null;

        var user = CurrentUser();
        var product = new Product
        {
            AdminId = PermissionChecker.IsSuperAdmin(user) ? null : user?.GetEffectiveAdminId(),
            Name = input.Name.Trim(),
            Description = input.Description.Trim(),
            Features = input.Features.Trim(),
            Price = input.Price,
            Url = NormalizeUrl(input.Url),
            ImagePath = imagePath
        };
        _db.Products.Add(product);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            _db.Entry(product).State = EntityState.Detached;
            DeleteImage(imagePath);
            _logger.LogError(ex, "Failed to save new product '{ProductName}'", product.Name);
            ModelState.AddModelError(string.Empty, "Product could not be saved to the database. Please try again or contact support.");
            SetPageState();
            return View(input);
        }

        TempData["SuccessMessage"] = "Product added successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        if (!HasPermission("View")) return AccessDenied();
        var product = await FindProduct(id);
        if (product == null) return ProductNotFound();

        SetPageState();
        return View(product);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        if (!HasPermission("Edit")) return AccessDenied();
        var product = await FindProduct(id);
        if (product == null) return ProductNotFound();

        SetPageState();
        ViewData["ProductId"] = id;
        ViewData["ImagePath"] = product.ImagePath;
        return View(new ProductInputModel
        {
            Name = product.Name,
            Description = product.Description,
            Features = product.Features,
            Price = product.Price,
            Url = product.Url
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ProductInputModel input)
    {
        if (!HasPermission("Edit")) return AccessDenied();
        var product = await FindProduct(id);
        if (product == null) return ProductNotFound();

        var imageError = ValidateImage(input.Image);
        if (imageError != null) ModelState.AddModelError(nameof(input.Image), imageError);

        if (!ModelState.IsValid)
        {
            SetPageState();
            ViewData["ProductId"] = id;
            ViewData["ImagePath"] = product.ImagePath;
            return View(input);
        }

        var oldImagePath = product.ImagePath;
        string? newImagePath = null;
        if (input.Image is { Length: > 0 })
        {
            newImagePath = await SaveImageAsync(input.Image);
            product.ImagePath = newImagePath;
        }

        product.Name = input.Name.Trim();
        product.Description = input.Description.Trim();
        product.Features = input.Features.Trim();
        product.Price = input.Price;
        product.Url = NormalizeUrl(input.Url);

        try
        {
            await _db.SaveChangesAsync();
            if (newImagePath != null) DeleteImage(oldImagePath);
        }
        catch (DbUpdateException ex)
        {
            DeleteImage(newImagePath);
            _logger.LogError(ex, "Failed to update product {ProductId}", id);
            ModelState.AddModelError(string.Empty, "Product changes could not be saved to the database. Please try again or contact support.");
            await _db.Entry(product).ReloadAsync();
            SetPageState();
            ViewData["ProductId"] = id;
            ViewData["ImagePath"] = product.ImagePath;
            return View(input);
        }

        TempData["SuccessMessage"] = "Product updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        if (!HasPermission("Delete")) return AccessDenied();
        var product = await FindProduct(id);
        if (product == null) return ProductNotFound();

        var imagePath = product.ImagePath;
        _db.Products.Remove(product);
        await _db.SaveChangesAsync();
        DeleteImage(imagePath);

        TempData["SuccessMessage"] = "Product deleted successfully.";
        return RedirectToAction(nameof(Index));
    }

    private UserRoleEntry? CurrentUser() => PermissionChecker.GetCurrentUser(_db, User);

    private bool HasPermission(string action) =>
        PermissionChecker.CanPerformAction(_db, User, "Product", action);

    private async Task<Product?> FindProduct(int id) =>
        await ProductDataScope.Products(_db, CurrentUser()).FirstOrDefaultAsync(p => p.Id == id);

    private IActionResult AccessDenied()
    {
        TempData["ErrorMessage"] = "Access Denied: You do not have permission to manage Products.";
        return RedirectToAction("List", "Lead");
    }

    private IActionResult ProductNotFound()
    {
        TempData["ErrorMessage"] = "Product not found or you do not have access to it.";
        return RedirectToAction(nameof(Index));
    }

    private void SetPageState()
    {
        ViewData["Title"] = "Product Management";
        ViewData["ActivePage"] = "PortfolioProduct";
    }

    private static string? NormalizeUrl(string? url) =>
        string.IsNullOrWhiteSpace(url) ? null : url.Trim();

    // ---------- Image helpers ----------

    private static string? ValidateImage(IFormFile? file)
    {
        if (file == null || file.Length == 0) return null;

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedImageExtensions.Contains(ext))
            return "Only JPG, PNG or WEBP images are allowed.";
        if (file.Length > MaxImageSize)
            return "Image size must be under 2 MB.";
        return null;
    }

    private async Task<string> SaveImageAsync(IFormFile file)
    {
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        var fileName = $"{Guid.NewGuid():N}{ext}";
        var folder = Path.Combine(_env.WebRootPath, "uploads", "products");
        Directory.CreateDirectory(folder);

        await using var stream = new FileStream(Path.Combine(folder, fileName), FileMode.Create);
        await file.CopyToAsync(stream);

        return $"/uploads/products/{fileName}";
    }

    private void DeleteImage(string? imagePath)
    {
        if (string.IsNullOrWhiteSpace(imagePath)) return;

        var fullPath = Path.Combine(_env.WebRootPath,
            imagePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        if (System.IO.File.Exists(fullPath)) System.IO.File.Delete(fullPath);
    }
}