namespace WarehouseOps.Application.Dtos;

public class CreateOrderRequest
{
    public Guid CustomerId { get; set; }

    public List<CreateOrderItemRequest> Items { get; set; } = new();
}
