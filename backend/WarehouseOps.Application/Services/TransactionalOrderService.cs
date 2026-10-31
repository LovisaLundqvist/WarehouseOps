using WarehouseOps.Application.Dtos;
using WarehouseOps.Application.Interfaces;

namespace WarehouseOps.Application.Services;

// Ensures that order changes and audit entries share the same database transaction.
public sealed class TransactionalOrderService : IOrderService
{
    private readonly IOrderService _inner;
    private readonly ITransactionExecutor _transactionExecutor;

    public TransactionalOrderService(
        IOrderService inner,
        ITransactionExecutor transactionExecutor)
    {
        _inner = inner;
        _transactionExecutor = transactionExecutor;
    }

    public Task<List<OrderDto>> GetAllAsync()
    {
        return _inner.GetAllAsync();
    }

    public Task<OrderDto?> GetByIdAsync(Guid id)
    {
        return _inner.GetByIdAsync(id);
    }

    public Task<OrderDto> CreateAsync(CreateOrderRequest request)
    {
        return _transactionExecutor.ExecuteAsync(() => _inner.CreateAsync(request));
    }

    public Task<OrderDto?> UpdateStatusAsync(Guid id, UpdateOrderStatusRequest request)
    {
        return _transactionExecutor.ExecuteAsync(() => _inner.UpdateStatusAsync(id, request));
    }

    public Task<OrderDto?> CancelAsync(Guid id)
    {
        return _transactionExecutor.ExecuteAsync(() => _inner.CancelAsync(id));
    }
}
