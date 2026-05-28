using WarehouseOps.Domain;

namespace WarehouseOps.Application.Interfaces;

public interface IInventoryRepository
{
    Task<List<InventoryItem>> GetAllAsync();

    Task<List<InventoryItem>> GetLowStockAsync();

    Task<InventoryItem?> GetByIdAsync(Guid id);

    Task<bool> ProductExistsAsync(Guid productId);

    Task<bool> InventoryExistsForProductAsync(Guid productId);

    Task AddAsync(InventoryItem inventoryItem);

    Task SaveChangesAsync();
}
