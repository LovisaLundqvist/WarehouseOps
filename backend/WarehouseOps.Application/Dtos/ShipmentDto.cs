namespace WarehouseOps.Application.Dtos;

public class ShipmentDto
{
    public Guid Id { get; set; }

    public Guid OrderId { get; set; }

    public string OrderStatus { get; set; } = string.Empty;

    public string CustomerName { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string TrackingNumber { get; set; } = string.Empty;

    public DateTime? ShippedDate { get; set; }

    public DateTime? DeliveredDate { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
