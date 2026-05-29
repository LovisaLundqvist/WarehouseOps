namespace WarehouseOps.Application.Dtos;

public class CreateShipmentRequest
{
    public Guid OrderId { get; set; }

    public string TrackingNumber { get; set; } = string.Empty;
}
