using WarehouseOps.Application.Dtos;
using WarehouseOps.Application.Interfaces;

namespace WarehouseOps.Application.Services;

// Keeps database changes and audit records in the same transaction.
public sealed class TransactionalIncidentService : IIncidentService
{
    private readonly IIncidentService _inner;
    private readonly ITransactionExecutor _transactionExecutor;

    public TransactionalIncidentService(
        IIncidentService inner,
        ITransactionExecutor transactionExecutor)
    {
        _inner = inner;
        _transactionExecutor = transactionExecutor;
    }

    public Task<List<IncidentDto>> GetAllAsync(string? status)
    {
        return _inner.GetAllAsync(status);
    }

    public Task<IncidentDto?> GetByIdAsync(Guid id)
    {
        return _inner.GetByIdAsync(id);
    }

    public Task<IncidentDto> CreateAsync(CreateIncidentRequest request)
    {
        return _transactionExecutor.ExecuteAsync(() => _inner.CreateAsync(request));
    }

    public Task<IncidentDto?> UpdateStatusAsync(Guid id, UpdateIncidentStatusRequest request)
    {
        return _transactionExecutor.ExecuteAsync(() => _inner.UpdateStatusAsync(id, request));
    }

    public Task<IncidentDto?> ResolveAsync(Guid id, ResolveIncidentRequest request)
    {
        return _transactionExecutor.ExecuteAsync(() => _inner.ResolveAsync(id, request));
    }

}
