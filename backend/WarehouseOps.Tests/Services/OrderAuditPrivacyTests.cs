using FluentAssertions;
using Moq;
using WarehouseOps.Application.Dtos;
using WarehouseOps.Application.Interfaces;
using WarehouseOps.Application.Services;
using WarehouseOps.Domain;

namespace WarehouseOps.Tests.Services;

public class OrderAuditPrivacyTests
{
    [Theory]
    [InlineData("Anna Andersson")]
    [InlineData("Customer with private details\nSecond line")]
    public async Task CreateAsync_ShouldLogCustomerIdWithoutCopyingCustomerDetails(string customerName)
    {
        var customerId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var customer = new Customer
        {
            Id = customerId,
            Name = customerName,
            Email = "private.customer@example.com",
            PhoneNumber = "+46 70 123 45 67",
            Address = "Private Street 123",
            CreatedAt = DateTime.UtcNow
        };

        var product = new Product
        {
            Id = productId,
            Name = "Scanner",
            Sku = "SCAN-001",
            Category = "Equipment",
            Price = 50m,
            CreatedAt = DateTime.UtcNow
        };

        var inventory = new InventoryItem
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            Product = product,
            QuantityInStock = 10,
            MinimumStockLevel = 2,
            CreatedAt = DateTime.UtcNow
        };

        var orderRepository = new Mock<IOrderRepository>();
        var auditLogService = new Mock<IAuditLogService>();
        Order? savedOrder = null;
        string? changesLogged = null;

        orderRepository.Setup(repository => repository.CustomerExistsAsync(customerId))
            .ReturnsAsync(true);
        orderRepository.Setup(repository => repository.GetProductByIdAsync(productId))
            .ReturnsAsync(product);
        orderRepository.Setup(repository => repository.GetInventoryByProductIdAsync(productId))
            .ReturnsAsync(inventory);
        orderRepository.Setup(repository => repository.AddAsync(It.IsAny<Order>()))
            .Callback<Order>(order =>
            {
                savedOrder = order;
                savedOrder.Customer = customer;

                foreach (var orderItem in savedOrder.OrderItems)
                {
                    orderItem.Product = product;
                }
            })
            .Returns(Task.CompletedTask);
        orderRepository.Setup(repository => repository.SaveChangesAsync())
            .Returns(Task.CompletedTask);
        orderRepository.Setup(repository => repository.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(() => savedOrder);

        auditLogService.Setup(service => service.LogAsync(
                "Order", "Created", "System", It.IsAny<string>()))
            .Callback<string, string, string, string>((_, _, _, changes) => changesLogged = changes)
            .Returns(Task.CompletedTask);

        var service = new OrderService(orderRepository.Object, auditLogService.Object);
        var request = new CreateOrderRequest
        {
            CustomerId = customerId,
            Items =
            [
                new CreateOrderItemRequest
                {
                    ProductId = productId,
                    Quantity = 2
                }
            ]
        };

        var result = await service.CreateAsync(request);

        result.CustomerName.Should().Be(customerName);
        result.CustomerId.Should().Be(customerId);
        result.TotalAmount.Should().Be(100m);
        inventory.QuantityInStock.Should().Be(8);

        changesLogged.Should().NotBeNull();
        changesLogged.Should().Contain(savedOrder!.Id.ToString());
        changesLogged.Should().Contain(customerId.ToString());
        changesLogged.Should().Contain("1 item(s)");
        changesLogged.Should().NotContain(customerName);
        changesLogged.Should().NotContain(customer.Email);
        changesLogged.Should().NotContain(customer.PhoneNumber);
        changesLogged.Should().NotContain(customer.Address);

        orderRepository.Verify(repository => repository.SaveChangesAsync(), Times.Once);
        auditLogService.Verify(service => service.LogAsync(
            "Order", "Created", "System", It.IsAny<string>()), Times.Once);
    }
}
