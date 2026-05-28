namespace WarehouseOps.Domain;

public class Shipment : BaseEntity
{
    public Guid OrderId { get; set; }

    public ShipmentStatus Status { get; set; }

    public string TrackingNumber { get; set; } = string.Empty;

    public DateTime? ShippedDate { get; set; }

    public DateTime? DeliveredDate { get; set; }

    public Order? Order { get; set; }
} 