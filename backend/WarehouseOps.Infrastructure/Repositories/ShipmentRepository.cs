using Microsoft.EntityFrameworkCore;
using WarehouseOps.Application.Interfaces;
using WarehouseOps.Domain;

namespace WarehouseOps.Infrastructure.Repositories;

public class ShipmentRepository : IShipmentRepository
{
    private readonly ApplicationDbContext _context;

    public ShipmentRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Shipment>> GetAllAsync()
    {
        return await _context.Shipments
            .Include(shipment => shipment.Order)
                .ThenInclude(order => order!.Customer)
            .OrderByDescending(shipment => shipment.CreatedAt)
            .ToListAsync();
    }

    public async Task<Shipment?> GetByIdAsync(Guid id)
    {
        return await _context.Shipments
            .Include(shipment => shipment.Order)
                .ThenInclude(order => order!.Customer)
            .FirstOrDefaultAsync(shipment => shipment.Id == id);
    }

    public async Task<Order?> GetOrderByIdAsync(Guid orderId)
    {
        return await _context.Orders
            .Include(order => order.Customer)
            .FirstOrDefaultAsync(order => order.Id == orderId);
    }

    public async Task<bool> ShipmentExistsForOrderAsync(Guid orderId)
    {
        return await _context.Shipments
            .AnyAsync(shipment => shipment.OrderId == orderId);
    }

    public async Task AddAsync(Shipment shipment)
    {
        await _context.Shipments.AddAsync(shipment);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
