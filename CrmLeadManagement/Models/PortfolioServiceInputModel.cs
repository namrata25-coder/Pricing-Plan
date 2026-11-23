using System.ComponentModel.DataAnnotations;

namespace CrmLeadManagement.Models;

public class PortfolioServiceInputModel : IValidatableObject
{
    [Required(ErrorMessage = "Project name is required.")]
    [StringLength(200, ErrorMessage = "Project name can be at most 200 characters.")]
    public string ProjectName { get; set; } = "";

    [Required(ErrorMessage = "Select a service type.")]
    [RegularExpression("^(Web|App)$", ErrorMessage = "Select Web or App.")]
    public string Type { get; set; } = "";

    [Required(ErrorMessage = "Description is required.")]
    [StringLength(4000, ErrorMessage = "Description can be at most 4000 characters.")]
    public string Description { get; set; } = "";

    [Required(ErrorMessage = "At least one feature is required.")]
    [StringLength(4000, ErrorMessage = "Features can be at most 4000 characters in total.")]
    public string Features { get; set; } = "";

    [Required(ErrorMessage = "Price is required.")]
    [Range(typeof(decimal), "0", "9999999999999999.99", ErrorMessage = "Price must be 0 or more.")]
    public decimal Price { get; set; }

    [StringLength(2048, ErrorMessage = "Demo link can be at most 2048 characters.")]
    public string? DemoLink { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!string.IsNullOrWhiteSpace(DemoLink) &&
            (!Uri.TryCreate(DemoLink, UriKind.Absolute, out var uri) ||
             (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) ||
             !uri.Host.Contains('.')))
        {
            yield return new ValidationResult("Enter a valid demo URL, e.g. www.example.com/demo", new[] { nameof(DemoLink) });
        }
    }
}
