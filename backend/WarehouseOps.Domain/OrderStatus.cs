namespace WarehouseOps.Domain;

public enum OrderStatus
{
    Pending = 1,
    Processing = 2,
    Packed = 3,
    Shipped = 4,
    Cancelled = 5,
    Completed = 6
}