namespace WarehouseOps.Domain;

public class Order : BaseEntity
{
    public Guid CustomerId { get; set; }

    public OrderStatus Status { get; set; }

    public decimal TotalAmount { get; set; }

    public Customer? Customer { get; set; }

    public List<OrderItem> OrderItems { get; set; } = new();
}