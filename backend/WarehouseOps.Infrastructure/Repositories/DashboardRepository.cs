using Microsoft.EntityFrameworkCore;
using WarehouseOps.Application.Dtos;
using WarehouseOps.Application.Interfaces;
using WarehouseOps.Domain;

namespace WarehouseOps.Infrastructure.Repositories;

public class DashboardRepository : IDashboardRepository
{
    private readonly ApplicationDbContext _context;

    public DashboardRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<DashboardSummaryDto> GetSummaryAsync()
    {
        var activeOrdersCount = await _context.Orders
            .CountAsync(order => order.Status != OrderStatus.Completed && order.Status != OrderStatus.Cancelled);

        var lowStockItemsCount = await _context.InventoryItems
            .CountAsync(inventoryItem => inventoryItem.QuantityInStock <= inventoryItem.MinimumStockLevel);

        var openIncidentsCount = await _context.Incidents
            .CountAsync(incident => incident.Status != IncidentStatus.Closed);

        var activeShipmentsCount = await _context.Shipments
            .CountAsync(shipment => shipment.Status != ShipmentStatus.Delivered && shipment.Status != ShipmentStatus.Cancelled);

        var orderStatusCounts = await GetOrderStatusCountsAsync();

        var recentActivities = await GetRecentActivitiesAsync();

        var recentShipments = await GetRecentShipmentsAsync();

        return new DashboardSummaryDto
        {
            ActiveOrdersCount = activeOrdersCount,
            LowStockItemsCount = lowStockItemsCount,
            OpenIncidentsCount = openIncidentsCount,
            ActiveShipmentsCount = activeShipmentsCount,
            OrderStatusCounts = orderStatusCounts,
            RecentActivities = recentActivities,
            RecentShipments = recentShipments
        };
    }

    private async Task<List<DashboardStatusCountDto>> GetOrderStatusCountsAsync()
    {
        var groupedOrderCounts = await _context.Orders
            .GroupBy(order => order.Status)
            .Select(group => new
            {
                Status = group.Key,
                Count = group.Count()
            })
            .ToListAsync();

        var countByStatus = groupedOrderCounts.ToDictionary(item => item.Status, item => item.Count);

        return Enum.GetValues<OrderStatus>()
            .Select(status => new DashboardStatusCountDto
            {
                Status = status.ToString(),
                Count = countByStatus.TryGetValue(status, out var count) ? count : 0
            })
            .ToList();
    }

    private async Task<List<DashboardActivityDto>> GetRecentActivitiesAsync()
    {
        var auditLogs = await _context.AuditLogs
            .OrderByDescending(auditLog => auditLog.PerformedAt)
            .Take(5)
            .ToListAsync();

        return auditLogs
            .Select(auditLog => new DashboardActivityDto
            {
                Title = $"{auditLog.EntityName} {auditLog.Action}",
                Description = auditLog.Changes,
                PerformedBy = auditLog.PerformedBy,
                PerformedAt = auditLog.PerformedAt
            })
            .ToList();
    }

    private async Task<List<DashboardRecentShipmentDto>> GetRecentShipmentsAsync()
    {
        var shipments = await _context.Shipments
            .Include(shipment => shipment.Order)
                .ThenInclude(order => order!.Customer)
            .OrderByDescending(shipment => shipment.CreatedAt)
            .Take(5)
            .ToListAsync();

        return shipments
            .Select(shipment => new DashboardRecentShipmentDto
            {
                Id = shipment.Id,
                OrderId = shipment.OrderId,
                CustomerName = shipment.Order?.Customer?.Name ?? string.Empty,
                Status = shipment.Status.ToString(),
                TrackingNumber = shipment.TrackingNumber,
                CreatedAt = shipment.CreatedAt
            })
            .ToList();
    }
}
