namespace CrmLeadManagement.Models;

public class PortfolioService
{
    public int Id { get; set; }
    public int? AdminId { get; set; }
    public string ProjectName { get; set; } = "";
    public string Category { get; set; } = "";
    public string Type { get; set; } = "";
    public string Description { get; set; } = "";
    public string Features { get; set; } = "";
    public decimal Price { get; set; }
    public string? DemoLink { get; set; }
}
