using WarehouseOps.Application.Dtos;
using WarehouseOps.Application.Interfaces;

namespace WarehouseOps.Application.Services;

// Keeps database changes and audit records in the same transaction.
public sealed class TransactionalProductService : IProductService
{
    private readonly IProductService _inner;
    private readonly ITransactionExecutor _transactionExecutor;

    public TransactionalProductService(
        IProductService inner,
        ITransactionExecutor transactionExecutor)
    {
        _inner = inner;
        _transactionExecutor = transactionExecutor;
    }

    public Task<List<ProductDto>> GetAllAsync(string? search, string? category)
    {
        return _inner.GetAllAsync(search, category);
    }

    public Task<ProductDto?> GetByIdAsync(Guid id)
    {
        return _inner.GetByIdAsync(id);
    }

    public Task<ProductDto> CreateAsync(CreateProductRequest request)
    {
        return _transactionExecutor.ExecuteAsync(() => _inner.CreateAsync(request));
    }

    public Task<ProductDto?> UpdateAsync(Guid id, UpdateProductRequest request)
    {
        return _transactionExecutor.ExecuteAsync(() => _inner.UpdateAsync(id, request));
    }

    public Task<bool> DeleteAsync(Guid id)
    {
        return _transactionExecutor.ExecuteAsync(() => _inner.DeleteAsync(id));
    }

}
