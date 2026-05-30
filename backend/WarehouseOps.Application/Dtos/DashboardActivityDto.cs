namespace WarehouseOps.Application.Dtos;

public class DashboardActivityDto
{
    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string PerformedBy { get; set; } = string.Empty;

    public DateTime PerformedAt { get; set; }
}
