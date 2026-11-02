using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using WarehouseOps.Api.Controllers;
using WarehouseOps.Application.Dtos;
using WarehouseOps.Application.Exceptions;
using WarehouseOps.Application.Interfaces;
using WarehouseOps.Application.Services;
using WarehouseOps.Domain;

namespace WarehouseOps.Tests.Services;

public class OrderCancellationRegressionTests
{
    [Theory]
    [InlineData(OrderStatus.Pending)]
    [InlineData(OrderStatus.Processing)]
    [InlineData(OrderStatus.Packed)]
    public async Task StatusEndpointCancellation_RestoresReservedStock_AndAuditsOnce(
        OrderStatus initialStatus)
    {
        var (service, orders, audit, order, inventory) = CreateScenario(initialStatus);

        var result = await service.UpdateStatusAsync(order.Id,
            new UpdateOrderStatusRequest { Status = "Cancelled" });

        result.Should().NotBeNull();
        result!.Status.Should().Be("Cancelled");
        order.Status.Should().Be(OrderStatus.Cancelled);
        inventory.QuantityInStock.Should().Be(10);
        inventory.UpdatedAt.Should().NotBeNull();
        orders.Verify(repository => repository.SaveChangesAsync(), Times.Once);
        audit.Verify(log => log.LogAsync(
            "Order", "Cancelled", "System",
            It.Is<string>(changes => changes.Contains("returned 4 item(s) to inventory"))),
            Times.Once);
    }

    [Fact]
    public async Task RepeatedCancellationAcrossBothEndpoints_DoesNotRestoreStockTwice()
    {
        var (service, orders, audit, order, inventory) = CreateScenario(OrderStatus.Processing);

        await service.UpdateStatusAsync(order.Id,
            new UpdateOrderStatusRequest { Status = "Cancelled" });
        await service.CancelAsync(order.Id);
        await service.UpdateStatusAsync(order.Id,
            new UpdateOrderStatusRequest { Status = "Cancelled" });

        inventory.QuantityInStock.Should().Be(10);
        orders.Verify(repository => repository.SaveChangesAsync(), Times.Once);
        audit.Verify(log => log.LogAsync(
            "Order", "Cancelled", "System", It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task CancelEndpoint_UsesSameStockRestorationLogic()
    {
        var (service, orders, audit, order, inventory) = CreateScenario(OrderStatus.Pending);

        var result = await service.CancelAsync(order.Id);

        result!.Status.Should().Be("Cancelled");
        inventory.QuantityInStock.Should().Be(10);
        orders.Verify(repository => repository.SaveChangesAsync(), Times.Once);
        audit.Verify(log => log.LogAsync(
            "Order", "Cancelled", "System", It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task MissingInventory_DoesNotPartiallyRestoreStockOrCancelOrder()
    {
        var (service, orders, audit, order, inventory) = CreateScenario(OrderStatus.Processing);
        var missingProductId = Guid.NewGuid();
        order.OrderItems.Add(new OrderItem
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            ProductId = missingProductId,
            Quantity = 2,
            UnitPrice = 200,
            CreatedAt = DateTime.UtcNow
        });
        orders.Setup(repository => repository.GetInventoryByProductIdAsync(missingProductId))
            .ReturnsAsync((InventoryItem?)null);

        Func<Task> action = () => service.UpdateStatusAsync(order.Id,
            new UpdateOrderStatusRequest { Status = "Cancelled" });

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"Cannot cancel order because inventory for product {missingProductId} is missing.");
        order.Status.Should().Be(OrderStatus.Processing);
        inventory.QuantityInStock.Should().Be(6);
        inventory.UpdatedAt.Should().BeNull();
        orders.Verify(repository => repository.SaveChangesAsync(), Times.Never);
        audit.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShippedOrder_CannotBeCancelledViaStatusEndpoint()
    {
        var (service, orders, audit, order, inventory) = CreateScenario(OrderStatus.Shipped);

        Func<Task> action = () => service.UpdateStatusAsync(order.Id,
            new UpdateOrderStatusRequest { Status = "Cancelled" });

        await action.Should().ThrowAsync<InvalidOperationException>();
        order.Status.Should().Be(OrderStatus.Shipped);
        inventory.QuantityInStock.Should().Be(6);
        orders.Verify(repository => repository.SaveChangesAsync(), Times.Never);
        audit.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task StatusEndpoint_ReturnsConflictForInventoryConcurrencyFailure()
    {
        var service = new Mock<IOrderService>();
        var orderId = Guid.NewGuid();
        var request = new UpdateOrderStatusRequest { Status = "Cancelled" };
        service.Setup(s => s.UpdateStatusAsync(orderId, request))
            .ThrowsAsync(new InventoryConcurrencyException(new Exception("Simulated concurrent update")));
        var controller = new OrdersController(service.Object);

        var response = await controller.UpdateStatus(orderId, request);

        var conflict = response.Result.Should().BeOfType<ConflictObjectResult>().Subject;
        conflict.StatusCode.Should().Be(409);
    }

    private static (
        OrderService Service,
        Mock<IOrderRepository> Orders,
        Mock<IAuditLogService> Audit,
        Order Order,
        InventoryItem Inventory) CreateScenario(OrderStatus status)
    {
        var orderId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var order = new Order
        {
            Id = orderId,
            CustomerId = Guid.NewGuid(),
            Status = status,
            CreatedAt = DateTime.UtcNow,
            OrderItems =
            [
                new OrderItem
                {
                    Id = Guid.NewGuid(),
                    OrderId = orderId,
                    ProductId = productId,
                    Quantity = 4,
                    UnitPrice = 100,
                    CreatedAt = DateTime.UtcNow
                }
            ]
        };
        var inventory = new InventoryItem
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            QuantityInStock = 6,
            MinimumStockLevel = 2,
            CreatedAt = DateTime.UtcNow
        };
        var orders = new Mock<IOrderRepository>();
        orders.Setup(repository => repository.GetByIdAsync(orderId)).ReturnsAsync(order);
        orders.Setup(repository => repository.GetInventoryByProductIdAsync(productId))
            .ReturnsAsync(inventory);
        orders.Setup(repository => repository.SaveChangesAsync())
            .Returns(Task.CompletedTask);

        var audit = new Mock<IAuditLogService>();
        audit.Setup(log => log.LogAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        return (new OrderService(orders.Object, audit.Object), orders, audit, order, inventory);
    }
}
