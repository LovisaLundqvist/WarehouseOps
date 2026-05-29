using WarehouseOps.Domain;

namespace WarehouseOps.Application.Interfaces;

public interface IOrderRepository
{
    Task<List<Order>> GetAllAsync();

    Task<Order?> GetByIdAsync(Guid id);

    Task<bool> CustomerExistsAsync(Guid customerId);

    Task<Product?> GetProductByIdAsync(Guid productId);

    Task<InventoryItem?> GetInventoryByProductIdAsync(Guid productId);

    Task AddAsync(Order order);

    Task SaveChangesAsync();
}
