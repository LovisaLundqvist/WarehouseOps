namespace WarehouseOps.Application.Dtos;

public class UpdateInventoryItemRequest
{
    public int QuantityInStock { get; set; }

    public int MinimumStockLevel { get; set; }
}
