using WarehouseOps.Application.Dtos;
using WarehouseOps.Application.Interfaces;

namespace WarehouseOps.Application.Services;

// Keeps database changes and audit records in the same transaction.
public sealed class TransactionalCustomerService : ICustomerService
{
    private readonly ICustomerService _inner;
    private readonly ITransactionExecutor _transactionExecutor;

    public TransactionalCustomerService(
        ICustomerService inner,
        ITransactionExecutor transactionExecutor)
    {
        _inner = inner;
        _transactionExecutor = transactionExecutor;
    }

    public Task<List<CustomerDto>> GetAllAsync(string? search)
    {
        return _inner.GetAllAsync(search);
    }

    public Task<CustomerDto?> GetByIdAsync(Guid id)
    {
        return _inner.GetByIdAsync(id);
    }

    public Task<CustomerDto> CreateAsync(CreateCustomerRequest request)
    {
        return _transactionExecutor.ExecuteAsync(() => _inner.CreateAsync(request));
    }

    public Task<CustomerDto?> UpdateAsync(Guid id, UpdateCustomerRequest request)
    {
        return _transactionExecutor.ExecuteAsync(() => _inner.UpdateAsync(id, request));
    }

}
