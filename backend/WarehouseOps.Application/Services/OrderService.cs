using WarehouseOps.Application.Dtos;
using WarehouseOps.Application.Interfaces;
using WarehouseOps.Application.Validation;
using WarehouseOps.Domain;

namespace WarehouseOps.Application.Services;

public class OrderService : IOrderService
{
    private const decimal MaximumOrderAmount = 9999999999999999.99m;

    private readonly IOrderRepository _orderRepository;
    private readonly IAuditLogService _auditLogService;

    public OrderService(IOrderRepository orderRepository, IAuditLogService auditLogService)
    {
        _orderRepository = orderRepository;
        _auditLogService = auditLogService;
    }

    public async Task<List<OrderDto>> GetAllAsync()
    {
        var orders = await _orderRepository.GetAllAsync();

        return orders
            .Select(MapToDto)
            .ToList();
    }

    public async Task<OrderDto?> GetByIdAsync(Guid id)
    {
        var order = await _orderRepository.GetByIdAsync(id);

        if (order is null)
        {
            return null;
        }

        return MapToDto(order);
    }

    public async Task<OrderDto> CreateAsync(CreateOrderRequest request)
    {
        ValidateCreateRequest(request);

        var customerExists = await _orderRepository.CustomerExistsAsync(request.CustomerId);

        if (!customerExists)
        {
            throw new InvalidOperationException("Customer does not exist.");
        }

        var duplicateProductIds = request.Items
            .GroupBy(item => item.ProductId)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToList();

        if (duplicateProductIds.Any())
        {
            throw new ArgumentException("An order cannot contain the same product more than once.");
        }

        // Validate every order line before changing any tracked inventory balances.
        var validatedLines = new List<(Product Product, InventoryItem Inventory, int Quantity)>();
        decimal totalAmount = 0;

        foreach (var requestItem in request.Items)
        {
            var product = await _orderRepository.GetProductByIdAsync(requestItem.ProductId);

            if (product is null)
            {
                throw new InvalidOperationException("Product does not exist.");
            }

            var inventoryItem = await _orderRepository.GetInventoryByProductIdAsync(requestItem.ProductId);

            if (inventoryItem is null)
            {
                throw new InvalidOperationException($"Inventory item does not exist for product {product.Name}.");
            }

            if (inventoryItem.QuantityInStock < requestItem.Quantity)
            {
                throw new InvalidOperationException($"Not enough stock for product {product.Name}.");
            }

            if (product.Price < 0 ||
                product.Price > MaximumOrderAmount / requestItem.Quantity)
            {
                throw new ArgumentException("Order total exceeds the maximum allowed value.");
            }

            var lineTotal = product.Price * requestItem.Quantity;

            if (totalAmount > MaximumOrderAmount - lineTotal)
            {
                throw new ArgumentException("Order total exceeds the maximum allowed value.");
            }

            totalAmount += lineTotal;
            validatedLines.Add((product, inventoryItem, requestItem.Quantity));
        }

        var order = new Order
        {
            Id = Guid.NewGuid(),
            CustomerId = request.CustomerId,
            Status = OrderStatus.Pending,
            TotalAmount = totalAmount,
            CreatedAt = DateTime.UtcNow
        };

        foreach (var line in validatedLines)
        {
            line.Inventory.QuantityInStock -= line.Quantity;
            line.Inventory.UpdatedAt = DateTime.UtcNow;

            order.OrderItems.Add(new OrderItem
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                ProductId = line.Product.Id,
                Quantity = line.Quantity,
                UnitPrice = line.Product.Price,
                CreatedAt = DateTime.UtcNow
            });
        }

        await _orderRepository.AddAsync(order);
        await _orderRepository.SaveChangesAsync();

        var createdOrder = await _orderRepository.GetByIdAsync(order.Id);

        if (createdOrder is null)
        {
            throw new InvalidOperationException("Order could not be loaded after creation.");
        }

        await _auditLogService.LogAsync(
            "Order",
            "Created",
            "System",
            $"Created order {createdOrder.Id} for customer {createdOrder.CustomerId} with {createdOrder.OrderItems.Count} item(s) and total amount {createdOrder.TotalAmount}.");

        return MapToDto(createdOrder);
    }

    public async Task<OrderDto?> UpdateStatusAsync(Guid id, UpdateOrderStatusRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var newStatus = ParseStatus(request.Status);

        var order = await _orderRepository.GetByIdAsync(id);

        if (order is null)
        {
            return null;
        }

        if (!IsValidStatusChange(order.Status, newStatus))
        {
            throw new InvalidOperationException($"Order status cannot be changed from {order.Status} to {newStatus}.");
        }

        var oldStatus = order.Status;

        order.Status = newStatus;
        order.UpdatedAt = DateTime.UtcNow;

        await _orderRepository.SaveChangesAsync();

        await _auditLogService.LogAsync(
            "Order",
            "Updated",
            "System",
            $"Updated order {order.Id} status from {oldStatus} to {order.Status}.");

        return MapToDto(order);
    }

    public async Task<OrderDto?> CancelAsync(Guid id)
    {
        var order = await _orderRepository.GetByIdAsync(id);

        if (order is null)
        {
            return null;
        }

        if (order.Status == OrderStatus.Shipped || order.Status == OrderStatus.Completed)
        {
            throw new InvalidOperationException("Shipped or completed orders cannot be cancelled.");
        }

        if (order.Status == OrderStatus.Cancelled)
        {
            return MapToDto(order);
        }

        foreach (var orderItem in order.OrderItems)
        {
            var inventoryItem = await _orderRepository.GetInventoryByProductIdAsync(orderItem.ProductId);

            if (inventoryItem is not null)
            {
                inventoryItem.QuantityInStock += orderItem.Quantity;
                inventoryItem.UpdatedAt = DateTime.UtcNow;
            }
        }

        order.Status = OrderStatus.Cancelled;
        order.UpdatedAt = DateTime.UtcNow;

        await _orderRepository.SaveChangesAsync();

        await _auditLogService.LogAsync(
            "Order",
            "Cancelled",
            "System",
            $"Cancelled order {order.Id} and returned {order.OrderItems.Sum(orderItem => orderItem.Quantity)} item(s) to inventory.");

        return MapToDto(order);
    }

    private static void ValidateCreateRequest(CreateOrderRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.CustomerId == Guid.Empty)
        {
            throw new ArgumentException("Customer id is required.");
        }

        if (request.Items is null || request.Items.Count == 0)
        {
            throw new ArgumentException("An order must contain at least one order item.");
        }

        foreach (var item in request.Items)
        {
            if (item is null)
            {
                throw new ArgumentException("Order items cannot be null.");
            }

            if (item.ProductId == Guid.Empty)
            {
                throw new ArgumentException("Product id is required.");
            }

            if (item.Quantity <= 0)
            {
                throw new ArgumentException("Quantity must be greater than zero.");
            }
        }
    }

    private static OrderStatus ParseStatus(string status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            throw new ArgumentException("Order status is required.");
        }

        if (!EnumNameParser.TryParse(status, out OrderStatus orderStatus))
        {
            throw new ArgumentException("Order status is invalid. Valid statuses are Pending, Processing, Packed, Shipped, Cancelled and Completed.");
        }

        return orderStatus;
    }

    private static bool IsValidStatusChange(OrderStatus currentStatus, OrderStatus newStatus)
    {
        if (currentStatus == newStatus)
        {
            return true;
        }

        return currentStatus switch
        {
            OrderStatus.Pending => newStatus == OrderStatus.Processing || newStatus == OrderStatus.Cancelled,
            OrderStatus.Processing => newStatus == OrderStatus.Packed || newStatus == OrderStatus.Cancelled,
            OrderStatus.Packed => newStatus == OrderStatus.Shipped || newStatus == OrderStatus.Cancelled,
            OrderStatus.Shipped => newStatus == OrderStatus.Completed,
            OrderStatus.Cancelled => false,
            OrderStatus.Completed => false,
            _ => false
        };
    }

    private static OrderDto MapToDto(Order order)
    {
        return new OrderDto
        {
            Id = order.Id,
            CustomerId = order.CustomerId,
            CustomerName = order.Customer?.Name ?? string.Empty,
            Status = order.Status.ToString(),
            TotalAmount = order.TotalAmount,
            CreatedAt = order.CreatedAt,
            UpdatedAt = order.UpdatedAt,
            Items = order.OrderItems
                .Select(orderItem => new OrderItemDto
                {
                    ProductId = orderItem.ProductId,
                    ProductName = orderItem.Product?.Name ?? string.Empty,
                    ProductSku = orderItem.Product?.Sku ?? string.Empty,
                    Quantity = orderItem.Quantity,
                    UnitPrice = orderItem.UnitPrice,
                    LineTotal = orderItem.Quantity * orderItem.UnitPrice
                })
                .ToList()
        };
    }
}
