using WarehouseOps.Application.Dtos;
using WarehouseOps.Application.Interfaces;
using WarehouseOps.Domain;

namespace WarehouseOps.Application.Services;

public class OrderService : IOrderService
{
    private readonly IOrderRepository _orderRepository;

    public OrderService(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
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

        var order = new Order
        {
            Id = Guid.NewGuid(),
            CustomerId = request.CustomerId,
            Status = OrderStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

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

            inventoryItem.QuantityInStock -= requestItem.Quantity;
            inventoryItem.UpdatedAt = DateTime.UtcNow;

            var orderItem = new OrderItem
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                ProductId = product.Id,
                Quantity = requestItem.Quantity,
                UnitPrice = product.Price,
                CreatedAt = DateTime.UtcNow
            };

            order.OrderItems.Add(orderItem);
        }

        order.TotalAmount = order.OrderItems.Sum(orderItem => orderItem.Quantity * orderItem.UnitPrice);

        await _orderRepository.AddAsync(order);

        await _orderRepository.SaveChangesAsync();

        var createdOrder = await _orderRepository.GetByIdAsync(order.Id);

        if (createdOrder is null)
        {
            throw new InvalidOperationException("Order could not be loaded after creation.");
        }

        return MapToDto(createdOrder);
    }

    public async Task<OrderDto?> UpdateStatusAsync(Guid id, UpdateOrderStatusRequest request)
    {
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

        order.Status = newStatus;
        order.UpdatedAt = DateTime.UtcNow;

        await _orderRepository.SaveChangesAsync();

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

        return MapToDto(order);
    }

    private static void ValidateCreateRequest(CreateOrderRequest request)
    {
        if (request.CustomerId == Guid.Empty)
        {
            throw new ArgumentException("Customer id is required.");
        }

        if (request.Items is null || !request.Items.Any())
        {
            throw new ArgumentException("An order must contain at least one order item.");
        }

        foreach (var item in request.Items)
        {
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

        var parsed = Enum.TryParse<OrderStatus>(status.Trim(), true, out var orderStatus);

        if (!parsed)
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
