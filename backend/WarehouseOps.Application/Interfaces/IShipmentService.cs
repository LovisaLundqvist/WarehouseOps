using WarehouseOps.Application.Dtos;

namespace WarehouseOps.Application.Interfaces;

public interface IShipmentService
{
    Task<List<ShipmentDto>> GetAllAsync();

    Task<ShipmentDto?> GetByIdAsync(Guid id);

    Task<ShipmentDto> CreateAsync(CreateShipmentRequest request);

    Task<ShipmentDto?> UpdateStatusAsync(Guid id, UpdateShipmentStatusRequest request);
}
