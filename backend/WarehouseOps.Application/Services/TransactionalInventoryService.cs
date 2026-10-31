using WarehouseOps.Application.Dtos;
using WarehouseOps.Application.Interfaces;

namespace WarehouseOps.Application.Services;

// Runs inventory changes and their audit entries in one database transaction.
public sealed class TransactionalInventoryService : IInventoryService
{
    private readonly IInventoryService _inner;
    private readonly ITransactionExecutor _transactionExecutor;

    public TransactionalInventoryService(
        IInventoryService inner,
        ITransactionExecutor transactionExecutor)
    {
        _inner = inner;
        _transactionExecutor = transactionExecutor;
    }

    public Task<List<InventoryItemDto>> GetAllAsync()
    {
        return _inner.GetAllAsync();
    }

    public Task<List<InventoryItemDto>> GetLowStockAsync()
    {
        return _inner.GetLowStockAsync();
    }

    public Task<InventoryItemDto?> GetByIdAsync(Guid id)
    {
        return _inner.GetByIdAsync(id);
    }

    public Task<InventoryItemDto> CreateAsync(CreateInventoryItemRequest request)
    {
        return _transactionExecutor.ExecuteAsync(() => _inner.CreateAsync(request));
    }

    public Task<InventoryItemDto?> UpdateAsync(Guid id, UpdateInventoryItemRequest request)
    {
        return _transactionExecutor.ExecuteAsync(() => _inner.UpdateAsync(id, request));
    }
}
