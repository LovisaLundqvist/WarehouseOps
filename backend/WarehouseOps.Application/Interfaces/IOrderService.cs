using WarehouseOps.Application.Dtos;

namespace WarehouseOps.Application.Interfaces;

public interface IOrderService
{
    Task<List<OrderDto>> GetAllAsync();

    Task<OrderDto?> GetByIdAsync(Guid id);

    Task<OrderDto> CreateAsync(CreateOrderRequest request);

    Task<OrderDto?> UpdateStatusAsync(Guid id, UpdateOrderStatusRequest request);

    Task<OrderDto?> CancelAsync(Guid id);
}
