using WarehouseOps.Application.Interfaces;

namespace WarehouseOps.Tests.Helpers;

// Counts transaction requests in service decorator tests.
public sealed class RecordingTransactionExecutor : ITransactionExecutor
{
    public int Executions { get; private set; }

    public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation)
    {
        Executions++;
        return await operation();
    }
}
