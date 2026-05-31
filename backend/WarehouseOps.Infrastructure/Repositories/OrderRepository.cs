using Microsoft.EntityFrameworkCore;
using WarehouseOps.Application.Interfaces;
using WarehouseOps.Domain;

namespace WarehouseOps.Infrastructure.Repositories;

public class OrderRepository : IOrderRepository
{
    private readonly ApplicationDbContext _context;

    public OrderRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Order>> GetAllAsync()
    {
        return await _context.Orders
            .Include(order => order.Customer)
            .Include(order => order.OrderItems)
                .ThenInclude(orderItem => orderItem.Product)
            .OrderByDescending(order => order.CreatedAt)
            .ToListAsync();
    }

    public async Task<Order?> GetByIdAsync(Guid id)
    {
        return await _context.Orders
            .Include(order => order.Customer)
            .Include(order => order.OrderItems)
                .ThenInclude(orderItem => orderItem.Product)
            .FirstOrDefaultAsync(order => order.Id == id);
    }

    public async Task<bool> CustomerExistsAsync(Guid customerId)
    {
        return await _context.Customers
            .AnyAsync(customer => customer.Id == customerId);
    }

    public async Task<Product?> GetProductByIdAsync(Guid productId)
    {
        return await _context.Products
            .FirstOrDefaultAsync(product => product.Id == productId);
    }

    public async Task<InventoryItem?> GetInventoryByProductIdAsync(Guid productId)
    {
        return await _context.InventoryItems
            .FirstOrDefaultAsync(inventoryItem => inventoryItem.ProductId == productId);
    }

    public async Task AddAsync(Order order)
    {
        await _context.Orders.AddAsync(order);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
