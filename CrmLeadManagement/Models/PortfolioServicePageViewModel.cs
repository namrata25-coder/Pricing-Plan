namespace CrmLeadManagement.Models;

public class PortfolioServicePageViewModel
{
    public PortfolioServiceInputModel Input { get; set; } = new();
    public IReadOnlyList<PortfolioService> Services { get; set; } = Array.Empty<PortfolioService>();
    public int? EditingServiceId { get; set; }
}
