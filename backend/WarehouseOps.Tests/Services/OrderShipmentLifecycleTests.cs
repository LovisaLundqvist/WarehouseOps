using FluentAssertions;
using Moq;
using WarehouseOps.Application.Dtos;
using WarehouseOps.Application.Interfaces;
using WarehouseOps.Application.Services;
using WarehouseOps.Domain;

namespace WarehouseOps.Tests.Services;

public class OrderShipmentLifecycleTests
{
    [Theory]
    [InlineData(OrderStatus.Pending)]
    [InlineData(OrderStatus.Processing)]
    [InlineData(OrderStatus.Shipped)]
    [InlineData(OrderStatus.Completed)]
    public async Task CreateShipment_RequiresPackedOrder(OrderStatus status)
    {
        var order = NewOrder(status);
        var repository = new Mock<IShipmentRepository>();
        repository.Setup(r => r.GetOrderByIdAsync(order.Id)).ReturnsAsync(order);
        var service = new ShipmentService(repository.Object, new Mock<IAuditLogService>().Object);

        Func<Task> action = () => service.CreateAsync(new CreateShipmentRequest
        {
            OrderId = order.Id,
            TrackingNumber = "WH-12345"
        });

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("An order must be packed before a shipment can be created.");
        repository.Verify(r => r.AddAsync(It.IsAny<Shipment>()), Times.Never);
        repository.Verify(r => r.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task ShipmentMarkedShipped_MovesOrderToShippedAndAuditsBoth()
    {
        var (service, repo, audit, shipment) = NewShipmentScenario(
            ShipmentStatus.Packed, OrderStatus.Packed);

        var result = await service.UpdateStatusAsync(shipment.Id,
            new UpdateShipmentStatusRequest { Status = "Shipped" });

        result!.Status.Should().Be("Shipped");
        result.OrderStatus.Should().Be("Shipped");
        shipment.Order!.Status.Should().Be(OrderStatus.Shipped);
        shipment.ShippedDate.Should().NotBeNull();
        shipment.Order.UpdatedAt.Should().NotBeNull();
        repo.Verify(r => r.SaveChangesAsync(), Times.Once);
        audit.Verify(a => a.LogAsync("Shipment", "Updated", "System", It.IsAny<string>()), Times.Once);
        audit.Verify(a => a.LogAsync("Order", "Updated", "System", It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task ShipmentDelivered_CompletesOrderAndAuditsBoth()
    {
        var (service, repo, audit, shipment) = NewShipmentScenario(
            ShipmentStatus.Shipped, OrderStatus.Shipped);
        shipment.ShippedDate = DateTime.UtcNow.AddHours(-1);

        var result = await service.UpdateStatusAsync(shipment.Id,
            new UpdateShipmentStatusRequest { Status = "Delivered" });

        result!.OrderStatus.Should().Be("Completed");
        shipment.Status.Should().Be(ShipmentStatus.Delivered);
        shipment.Order!.Status.Should().Be(OrderStatus.Completed);
        shipment.DeliveredDate.Should().NotBeNull();
        repo.Verify(r => r.SaveChangesAsync(), Times.Once);
        audit.Verify(a => a.LogAsync("Shipment", "Updated", "System", It.IsAny<string>()), Times.Once);
        audit.Verify(a => a.LogAsync("Order", "Updated", "System", It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task LegacyPackedOrder_WithAlreadyShippedShipment_CanBeReconciledOnDelivery()
    {
        var (service, _, _, shipment) = NewShipmentScenario(
            ShipmentStatus.Shipped, OrderStatus.Packed);

        await service.UpdateStatusAsync(shipment.Id,
            new UpdateShipmentStatusRequest { Status = "Delivered" });

        shipment.Order!.Status.Should().Be(OrderStatus.Completed);
    }

    [Theory]
    [InlineData(OrderStatus.Pending)]
    [InlineData(OrderStatus.Processing)]
    [InlineData(OrderStatus.Cancelled)]
    public async Task DeliveryRejected_WhenOrderNotReady(OrderStatus status)
    {
        var (service, repo, audit, shipment) = NewShipmentScenario(
            ShipmentStatus.Shipped, status);

        Func<Task> action = () => service.UpdateStatusAsync(shipment.Id,
            new UpdateShipmentStatusRequest { Status = "Delivered" });

        await action.Should().ThrowAsync<InvalidOperationException>();
        shipment.Status.Should().Be(ShipmentStatus.Shipped);
        shipment.Order!.Status.Should().Be(status);
        repo.Verify(r => r.SaveChangesAsync(), Times.Never);
        audit.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShippedDelayedShipment_CannotBeCancelled()
    {
        var (service, repo, _, shipment) = NewShipmentScenario(
            ShipmentStatus.Delayed, OrderStatus.Shipped);
        shipment.ShippedDate = DateTime.UtcNow.AddHours(-1);

        Func<Task> action = () => service.UpdateStatusAsync(shipment.Id,
            new UpdateShipmentStatusRequest { Status = "Cancelled" });

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("A shipped shipment cannot be cancelled.");
        shipment.Status.Should().Be(ShipmentStatus.Delayed);
        repo.Verify(r => r.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task UnshippedDelayedShipment_CanBeCancelled()
    {
        var (service, repo, _, shipment) = NewShipmentScenario(
            ShipmentStatus.Delayed, OrderStatus.Packed);

        await service.UpdateStatusAsync(shipment.Id,
            new UpdateShipmentStatusRequest { Status = "Cancelled" });

        shipment.Status.Should().Be(ShipmentStatus.Cancelled);
        shipment.Order!.Status.Should().Be(OrderStatus.Packed);
        repo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task RepeatingShippedStatus_DoesNotOverwriteShippingTimeOrWriteAudit()
    {
        var (service, repo, audit, shipment) = NewShipmentScenario(
            ShipmentStatus.Shipped, OrderStatus.Shipped);
        var original = DateTime.UtcNow.AddHours(-2);
        shipment.ShippedDate = original;

        await service.UpdateStatusAsync(shipment.Id,
            new UpdateShipmentStatusRequest { Status = "Shipped" });

        shipment.ShippedDate.Should().Be(original);
        repo.Verify(r => r.SaveChangesAsync(), Times.Never);
        audit.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(ShipmentStatus.Pending)]
    [InlineData(ShipmentStatus.Packed)]
    [InlineData(ShipmentStatus.Shipped)]
    [InlineData(ShipmentStatus.Delayed)]
    [InlineData(ShipmentStatus.Delivered)]
    public async Task CancelOrder_RejectsAnyActiveShipment_BeforeInventoryIsRestored(ShipmentStatus status)
    {
        var (service, repo, audit, order, inventory) = NewOrderScenario(OrderStatus.Packed);
        repo.Setup(r => r.GetShipmentByOrderIdAsync(order.Id))
            .ReturnsAsync(NewShipment(order, status));

        Func<Task> action = () => service.CancelAsync(order.Id);

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Cancel the active shipment before cancelling its order.");
        order.Status.Should().Be(OrderStatus.Packed);
        inventory.QuantityInStock.Should().Be(6);
        repo.Verify(r => r.GetInventoryByProductIdAsync(It.IsAny<Guid>()), Times.Never);
        repo.Verify(r => r.SaveChangesAsync(), Times.Never);
        audit.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task StatusCancellation_RejectsActiveShipment()
    {
        var (service, repo, _, order, inventory) = NewOrderScenario(OrderStatus.Packed);
        repo.Setup(r => r.GetShipmentByOrderIdAsync(order.Id))
            .ReturnsAsync(NewShipment(order, ShipmentStatus.Pending));

        Func<Task> action = () => service.UpdateStatusAsync(order.Id,
            new UpdateOrderStatusRequest { Status = "Cancelled" });

        await action.Should().ThrowAsync<InvalidOperationException>();
        inventory.QuantityInStock.Should().Be(6);
        order.Status.Should().Be(OrderStatus.Packed);
    }

    [Fact]
    public async Task OrderCanBeCancelledAfterShipmentIsCancelled()
    {
        var (service, repo, audit, order, inventory) = NewOrderScenario(OrderStatus.Packed);
        repo.Setup(r => r.GetShipmentByOrderIdAsync(order.Id))
            .ReturnsAsync(NewShipment(order, ShipmentStatus.Cancelled));

        var result = await service.CancelAsync(order.Id);

        result!.Status.Should().Be("Cancelled");
        inventory.QuantityInStock.Should().Be(10);
        repo.Verify(r => r.SaveChangesAsync(), Times.Once);
        audit.Verify(a => a.LogAsync("Order", "Cancelled", "System", It.IsAny<string>()), Times.Once);
    }

    [Theory]
    [InlineData(ShipmentStatus.Pending)]
    [InlineData(ShipmentStatus.Packed)]
    [InlineData(ShipmentStatus.Delayed)]
    public async Task ManualOrderShipping_RejectsShipmentNotYetShipped(ShipmentStatus status)
    {
        var (service, repo, _, order, _) = NewOrderScenario(OrderStatus.Packed);
        repo.Setup(r => r.GetShipmentByOrderIdAsync(order.Id))
            .ReturnsAsync(NewShipment(order, status));

        Func<Task> action = () => service.UpdateStatusAsync(order.Id,
            new UpdateOrderStatusRequest { Status = "Shipped" });

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Order cannot be marked as shipped before its shipment is shipped.");
        order.Status.Should().Be(OrderStatus.Packed);
    }

    [Fact]
    public async Task ManualOrderShipping_RejectsMissingShipment()
    {
        var (service, _, _, order, _) = NewOrderScenario(OrderStatus.Packed);

        Func<Task> action = () => service.UpdateStatusAsync(order.Id,
            new UpdateOrderStatusRequest { Status = "Shipped" });

        await action.Should().ThrowAsync<InvalidOperationException>();
        order.Status.Should().Be(OrderStatus.Packed);
    }

    [Theory]
    [InlineData(ShipmentStatus.Pending)]
    [InlineData(ShipmentStatus.Shipped)]
    [InlineData(ShipmentStatus.Delayed)]
    public async Task ManualOrderCompletion_RejectsUndeliveredShipment(ShipmentStatus status)
    {
        var (service, repo, _, order, _) = NewOrderScenario(OrderStatus.Shipped);
        repo.Setup(r => r.GetShipmentByOrderIdAsync(order.Id))
            .ReturnsAsync(NewShipment(order, status));

        Func<Task> action = () => service.UpdateStatusAsync(order.Id,
            new UpdateOrderStatusRequest { Status = "Completed" });

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Order cannot be completed before its shipment is delivered.");
        order.Status.Should().Be(OrderStatus.Shipped);
    }

    [Fact]
    public async Task ManualOrderCompletion_AllowsDeliveredShipment()
    {
        var (service, repo, _, order, _) = NewOrderScenario(OrderStatus.Shipped);
        repo.Setup(r => r.GetShipmentByOrderIdAsync(order.Id))
            .ReturnsAsync(NewShipment(order, ShipmentStatus.Delivered));

        var result = await service.UpdateStatusAsync(order.Id,
            new UpdateOrderStatusRequest { Status = "Completed" });

        result!.Status.Should().Be("Completed");
        repo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    private static (ShipmentService Service, Mock<IShipmentRepository> Repository,
        Mock<IAuditLogService> Audit, Shipment Shipment) NewShipmentScenario(
        ShipmentStatus shipmentStatus, OrderStatus orderStatus)
    {
        var order = NewOrder(orderStatus);
        var shipment = NewShipment(order, shipmentStatus);
        var repository = new Mock<IShipmentRepository>();
        repository.Setup(r => r.GetByIdAsync(shipment.Id)).ReturnsAsync(shipment);
        repository.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        var audit = NewAuditMock();
        return (new ShipmentService(repository.Object, audit.Object), repository, audit, shipment);
    }

    private static (OrderService Service, Mock<IOrderRepository> Repository,
        Mock<IAuditLogService> Audit, Order Order, InventoryItem Inventory) NewOrderScenario(OrderStatus status)
    {
        var order = NewOrder(status);
        var productId = Guid.NewGuid();
        order.OrderItems.Add(new OrderItem
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            ProductId = productId,
            Quantity = 4,
            UnitPrice = 120,
            CreatedAt = DateTime.UtcNow
        });
        var inventory = new InventoryItem
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            QuantityInStock = 6,
            MinimumStockLevel = 2,
            CreatedAt = DateTime.UtcNow
        };
        var repository = new Mock<IOrderRepository>();
        repository.Setup(r => r.GetByIdAsync(order.Id)).ReturnsAsync(order);
        repository.Setup(r => r.GetInventoryByProductIdAsync(productId)).ReturnsAsync(inventory);
        repository.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        var audit = NewAuditMock();
        return (new OrderService(repository.Object, audit.Object), repository, audit, order, inventory);
    }

    private static Mock<IAuditLogService> NewAuditMock()
    {
        var audit = new Mock<IAuditLogService>();
        audit.Setup(a => a.LogAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        return audit;
    }

    private static Order NewOrder(OrderStatus status)
    {
        return new Order
        {
            Id = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            Status = status,
            CreatedAt = DateTime.UtcNow
        };
    }

    private static Shipment NewShipment(Order order, ShipmentStatus status)
    {
        return new Shipment
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            Order = order,
            Status = status,
            TrackingNumber = "TEST-TRACKING",
            CreatedAt = DateTime.UtcNow
        };
    }
}
