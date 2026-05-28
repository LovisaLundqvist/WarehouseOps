namespace WarehouseOps.Application.Dtos;

public class CreateInventoryItemRequest
{
    public Guid ProductId { get; set; }

    public int QuantityInStock { get; set; }

    public int MinimumStockLevel { get; set; }
}
