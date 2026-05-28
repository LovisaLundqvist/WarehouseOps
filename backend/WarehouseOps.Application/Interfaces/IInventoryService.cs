using WarehouseOps.Application.Dtos;

namespace WarehouseOps.Application.Interfaces;

public interface IInventoryService
{
    Task<List<InventoryItemDto>> GetAllAsync();

    Task<List<InventoryItemDto>> GetLowStockAsync();

    Task<InventoryItemDto?> GetByIdAsync(Guid id);

    Task<InventoryItemDto> CreateAsync(CreateInventoryItemRequest request);

    Task<InventoryItemDto?> UpdateAsync(Guid id, UpdateInventoryItemRequest request);
}
