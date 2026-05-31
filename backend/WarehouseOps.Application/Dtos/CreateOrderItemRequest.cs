namespace WarehouseOps.Application.Dtos;

public class CreateOrderItemRequest
{
    public Guid ProductId { get; set; }

    public int Quantity { get; set; }
}
