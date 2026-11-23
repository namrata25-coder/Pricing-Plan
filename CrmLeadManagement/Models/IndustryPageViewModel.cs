namespace CrmLeadManagement.Models;

public class IndustryPageViewModel
{
    public IndustryDemoInputModel Input { get; set; } = new();
    public IReadOnlyList<string> Categories { get; set; } = IndustryCategories.All;
    public string? SelectedCategory { get; set; }
    public IReadOnlyList<IndustryDemoLink> DemoLinks { get; set; } = Array.Empty<IndustryDemoLink>();
}
