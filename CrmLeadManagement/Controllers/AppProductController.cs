using CrmLeadManagement.Models;
using CrmLeadManagement.Models.Dtos.App;
using CrmLeadManagement.Security;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CrmLeadManagement.Controllers.AppApi;

/// <summary>
/// Mobile/App JSON REST API for Portfolio Products.
/// Base route: /api/v1/app/products
/// Features are sent and stored as one string, one feature per line ("\n").
/// Responses also include FeatureList (the same features as an array) for easy display in the app.
/// </summary>
[Route("api/v1/app/products")]
[Produces("application/json")]
[Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme + "," + BearerTokenAuthenticationHandler.SchemeName)]
public class AppProductController : ControllerBase
{
    private const int MaxPageSize = 100;
    private readonly AppDbContext _db;
    private readonly ILogger<AppProductController> _logger;

    public AppProductController(AppDbContext db, ILogger<AppProductController> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// GET /api/v1/app/products?search=crm&amp;page=1&amp;pageSize=20
    /// Lists the current company's products, newest first.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (!CanPerform("View")) return Forbidden("You do not have permission to view products.");

        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var query = ProductDataScope.Products(_db, CurrentUser()).AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(p => p.Name.Contains(term) ||
                                     p.Description.Contains(term) ||
                                     p.Features.Contains(term));
        }

        var totalCount = await query.CountAsync();
        var products = await query.OrderByDescending(p => p.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var result = new AppProductListDto
        {
            Items = products.Select(ToDto).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };
        return Ok(AppApiResponse<AppProductListDto>.Ok(result, "Products retrieved successfully."));
    }

    /// <summary>GET /api/v1/app/products/5</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        if (!CanPerform("View")) return Forbidden("You do not have permission to view products.");
        var product = await ProductDataScope.Products(_db, CurrentUser())
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id);
        if (product == null) return NotFound(AppApiErrorResponse.Fail("Product not found."));

        return Ok(AppApiResponse<AppProductDto>.Ok(ToDto(product), "Product retrieved successfully."));
    }

    /// <summary>
    /// POST /api/v1/app/products
    /// Body: { "Name": "...", "Description": "...", "Features": "Feature 1\nFeature 2", "Price": 999, "Url": "www.example.com" }
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ProductInputModel? input)
    {
        if (!CanPerform("Create")) return Forbidden("You do not have permission to create products.");
        if (input == null) return BadRequest(AppApiErrorResponse.Fail("Request body is required."));
        if (!TryValidateModel(input)) return ValidationFailure();

        var user = CurrentUser();
        var product = new Product
        {
            AdminId = PermissionChecker.IsSuperAdmin(user) ? null : user?.GetEffectiveAdminId(),
            Name = input.Name.Trim(),
            Description = input.Description.Trim(),
            Features = CleanFeatures(input.Features),
            Price = input.Price,
            Url = NormalizeUrl(input.Url)
        };
        _db.Products.Add(product);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            _db.Entry(product).State = EntityState.Detached;
            _logger.LogError(ex, "App API: failed to save new product '{ProductName}'", product.Name);
            return SaveFailure("Product could not be saved to the database. Please try again or contact support.");
        }

        return CreatedAtAction(nameof(GetById), new { id = product.Id },
            AppApiResponse<AppProductDto>.Ok(ToDto(product), "Product created successfully."));
    }

    /// <summary>PUT /api/v1/app/products/5 (same body as POST)</summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] ProductInputModel? input)
    {
        if (!CanPerform("Edit")) return Forbidden("You do not have permission to update products.");
        if (input == null) return BadRequest(AppApiErrorResponse.Fail("Request body is required."));
        if (!TryValidateModel(input)) return ValidationFailure();

        var product = await ProductDataScope.Products(_db, CurrentUser())
            .FirstOrDefaultAsync(p => p.Id == id);
        if (product == null) return NotFound(AppApiErrorResponse.Fail("Product not found."));

        product.Name = input.Name.Trim();
        product.Description = input.Description.Trim();
        product.Features = CleanFeatures(input.Features);
        product.Price = input.Price;
        product.Url = NormalizeUrl(input.Url);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "App API: failed to update product {ProductId}", id);
            await _db.Entry(product).ReloadAsync();
            return SaveFailure("Product changes could not be saved to the database. Please try again or contact support.");
        }

        return Ok(AppApiResponse<AppProductDto>.Ok(ToDto(product), "Product updated successfully."));
    }

    /// <summary>DELETE /api/v1/app/products/5</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        if (!CanPerform("Delete")) return Forbidden("You do not have permission to delete products.");
        var product = await ProductDataScope.Products(_db, CurrentUser())
            .FirstOrDefaultAsync(p => p.Id == id);
        if (product == null) return NotFound(AppApiErrorResponse.Fail("Product not found."));

        _db.Products.Remove(product);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "App API: failed to delete product {ProductId}", id);
            _db.Entry(product).State = EntityState.Unchanged;
            return SaveFailure("Product could not be deleted. Please try again or contact support.");
        }

        return Ok(AppApiResponse.Ok("Product deleted successfully."));
    }

    private UserRoleEntry? CurrentUser() => PermissionChecker.GetCurrentUser(_db, User);

    private bool CanPerform(string action) =>
        PermissionChecker.CanPerformAction(_db, User, "Product", action);

    private IActionResult Forbidden(string message) =>
        StatusCode(StatusCodes.Status403Forbidden, AppApiErrorResponse.Fail(message));

    private IActionResult SaveFailure(string message) =>
        StatusCode(StatusCodes.Status500InternalServerError, AppApiErrorResponse.Fail(message));

    private IActionResult ValidationFailure()
    {
        var errors = ModelState
            .Where(item => item.Value?.Errors.Count > 0)
            .ToDictionary(
                item => item.Key,
                item => item.Value!.Errors
                    .Select(error => string.IsNullOrWhiteSpace(error.ErrorMessage) ? "Invalid value." : error.ErrorMessage)
                    .ToArray());
        return BadRequest(AppApiErrorResponse.Fail("Product validation failed.", errors));
    }

    // Trims each feature line and drops empty / duplicate lines (same rules as the web form).
    private static string CleanFeatures(string features) =>
        string.Join("\n", SplitFeatures(features));

    private static List<string> SplitFeatures(string? features) =>
        (features ?? "")
            .Split('\n')
            .Select(f => f.Trim())
            .Where(f => f.Length > 0)
            .Distinct()
            .ToList();

    private static AppProductDto ToDto(Product product) => new()
    {
        Id = product.Id,
        Name = product.Name,
        Description = product.Description,
        Features = product.Features,
        FeatureList = SplitFeatures(product.Features),
        Price = product.Price,
        Url = product.Url
    };

    private static string? NormalizeUrl(string? url) =>
        string.IsNullOrWhiteSpace(url) ? null : url.Trim();
}

public class AppProductDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Features { get; set; } = "";
    public List<string> FeatureList { get; set; } = new();
    public decimal Price { get; set; }
    public string? Url { get; set; }
}

public class AppProductListDto
{
    public List<AppProductDto> Items { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
}