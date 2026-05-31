using FluentAssertions;
using Moq;
using WarehouseOps.Application.Dtos;
using WarehouseOps.Application.Interfaces;
using WarehouseOps.Application.Services;
using WarehouseOps.Domain;

namespace WarehouseOps.Tests.Services;

public class OrderServiceTests
{
    private readonly Mock<IOrderRepository> _orderRepositoryMock;
    private readonly Mock<IAuditLogService> _auditLogServiceMock;
    private readonly OrderService _orderService;

    public OrderServiceTests()
    {
        _orderRepositoryMock = new Mock<IOrderRepository>();
        _auditLogServiceMock = new Mock<IAuditLogService>();

        _orderService = new OrderService(
            _orderRepositoryMock.Object,
            _auditLogServiceMock.Object);
    }

    [Fact]
    public async Task CreateAsync_ShouldCreateOrder_WhenRequestIsValid()
    {
        var customerId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        var customer = CreateCustomer(customerId);
        var product = CreateProduct(productId);
        var inventoryItem = CreateInventoryItem(productId, product, 10, 2);

        Order? createdOrder = null;

        var request = new CreateOrderRequest
        {
            CustomerId = customerId,
            Items =
            [
                new CreateOrderItemRequest
                {
                    ProductId = productId,
                    Quantity = 3
                }
            ]
        };

        _orderRepositoryMock
            .Setup(repository => repository.CustomerExistsAsync(customerId))
            .ReturnsAsync(true);

        _orderRepositoryMock
            .Setup(repository => repository.GetProductByIdAsync(productId))
            .ReturnsAsync(product);

        _orderRepositoryMock
            .Setup(repository => repository.GetInventoryByProductIdAsync(productId))
            .ReturnsAsync(inventoryItem);

        _orderRepositoryMock
            .Setup(repository => repository.AddAsync(It.IsAny<Order>()))
            .Callback<Order>(order =>
            {
                createdOrder = order;
                createdOrder.Customer = customer;

                foreach (var orderItem in createdOrder.OrderItems)
                {
                    orderItem.Product = product;
                }
            })
            .Returns(Task.CompletedTask);

        _orderRepositoryMock
            .Setup(repository => repository.SaveChangesAsync())
            .Returns(Task.CompletedTask);

        _orderRepositoryMock
            .Setup(repository => repository.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(() => createdOrder);

        _auditLogServiceMock
            .Setup(service => service.LogAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var result = await _orderService.CreateAsync(request);

        result.CustomerId.Should().Be(customerId);
        result.CustomerName.Should().Be("Nordic Retail AB");
        result.Status.Should().Be("Pending");
        result.Items.Should().HaveCount(1);
        result.Items[0].ProductId.Should().Be(productId);
        result.Items[0].ProductName.Should().Be("Barcode Scanner");
        result.Items[0].Quantity.Should().Be(3);
        result.Items[0].UnitPrice.Should().Be(1399);
        result.TotalAmount.Should().Be(4197);

        inventoryItem.QuantityInStock.Should().Be(7);
        inventoryItem.UpdatedAt.Should().NotBeNull();

        _orderRepositoryMock.Verify(
            repository => repository.AddAsync(It.IsAny<Order>()),
            Times.Once);

        _orderRepositoryMock.Verify(
            repository => repository.SaveChangesAsync(),
            Times.Once);

        _auditLogServiceMock.Verify(
            service => service.LogAsync(
                "Order",
                "Created",
                "System",
                It.Is<string>(changes => changes.Contains("Created order"))),
            Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ShouldThrowArgumentException_WhenCustomerIdIsMissing()
    {
        var request = new CreateOrderRequest
        {
            CustomerId = Guid.Empty,
            Items =
            [
                new CreateOrderItemRequest
                {
                    ProductId = Guid.NewGuid(),
                    Quantity = 1
                }
            ]
        };

        var action = async () => await _orderService.CreateAsync(request);

        await action.Should()
            .ThrowAsync<ArgumentException>()
            .WithMessage("Customer id is required.");

        _orderRepositoryMock.Verify(
            repository => repository.AddAsync(It.IsAny<Order>()),
            Times.Never);

        _orderRepositoryMock.Verify(
            repository => repository.SaveChangesAsync(),
            Times.Never);
    }

    [Fact]
    public async Task CreateAsync_ShouldThrowInvalidOperationException_WhenCustomerDoesNotExist()
    {
        var customerId = Guid.NewGuid();

        var request = new CreateOrderRequest
        {
            CustomerId = customerId,
            Items =
            [
                new CreateOrderItemRequest
                {
                    ProductId = Guid.NewGuid(),
                    Quantity = 1
                }
            ]
        };

        _orderRepositoryMock
            .Setup(repository => repository.CustomerExistsAsync(customerId))
            .ReturnsAsync(false);

        var action = async () => await _orderService.CreateAsync(request);

        await action.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("Customer does not exist.");

        _orderRepositoryMock.Verify(
            repository => repository.AddAsync(It.IsAny<Order>()),
            Times.Never);

        _orderRepositoryMock.Verify(
            repository => repository.SaveChangesAsync(),
            Times.Never);
    }

    [Fact]
    public async Task CreateAsync_ShouldThrowArgumentException_WhenSameProductIsAddedMoreThanOnce()
    {
        var customerId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        var request = new CreateOrderRequest
        {
            CustomerId = customerId,
            Items =
            [
                new CreateOrderItemRequest
                {
                    ProductId = productId,
                    Quantity = 1
                },
                new CreateOrderItemRequest
                {
                    ProductId = productId,
                    Quantity = 2
                }
            ]
        };

        _orderRepositoryMock
            .Setup(repository => repository.CustomerExistsAsync(customerId))
            .ReturnsAsync(true);

        var action = async () => await _orderService.CreateAsync(request);

        await action.Should()
            .ThrowAsync<ArgumentException>()
            .WithMessage("An order cannot contain the same product more than once.");

        _orderRepositoryMock.Verify(
            repository => repository.AddAsync(It.IsAny<Order>()),
            Times.Never);

        _orderRepositoryMock.Verify(
            repository => repository.SaveChangesAsync(),
            Times.Never);
    }

    [Fact]
    public async Task CreateAsync_ShouldThrowInvalidOperationException_WhenInventoryItemDoesNotExist()
    {
        var customerId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var product = CreateProduct(productId);

        var request = new CreateOrderRequest
        {
            CustomerId = customerId,
            Items =
            [
                new CreateOrderItemRequest
                {
                    ProductId = productId,
                    Quantity = 1
                }
            ]
        };

        _orderRepositoryMock
            .Setup(repository => repository.CustomerExistsAsync(customerId))
            .ReturnsAsync(true);

        _orderRepositoryMock
            .Setup(repository => repository.GetProductByIdAsync(productId))
            .ReturnsAsync(product);

        _orderRepositoryMock
            .Setup(repository => repository.GetInventoryByProductIdAsync(productId))
            .ReturnsAsync((InventoryItem?)null);

        var action = async () => await _orderService.CreateAsync(request);

        await action.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("Inventory item does not exist for product Barcode Scanner.");

        _orderRepositoryMock.Verify(
            repository => repository.AddAsync(It.IsAny<Order>()),
            Times.Never);

        _orderRepositoryMock.Verify(
            repository => repository.SaveChangesAsync(),
            Times.Never);
    }

    [Fact]
    public async Task CreateAsync_ShouldThrowInvalidOperationException_WhenStockIsTooLow()
    {
        var customerId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        var product = CreateProduct(productId);
        var inventoryItem = CreateInventoryItem(productId, product, 2, 1);

        var request = new CreateOrderRequest
        {
            CustomerId = customerId,
            Items =
            [
                new CreateOrderItemRequest
                {
                    ProductId = productId,
                    Quantity = 5
                }
            ]
        };

        _orderRepositoryMock
            .Setup(repository => repository.CustomerExistsAsync(customerId))
            .ReturnsAsync(true);

        _orderRepositoryMock
            .Setup(repository => repository.GetProductByIdAsync(productId))
            .ReturnsAsync(product);

        _orderRepositoryMock
            .Setup(repository => repository.GetInventoryByProductIdAsync(productId))
            .ReturnsAsync(inventoryItem);

        var action = async () => await _orderService.CreateAsync(request);

        await action.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("Not enough stock for product Barcode Scanner.");

        inventoryItem.QuantityInStock.Should().Be(2);

        _orderRepositoryMock.Verify(
            repository => repository.AddAsync(It.IsAny<Order>()),
            Times.Never);

        _orderRepositoryMock.Verify(
            repository => repository.SaveChangesAsync(),
            Times.Never);
    }

    [Fact]
    public async Task CancelAsync_ShouldReturnItemsToInventory_WhenOrderCanBeCancelled()
    {
        var orderId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        var product = CreateProduct(productId);
        var inventoryItem = CreateInventoryItem(productId, product, 5, 2);

        var order = new Order
        {
            Id = orderId,
            CustomerId = customerId,
            Customer = CreateCustomer(customerId),
            Status = OrderStatus.Processing,
            TotalAmount = 2798,
            CreatedAt = DateTime.UtcNow,
            OrderItems =
            [
                new OrderItem
                {
                    Id = Guid.NewGuid(),
                    OrderId = orderId,
                    ProductId = productId,
                    Product = product,
                    Quantity = 2,
                    UnitPrice = 1399,
                    CreatedAt = DateTime.UtcNow
                }
            ]
        };

        _orderRepositoryMock
            .Setup(repository => repository.GetByIdAsync(orderId))
            .ReturnsAsync(order);

        _orderRepositoryMock
            .Setup(repository => repository.GetInventoryByProductIdAsync(productId))
            .ReturnsAsync(inventoryItem);

        _orderRepositoryMock
            .Setup(repository => repository.SaveChangesAsync())
            .Returns(Task.CompletedTask);

        _auditLogServiceMock
            .Setup(service => service.LogAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var result = await _orderService.CancelAsync(orderId);

        result.Should().NotBeNull();
        result!.Status.Should().Be("Cancelled");

        order.Status.Should().Be(OrderStatus.Cancelled);
        order.UpdatedAt.Should().NotBeNull();

        inventoryItem.QuantityInStock.Should().Be(7);
        inventoryItem.UpdatedAt.Should().NotBeNull();

        _orderRepositoryMock.Verify(
            repository => repository.SaveChangesAsync(),
            Times.Once);

        _auditLogServiceMock.Verify(
            service => service.LogAsync(
                "Order",
                "Cancelled",
                "System",
                It.Is<string>(changes =>
                    changes.Contains("Cancelled order") &&
                    changes.Contains("returned 2 item(s) to inventory"))),
            Times.Once);
    }

    [Fact]
    public async Task CancelAsync_ShouldThrowInvalidOperationException_WhenOrderIsShipped()
    {
        var orderId = Guid.NewGuid();

        var order = new Order
        {
            Id = orderId,
            CustomerId = Guid.NewGuid(),
            Status = OrderStatus.Shipped,
            CreatedAt = DateTime.UtcNow
        };

        _orderRepositoryMock
            .Setup(repository => repository.GetByIdAsync(orderId))
            .ReturnsAsync(order);

        var action = async () => await _orderService.CancelAsync(orderId);

        await action.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("Shipped or completed orders cannot be cancelled.");

        _orderRepositoryMock.Verify(
            repository => repository.SaveChangesAsync(),
            Times.Never);
    }

    [Fact]
    public async Task UpdateStatusAsync_ShouldThrowInvalidOperationException_WhenStatusChangeIsInvalid()
    {
        var orderId = Guid.NewGuid();

        var order = new Order
        {
            Id = orderId,
            CustomerId = Guid.NewGuid(),
            Status = OrderStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        var request = new UpdateOrderStatusRequest
        {
            Status = "Completed"
        };

        _orderRepositoryMock
            .Setup(repository => repository.GetByIdAsync(orderId))
            .ReturnsAsync(order);

        var action = async () => await _orderService.UpdateStatusAsync(orderId, request);

        await action.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("Order status cannot be changed from Pending to Completed.");

        order.Status.Should().Be(OrderStatus.Pending);

        _orderRepositoryMock.Verify(
            repository => repository.SaveChangesAsync(),
            Times.Never);
    }

    private static Customer CreateCustomer(Guid customerId)
    {
        return new Customer
        {
            Id = customerId,
            Name = "Nordic Retail AB",
            Email = "logistics@nordic-retail.se",
            PhoneNumber = "+46 31 123 456",
            Address = "Lagergatan 12, Göteborg",
            CreatedAt = DateTime.UtcNow
        };
    }

    private static Product CreateProduct(Guid productId)
    {
        return new Product
        {
            Id = productId,
            Name = "Barcode Scanner",
            Sku = "SCAN-001",
            Category = "Equipment",
            Description = "Wireless scanner",
            Price = 1399,
            CreatedAt = DateTime.UtcNow
        };
    }

    private static InventoryItem CreateInventoryItem(
        Guid productId,
        Product product,
        int quantityInStock,
        int minimumStockLevel)
    {
        return new InventoryItem
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            Product = product,
            QuantityInStock = quantityInStock,
            MinimumStockLevel = minimumStockLevel,
            CreatedAt = DateTime.UtcNow
        };
    }
}
