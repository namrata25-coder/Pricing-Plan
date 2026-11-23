using System.ComponentModel.DataAnnotations;

namespace CrmLeadManagement.Models;

public class ProductInputModel : IValidatableObject
{
    [Required(ErrorMessage = "Product name is required.")]
    [StringLength(200, ErrorMessage = "Product name can be at most 200 characters.")]
    public string Name { get; set; } = "";

    [Required(ErrorMessage = "Description is required.")]
    [StringLength(4000, ErrorMessage = "Description can be at most 4000 characters.")]
    public string Description { get; set; } = "";

    [Required(ErrorMessage = "At least one feature is required.")]
    [StringLength(4000, ErrorMessage = "Features can be at most 4000 characters in total.")]
    public string Features { get; set; } = "";

    [Required(ErrorMessage = "Price is required.")]
    [Range(typeof(decimal), "0", "9999999999999999.99", ErrorMessage = "Price must be 0 or more.")]
    public decimal Price { get; set; }

    private string? _url;

    public IFormFile? Image { get; set; }

    // The form shows a fixed "https://" prefix, so users naturally type "www.example.com".
    // Add the scheme here so that input is accepted instead of failing validation.
    [StringLength(2048, ErrorMessage = "URL can be at most 2048 characters.")]
    public string? Url
    {
        get => _url;
        set
        {
            var trimmed = value?.Trim();
            if (string.IsNullOrEmpty(trimmed))
            {
                _url = null;
            }
            else if (trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                     trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
                     trimmed.Contains("://"))
            {
                _url = trimmed;
            }
            else
            {
                _url = "https://" + trimmed;
            }
        }
    }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!string.IsNullOrWhiteSpace(Url) &&
            (!Uri.TryCreate(Url, UriKind.Absolute, out var uri) ||
             (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) ||
             !uri.Host.Contains('.')))
        {
            yield return new ValidationResult("Enter a valid website address, e.g. www.example.com/product", new[] { nameof(Url) });
        }
    }
}