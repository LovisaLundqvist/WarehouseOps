using WarehouseOps.Application.Dtos;
using WarehouseOps.Application.Interfaces;

namespace WarehouseOps.Application.Services;

// Keeps database changes and audit records in the same transaction.
public sealed class TransactionalShipmentService : IShipmentService
{
    private readonly IShipmentService _inner;
    private readonly ITransactionExecutor _transactionExecutor;

    public TransactionalShipmentService(
        IShipmentService inner,
        ITransactionExecutor transactionExecutor)
    {
        _inner = inner;
        _transactionExecutor = transactionExecutor;
    }

    public Task<List<ShipmentDto>> GetAllAsync()
    {
        return _inner.GetAllAsync();
    }

    public Task<ShipmentDto?> GetByIdAsync(Guid id)
    {
        return _inner.GetByIdAsync(id);
    }

    public Task<ShipmentDto> CreateAsync(CreateShipmentRequest request)
    {
        return _transactionExecutor.ExecuteAsync(() => _inner.CreateAsync(request));
    }

    public Task<ShipmentDto?> UpdateStatusAsync(Guid id, UpdateShipmentStatusRequest request)
    {
        return _transactionExecutor.ExecuteAsync(() => _inner.UpdateStatusAsync(id, request));
    }

}
