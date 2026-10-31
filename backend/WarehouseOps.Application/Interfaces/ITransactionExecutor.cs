namespace WarehouseOps.Application.Interfaces;

public interface ITransactionExecutor
{
    // Runs business changes and their audit log in one database transaction.
    Task<T> ExecuteAsync<T>(Func<Task<T>> operation);
}
