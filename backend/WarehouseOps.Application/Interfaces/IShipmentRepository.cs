using WarehouseOps.Domain;

namespace WarehouseOps.Application.Interfaces;

public interface IShipmentRepository
{
    Task<List<Shipment>> GetAllAsync();

    Task<Shipment?> GetByIdAsync(Guid id);

    Task<Order?> GetOrderByIdAsync(Guid orderId);

    Task<bool> ShipmentExistsForOrderAsync(Guid orderId);

    Task AddAsync(Shipment shipment);

    Task SaveChangesAsync();
}
