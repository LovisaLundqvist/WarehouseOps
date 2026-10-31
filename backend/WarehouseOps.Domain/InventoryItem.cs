namespace WarehouseOps.Domain;

public class InventoryItem : BaseEntity
{
    public Guid ProductId { get; set; }

    public int QuantityInStock { get; set; }

    public int MinimumStockLevel { get; set; }

    // SQL Server generates a new value whenever this inventory row changes.
    // EF Core uses it to reject updates based on an outdated stock balance.
    public byte[] RowVersion { get; set; } = [];

    public Product? Product { get; set; }
}
