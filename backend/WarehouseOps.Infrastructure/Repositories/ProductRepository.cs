using Microsoft.EntityFrameworkCore;
using WarehouseOps.Application.Interfaces;
using WarehouseOps.Domain;

namespace WarehouseOps.Infrastructure.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly ApplicationDbContext _context;

    public ProductRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Product>> GetAllAsync(string? search, string? category)
    {
        var query = _context.Products.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchText = search.Trim();

            query = query.Where(product =>
                product.Name.Contains(searchText) ||
                product.Sku.Contains(searchText) ||
                product.Description.Contains(searchText));
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            var categoryText = category.Trim();

            query = query.Where(product => product.Category == categoryText);
        }

        return await query
            .OrderBy(product => product.Name)
            .ToListAsync();
    }

    public async Task<Product?> GetByIdAsync(Guid id)
    {
        return await _context.Products
            .FirstOrDefaultAsync(product => product.Id == id);
    }

    public async Task<bool> SkuExistsAsync(string sku, Guid? excludedProductId = null)
    {
        return await _context.Products
            .AnyAsync(product =>
                product.Sku == sku &&
                (!excludedProductId.HasValue || product.Id != excludedProductId.Value));
    }

    public async Task AddAsync(Product product)
    {
        await _context.Products.AddAsync(product);
    }

    public void Delete(Product product)
    {
        _context.Products.Remove(product);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}