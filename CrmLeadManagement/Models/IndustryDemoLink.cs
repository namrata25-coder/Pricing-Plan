namespace CrmLeadManagement.Models;

public class IndustryDemoLink
{
    public int Id { get; set; }
    public int? AdminId { get; set; }
    public string Category { get; set; } = "";
    public string DemoLink { get; set; } = "";
}

public static class IndustryCategories
{
    public static IReadOnlyList<string> All { get; } =
    [
        "Food",
        "Movie",
        "Healthcare",
        "Education",
        "Retail",
        "Finance",
        "Real Estate",
        "Travel & Hospitality",
        "Manufacturing",
        "Logistics",
        "Technology",
        "Other"
    ];
}
