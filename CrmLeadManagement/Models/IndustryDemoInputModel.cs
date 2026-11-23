using System.ComponentModel.DataAnnotations;

namespace CrmLeadManagement.Models;

public class IndustryDemoInputModel : IValidatableObject
{
    [Required(ErrorMessage = "Select a category.")]
    [StringLength(200)]
    public string Category { get; set; } = "";

    [Required(ErrorMessage = "Demo link is required.")]
    [StringLength(2048, ErrorMessage = "Demo link can be at most 2048 characters.")]
    public string DemoLink { get; set; } = "";

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!string.IsNullOrWhiteSpace(Category) &&
            !IndustryCategories.All.Contains(Category.Trim(), StringComparer.OrdinalIgnoreCase))
        {
            yield return new ValidationResult("Select a category from the list.", new[] { nameof(Category) });
        }

        if (!string.IsNullOrWhiteSpace(DemoLink) &&
            (!Uri.TryCreate(DemoLink, UriKind.Absolute, out var uri) ||
             (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) ||
             !uri.Host.Contains('.')))
        {
            yield return new ValidationResult("Enter a valid demo URL, e.g. https://example.com/demo", new[] { nameof(DemoLink) });
        }
    }
}
