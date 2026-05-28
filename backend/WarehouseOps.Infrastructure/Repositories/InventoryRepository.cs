using Microsoft.EntityFrameworkCore;
using WarehouseOps.Application.Interfaces;
using WarehouseOps.Domain;

namespace WarehouseOps.Infrastructure.Repositories;

public class InventoryRepository : IInventoryRepository
{
    private readonly ApplicationDbContext _context;

    public InventoryRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<InventoryItem>> GetAllAsync()
    {
        return await _context.InventoryItems
            .Include(inventoryItem => inventoryItem.Product)
            .OrderBy(inventoryItem => inventoryItem.Product == null ? string.Empty : inventoryItem.Product.Name)
            .ToListAsync();
    }

    public async Task<List<InventoryItem>> GetLowStockAsync()
    {
        return await _context.InventoryItems
            .Include(inventoryItem => inventoryItem.Product)
            .Where(inventoryItem => inventoryItem.QuantityInStock <= inventoryItem.MinimumStockLevel)
            .OrderBy(inventoryItem => inventoryItem.Product == null ? string.Empty : inventoryItem.Product.Name)
            .ToListAsync();
    }

    public async Task<InventoryItem?> GetByIdAsync(Guid id)
    {
        return await _context.InventoryItems
            .Include(inventoryItem => inventoryItem.Product)
            .FirstOrDefaultAsync(inventoryItem => inventoryItem.Id == id);
    }

    public async Task<bool> ProductExistsAsync(Guid productId)
    {
        return await _context.Products
            .AnyAsync(product => product.Id == productId);
    }

    public async Task<bool> InventoryExistsForProductAsync(Guid productId)
    {
        return await _context.InventoryItems
            .AnyAsync(inventoryItem => inventoryItem.ProductId == productId);
    }

    public async Task AddAsync(InventoryItem inventoryItem)
    {
        await _context.InventoryItems.AddAsync(inventoryItem);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
