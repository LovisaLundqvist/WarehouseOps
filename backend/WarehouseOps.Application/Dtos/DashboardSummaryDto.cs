namespace WarehouseOps.Application.Dtos;

public class DashboardSummaryDto
{
    public int ActiveOrdersCount { get; set; }

    public int LowStockItemsCount { get; set; }

    public int OpenIncidentsCount { get; set; }

    public int ActiveShipmentsCount { get; set; }

    public List<DashboardStatusCountDto> OrderStatusCounts { get; set; } = new();

    public List<DashboardActivityDto> RecentActivities { get; set; } = new();

    public List<DashboardRecentShipmentDto> RecentShipments { get; set; } = new();
}
