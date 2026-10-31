using Microsoft.EntityFrameworkCore;
using WarehouseOps.Application.Interfaces;

namespace WarehouseOps.Infrastructure.Services;

public sealed class EfTransactionExecutor : ITransactionExecutor
{
    private readonly ApplicationDbContext _context;

    public EfTransactionExecutor(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation)
    {
        ArgumentNullException.ThrowIfNull(operation);

        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var result = await operation();
            await transaction.CommitAsync();
            return result;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
