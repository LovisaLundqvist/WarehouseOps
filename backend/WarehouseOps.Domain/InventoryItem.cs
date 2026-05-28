namespace WarehouseOps.Domain;

public class InventoryItem : BaseEntity
{
    public Guid ProductId { get; set; }

    public int QuantityInStock { get; set; }

    public int MinimumStockLevel { get; set; }

    public Product? Product { get; set; }
}