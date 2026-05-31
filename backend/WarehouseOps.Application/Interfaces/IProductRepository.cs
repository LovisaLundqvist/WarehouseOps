using WarehouseOps.Domain;

namespace WarehouseOps.Application.Interfaces;

public interface IProductRepository
{
    Task<List<Product>> GetAllAsync(string? search, string? category);

    Task<Product?> GetByIdAsync(Guid id);

    Task<bool> SkuExistsAsync(string sku, Guid? excludedProductId = null);

    Task<bool> IsProductInUseAsync(Guid productId);

    Task AddAsync(Product product);

    void Delete(Product product);

    Task SaveChangesAsync();
}
