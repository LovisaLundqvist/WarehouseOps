namespace WarehouseOps.Application.Dtos;

public class InventoryItemDto
{
    public Guid Id { get; set; }

    public Guid ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public string ProductSku { get; set; } = string.Empty;

    public int QuantityInStock { get; set; }

    public int MinimumStockLevel { get; set; }

    public bool IsLowStock { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
