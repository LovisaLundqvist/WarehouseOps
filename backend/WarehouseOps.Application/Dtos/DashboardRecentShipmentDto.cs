namespace WarehouseOps.Application.Dtos;

public class DashboardRecentShipmentDto
{
    public Guid Id { get; set; }

    public Guid OrderId { get; set; }

    public string CustomerName { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string TrackingNumber { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}
