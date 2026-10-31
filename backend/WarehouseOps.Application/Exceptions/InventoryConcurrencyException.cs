namespace WarehouseOps.Application.Exceptions;

public sealed class InventoryConcurrencyException : Exception
{
    public InventoryConcurrencyException(Exception innerException)
        : base("Inventory was changed by another operation. Refresh and try again.", innerException)
    {
    }
}
