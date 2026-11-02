using FluentAssertions;
using Moq;
using WarehouseOps.Application.Dtos;
using WarehouseOps.Application.Interfaces;
using WarehouseOps.Application.Services;
using WarehouseOps.Domain;

namespace WarehouseOps.Tests.Services;

public class ShipmentDelayWorkflowTests
{
    [Fact]
    public async Task PendingDelayed_CannotBeDispatchedWithoutPacking()
    {
        var (service, repository, audit, shipment) = Scenario(ShipmentStatus.Pending);

        await ChangeAsync(service, shipment, "Delayed");
        Func<Task> action = () => ChangeAsync(service, shipment, "Shipped");

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("A shipment delayed before dispatch must be packed again before shipping.");
        shipment.Status.Should().Be(ShipmentStatus.Delayed);
        shipment.ShippedDate.Should().BeNull();
        shipment.Order!.Status.Should().Be(OrderStatus.Packed);
        repository.Verify(r => r.SaveChangesAsync(), Times.Once);
        VerifyAuditCount(audit, 1);
    }

    [Fact]
    public async Task PendingDelayed_CanBePackedAndThenDispatched()
    {
        var (service, repository, audit, shipment) = Scenario(ShipmentStatus.Pending);

        await ChangeAsync(service, shipment, "Delayed");
        await ChangeAsync(service, shipment, "Packed");
        var result = await ChangeAsync(service, shipment, "Shipped");

        shipment.Status.Should().Be(ShipmentStatus.Shipped);
        shipment.ShippedDate.Should().NotBeNull();
        shipment.Order!.Status.Should().Be(OrderStatus.Shipped);
        result!.OrderStatus.Should().Be("Shipped");
        repository.Verify(r => r.SaveChangesAsync(), Times.Exactly(3));
        VerifyAuditCount(audit, 4);
    }

    [Fact]
    public async Task PackedDelayed_RequiresNewPackingConfirmation()
    {
        var (service, repository, audit, shipment) = Scenario(ShipmentStatus.Packed);

        await ChangeAsync(service, shipment, "Delayed");
        Func<Task> prematureDispatch = () => ChangeAsync(service, shipment, "Shipped");

        await prematureDispatch.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("A shipment delayed before dispatch must be packed again before shipping.");
        shipment.Order!.Status.Should().Be(OrderStatus.Packed);

        await ChangeAsync(service, shipment, "Packed");
        await ChangeAsync(service, shipment, "Shipped");

        shipment.Status.Should().Be(ShipmentStatus.Shipped);
        shipment.Order.Status.Should().Be(OrderStatus.Shipped);
        repository.Verify(r => r.SaveChangesAsync(), Times.Exactly(3));
        VerifyAuditCount(audit, 4);
    }

    [Fact]
    public async Task ShippedDelayed_CanResumeTransitWithoutChangingOriginalShippingDate()
    {
        var (service, repository, audit, shipment) = Scenario(ShipmentStatus.Shipped, OrderStatus.Shipped);
        var originalShippedDate = DateTime.UtcNow.AddHours(-4);
        shipment.ShippedDate = originalShippedDate;

        await ChangeAsync(service, shipment, "Delayed");
        var result = await ChangeAsync(service, shipment, "Shipped");

        result!.Status.Should().Be("Shipped");
        result.OrderStatus.Should().Be("Shipped");
        shipment.ShippedDate.Should().Be(originalShippedDate);
        repository.Verify(r => r.SaveChangesAsync(), Times.Exactly(2));
        VerifyAuditCount(audit, 2);
    }

    [Fact]
    public async Task InTransitDelayed_CannotReturnToPacked()
    {
        var (service, repository, audit, shipment) = Scenario(ShipmentStatus.Delayed, OrderStatus.Shipped);
        shipment.ShippedDate = DateTime.UtcNow.AddHours(-2);

        Func<Task> action = () => ChangeAsync(service, shipment, "Packed");

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("A shipment delayed in transit cannot be packed again.");
        shipment.Status.Should().Be(ShipmentStatus.Delayed);
        repository.Verify(r => r.SaveChangesAsync(), Times.Never);
        VerifyAuditCount(audit, 0);
    }

    [Fact]
    public async Task PreDispatchDelayed_CanStillBeCancelled()
    {
        var (service, repository, audit, shipment) = Scenario(ShipmentStatus.Delayed);

        await ChangeAsync(service, shipment, "Cancelled");

        shipment.Status.Should().Be(ShipmentStatus.Cancelled);
        shipment.Order!.Status.Should().Be(OrderStatus.Packed);
        repository.Verify(r => r.SaveChangesAsync(), Times.Once);
        VerifyAuditCount(audit, 1);
    }

    [Fact]
    public async Task InTransitDelayed_CannotBeCancelled()
    {
        var (service, repository, audit, shipment) = Scenario(ShipmentStatus.Delayed, OrderStatus.Shipped);
        shipment.ShippedDate = DateTime.UtcNow.AddHours(-2);

        Func<Task> action = () => ChangeAsync(service, shipment, "Cancelled");

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("A shipped shipment cannot be cancelled.");
        shipment.Status.Should().Be(ShipmentStatus.Delayed);
        repository.Verify(r => r.SaveChangesAsync(), Times.Never);
        VerifyAuditCount(audit, 0);
    }

    [Fact]
    public async Task Pending_CannotBeDispatchedWithoutPacking()
    {
        var (service, repository, audit, shipment) = Scenario(ShipmentStatus.Pending);

        Func<Task> action = () => ChangeAsync(service, shipment, "Shipped");

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Shipment status cannot be changed from Pending to Shipped.");
        repository.Verify(r => r.SaveChangesAsync(), Times.Never);
        VerifyAuditCount(audit, 0);
    }

    [Fact]
    public async Task RepeatedDelayedUpdate_DoesNotChangeTimestampOrWriteAudit()
    {
        var (service, repository, audit, shipment) = Scenario(ShipmentStatus.Delayed);
        var updatedAt = DateTime.UtcNow.AddHours(-1);
        shipment.UpdatedAt = updatedAt;

        var result = await ChangeAsync(service, shipment, "Delayed");

        result!.Status.Should().Be("Delayed");
        shipment.UpdatedAt.Should().Be(updatedAt);
        repository.Verify(r => r.SaveChangesAsync(), Times.Never);
        VerifyAuditCount(audit, 0);
    }

    private static Task<ShipmentDto?> ChangeAsync(
        ShipmentService service, Shipment shipment, string status)
    {
        return service.UpdateStatusAsync(
            shipment.Id, new UpdateShipmentStatusRequest { Status = status });
    }

    private static void VerifyAuditCount(Mock<IAuditLogService> audit, int count)
    {
        audit.Verify(service => service.LogAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Exactly(count));
    }

    private static (ShipmentService Service, Mock<IShipmentRepository> Repository,
        Mock<IAuditLogService> Audit, Shipment Shipment) Scenario(
            ShipmentStatus shipmentStatus,
            OrderStatus orderStatus = OrderStatus.Packed)
    {
        var order = new Order
        {
            Id = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            Status = orderStatus,
            CreatedAt = DateTime.UtcNow
        };

        var shipment = new Shipment
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            Order = order,
            Status = shipmentStatus,
            TrackingNumber = "TEST-TRACKING",
            CreatedAt = DateTime.UtcNow
        };

        var repository = new Mock<IShipmentRepository>();
        repository.Setup(r => r.GetByIdAsync(shipment.Id)).ReturnsAsync(shipment);
        repository.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var audit = new Mock<IAuditLogService>();
        audit.Setup(a => a.LogAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        return (new ShipmentService(repository.Object, audit.Object), repository, audit, shipment);
    }
}
