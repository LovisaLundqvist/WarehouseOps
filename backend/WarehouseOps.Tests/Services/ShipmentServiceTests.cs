using FluentAssertions;
using Moq;
using WarehouseOps.Application.Dtos;
using WarehouseOps.Application.Interfaces;
using WarehouseOps.Application.Services;
using WarehouseOps.Domain;

namespace WarehouseOps.Tests.Services;

public class ShipmentServiceTests
{
    private readonly Mock<IShipmentRepository> _shipmentRepositoryMock;
    private readonly Mock<IAuditLogService> _auditLogServiceMock;
    private readonly ShipmentService _shipmentService;

    public ShipmentServiceTests()
    {
        _shipmentRepositoryMock = new Mock<IShipmentRepository>();
        _auditLogServiceMock = new Mock<IAuditLogService>();

        _shipmentService = new ShipmentService(
            _shipmentRepositoryMock.Object,
            _auditLogServiceMock.Object);
    }

    [Fact]
    public async Task CreateAsync_ShouldCreateShipment_WhenRequestIsValid()
    {
        var orderId = Guid.NewGuid();
        var shipmentId = Guid.NewGuid();

        var order = CreateOrder(orderId, OrderStatus.Packed);
        Shipment? createdShipment = null;

        var request = new CreateShipmentRequest
        {
            OrderId = orderId,
            TrackingNumber = " WH-TRK-1001 "
        };

        _shipmentRepositoryMock
            .Setup(repository => repository.GetOrderByIdAsync(orderId))
            .ReturnsAsync(order);

        _shipmentRepositoryMock
            .Setup(repository => repository.ShipmentExistsForOrderAsync(orderId))
            .ReturnsAsync(false);

        _shipmentRepositoryMock
            .Setup(repository => repository.AddAsync(It.IsAny<Shipment>()))
            .Callback<Shipment>(shipment =>
            {
                shipment.Id = shipmentId;
                shipment.Order = order;
                createdShipment = shipment;
            })
            .Returns(Task.CompletedTask);

        _shipmentRepositoryMock
            .Setup(repository => repository.SaveChangesAsync())
            .Returns(Task.CompletedTask);

        _shipmentRepositoryMock
            .Setup(repository => repository.GetByIdAsync(shipmentId))
            .ReturnsAsync(() => createdShipment);

        _auditLogServiceMock
            .Setup(service => service.LogAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var result = await _shipmentService.CreateAsync(request);

        result.Id.Should().Be(shipmentId);
        result.OrderId.Should().Be(orderId);
        result.OrderStatus.Should().Be("Packed");
        result.CustomerName.Should().Be("Nordic Retail AB");
        result.Status.Should().Be("Pending");
        result.TrackingNumber.Should().Be("WH-TRK-1001");

        _shipmentRepositoryMock.Verify(
            repository => repository.AddAsync(It.Is<Shipment>(shipment =>
                shipment.OrderId == orderId &&
                shipment.Status == ShipmentStatus.Pending &&
                shipment.TrackingNumber == "WH-TRK-1001")),
            Times.Once);

        _shipmentRepositoryMock.Verify(
            repository => repository.SaveChangesAsync(),
            Times.Once);

        _auditLogServiceMock.Verify(
            service => service.LogAsync(
                "Shipment",
                "Created",
                "System",
                It.Is<string>(changes =>
                    changes.Contains("Created shipment") &&
                    changes.Contains("WH-TRK-1001"))),
            Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ShouldThrowArgumentException_WhenOrderIdIsMissing()
    {
        var request = new CreateShipmentRequest
        {
            OrderId = Guid.Empty,
            TrackingNumber = "WH-TRK-1001"
        };

        var action = async () => await _shipmentService.CreateAsync(request);

        await action.Should()
            .ThrowAsync<ArgumentException>()
            .WithMessage("Order id is required.");

        _shipmentRepositoryMock.Verify(
            repository => repository.AddAsync(It.IsAny<Shipment>()),
            Times.Never);

        _shipmentRepositoryMock.Verify(
            repository => repository.SaveChangesAsync(),
            Times.Never);
    }

    [Fact]
    public async Task CreateAsync_ShouldThrowArgumentException_WhenTrackingNumberIsMissing()
    {
        var request = new CreateShipmentRequest
        {
            OrderId = Guid.NewGuid(),
            TrackingNumber = " "
        };

        var action = async () => await _shipmentService.CreateAsync(request);

        await action.Should()
            .ThrowAsync<ArgumentException>()
            .WithMessage("Tracking number is required.");

        _shipmentRepositoryMock.Verify(
            repository => repository.AddAsync(It.IsAny<Shipment>()),
            Times.Never);

        _shipmentRepositoryMock.Verify(
            repository => repository.SaveChangesAsync(),
            Times.Never);
    }

    [Fact]
    public async Task CreateAsync_ShouldThrowInvalidOperationException_WhenOrderDoesNotExist()
    {
        var orderId = Guid.NewGuid();

        var request = new CreateShipmentRequest
        {
            OrderId = orderId,
            TrackingNumber = "WH-TRK-1001"
        };

        _shipmentRepositoryMock
            .Setup(repository => repository.GetOrderByIdAsync(orderId))
            .ReturnsAsync((Order?)null);

        var action = async () => await _shipmentService.CreateAsync(request);

        await action.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("Order does not exist.");

        _shipmentRepositoryMock.Verify(
            repository => repository.AddAsync(It.IsAny<Shipment>()),
            Times.Never);

        _shipmentRepositoryMock.Verify(
            repository => repository.SaveChangesAsync(),
            Times.Never);
    }

    [Fact]
    public async Task CreateAsync_ShouldThrowInvalidOperationException_WhenOrderIsCancelled()
    {
        var orderId = Guid.NewGuid();
        var order = CreateOrder(orderId, OrderStatus.Cancelled);

        var request = new CreateShipmentRequest
        {
            OrderId = orderId,
            TrackingNumber = "WH-TRK-1001"
        };

        _shipmentRepositoryMock
            .Setup(repository => repository.GetOrderByIdAsync(orderId))
            .ReturnsAsync(order);

        var action = async () => await _shipmentService.CreateAsync(request);

        await action.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("A shipment cannot be created for a cancelled order.");

        _shipmentRepositoryMock.Verify(
            repository => repository.AddAsync(It.IsAny<Shipment>()),
            Times.Never);

        _shipmentRepositoryMock.Verify(
            repository => repository.SaveChangesAsync(),
            Times.Never);
    }

    [Fact]
    public async Task CreateAsync_ShouldThrowInvalidOperationException_WhenOrderAlreadyHasShipment()
    {
        var orderId = Guid.NewGuid();
        var order = CreateOrder(orderId, OrderStatus.Packed);

        var request = new CreateShipmentRequest
        {
            OrderId = orderId,
            TrackingNumber = "WH-TRK-1001"
        };

        _shipmentRepositoryMock
            .Setup(repository => repository.GetOrderByIdAsync(orderId))
            .ReturnsAsync(order);

        _shipmentRepositoryMock
            .Setup(repository => repository.ShipmentExistsForOrderAsync(orderId))
            .ReturnsAsync(true);

        var action = async () => await _shipmentService.CreateAsync(request);

        await action.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("This order already has a shipment.");

        _shipmentRepositoryMock.Verify(
            repository => repository.AddAsync(It.IsAny<Shipment>()),
            Times.Never);

        _shipmentRepositoryMock.Verify(
            repository => repository.SaveChangesAsync(),
            Times.Never);
    }

    [Fact]
    public async Task UpdateStatusAsync_ShouldUpdateShipmentStatus_WhenStatusChangeIsValid()
    {
        var shipmentId = Guid.NewGuid();
        var orderId = Guid.NewGuid();

        var shipment = CreateShipment(
            shipmentId,
            orderId,
            ShipmentStatus.Pending);

        var request = new UpdateShipmentStatusRequest
        {
            Status = "Packed"
        };

        _shipmentRepositoryMock
            .Setup(repository => repository.GetByIdAsync(shipmentId))
            .ReturnsAsync(shipment);

        _shipmentRepositoryMock
            .Setup(repository => repository.SaveChangesAsync())
            .Returns(Task.CompletedTask);

        _auditLogServiceMock
            .Setup(service => service.LogAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var result = await _shipmentService.UpdateStatusAsync(shipmentId, request);

        result.Should().NotBeNull();
        result!.Status.Should().Be("Packed");

        shipment.Status.Should().Be(ShipmentStatus.Packed);
        shipment.UpdatedAt.Should().NotBeNull();

        _shipmentRepositoryMock.Verify(
            repository => repository.SaveChangesAsync(),
            Times.Once);

        _auditLogServiceMock.Verify(
            service => service.LogAsync(
                "Shipment",
                "Updated",
                "System",
                It.Is<string>(changes =>
                    changes.Contains("Updated shipment") &&
                    changes.Contains("status from Pending to Packed"))),
            Times.Once);
    }

    [Fact]
    public async Task UpdateStatusAsync_ShouldSetDeliveredDate_WhenStatusBecomesDelivered()
    {
        var shipmentId = Guid.NewGuid();
        var orderId = Guid.NewGuid();

        var shipment = CreateShipment(
            shipmentId,
            orderId,
            ShipmentStatus.Shipped);

        var request = new UpdateShipmentStatusRequest
        {
            Status = "Delivered"
        };

        _shipmentRepositoryMock
            .Setup(repository => repository.GetByIdAsync(shipmentId))
            .ReturnsAsync(shipment);

        _shipmentRepositoryMock
            .Setup(repository => repository.SaveChangesAsync())
            .Returns(Task.CompletedTask);

        _auditLogServiceMock
            .Setup(service => service.LogAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var result = await _shipmentService.UpdateStatusAsync(shipmentId, request);

        result.Should().NotBeNull();
        result!.Status.Should().Be("Delivered");
        result.DeliveredDate.Should().NotBeNull();

        shipment.Status.Should().Be(ShipmentStatus.Delivered);
        shipment.DeliveredDate.Should().NotBeNull();
        shipment.UpdatedAt.Should().NotBeNull();

        _shipmentRepositoryMock.Verify(
            repository => repository.SaveChangesAsync(),
            Times.Once);
    }

    [Fact]
    public async Task UpdateStatusAsync_ShouldReturnNull_WhenShipmentDoesNotExist()
    {
        var shipmentId = Guid.NewGuid();

        var request = new UpdateShipmentStatusRequest
        {
            Status = "Packed"
        };

        _shipmentRepositoryMock
            .Setup(repository => repository.GetByIdAsync(shipmentId))
            .ReturnsAsync((Shipment?)null);

        var result = await _shipmentService.UpdateStatusAsync(shipmentId, request);

        result.Should().BeNull();

        _shipmentRepositoryMock.Verify(
            repository => repository.SaveChangesAsync(),
            Times.Never);

        _auditLogServiceMock.Verify(
            service => service.LogAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateStatusAsync_ShouldThrowArgumentException_WhenStatusIsInvalid()
    {
        var request = new UpdateShipmentStatusRequest
        {
            Status = "Unknown"
        };

        var action = async () => await _shipmentService.UpdateStatusAsync(Guid.NewGuid(), request);

        await action.Should()
            .ThrowAsync<ArgumentException>()
            .WithMessage("Shipment status is invalid. Valid statuses are Pending, Packed, Shipped, Delivered, Delayed and Cancelled.");

        _shipmentRepositoryMock.Verify(
            repository => repository.GetByIdAsync(It.IsAny<Guid>()),
            Times.Never);

        _shipmentRepositoryMock.Verify(
            repository => repository.SaveChangesAsync(),
            Times.Never);
    }

    [Fact]
    public async Task UpdateStatusAsync_ShouldThrowInvalidOperationException_WhenShipmentIsDelivered()
    {
        var shipmentId = Guid.NewGuid();
        var orderId = Guid.NewGuid();

        var shipment = CreateShipment(
            shipmentId,
            orderId,
            ShipmentStatus.Delivered);

        var request = new UpdateShipmentStatusRequest
        {
            Status = "Shipped"
        };

        _shipmentRepositoryMock
            .Setup(repository => repository.GetByIdAsync(shipmentId))
            .ReturnsAsync(shipment);

        var action = async () => await _shipmentService.UpdateStatusAsync(shipmentId, request);

        await action.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("Shipment status cannot be changed from Delivered to Shipped.");

        shipment.Status.Should().Be(ShipmentStatus.Delivered);

        _shipmentRepositoryMock.Verify(
            repository => repository.SaveChangesAsync(),
            Times.Never);
    }

    private static Order CreateOrder(Guid orderId, OrderStatus status)
    {
        var customerId = Guid.NewGuid();

        return new Order
        {
            Id = orderId,
            CustomerId = customerId,
            Customer = new Customer
            {
                Id = customerId,
                Name = "Nordic Retail AB",
                Email = "logistics@nordic-retail.se",
                PhoneNumber = "+46 31 123 456",
                Address = "Lagergatan 12, Göteborg",
                CreatedAt = DateTime.UtcNow
            },
            Status = status,
            TotalAmount = 4197,
            CreatedAt = DateTime.UtcNow
        };
    }

    private static Shipment CreateShipment(
        Guid shipmentId,
        Guid orderId,
        ShipmentStatus status)
    {
        var order = CreateOrder(orderId, OrderStatus.Packed);

        return new Shipment
        {
            Id = shipmentId,
            OrderId = orderId,
            Order = order,
            Status = status,
            TrackingNumber = "WH-TRK-1001",
            CreatedAt = DateTime.UtcNow
        };
    }
}
