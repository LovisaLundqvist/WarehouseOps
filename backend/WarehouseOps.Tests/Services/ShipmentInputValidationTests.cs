using FluentAssertions;
using Moq;
using WarehouseOps.Application.Dtos;
using WarehouseOps.Application.Interfaces;
using WarehouseOps.Application.Services;
using WarehouseOps.Domain;

namespace WarehouseOps.Tests.Services;

public class ShipmentInputValidationTests
{
    private readonly Mock<IShipmentRepository> _shipmentRepositoryMock = new();
    private readonly Mock<IAuditLogService> _auditLogServiceMock = new();

    private ShipmentService CreateService() => new(
        _shipmentRepositoryMock.Object,
        _auditLogServiceMock.Object);

    [Fact]
    public async Task CreateAsync_ShouldRejectNullRequest()
    {
        var service = CreateService();
        Func<Task> action = async () => await service.CreateAsync(null!);

        await action.Should().ThrowAsync<ArgumentNullException>();
        VerifyNoWritesOrOrderLookups();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" \t ")]
    public async Task CreateAsync_ShouldRejectMissingTrackingNumber(string? trackingNumber)
    {
        var service = CreateService();
        var request = new CreateShipmentRequest
        {
            OrderId = Guid.NewGuid(),
            TrackingNumber = trackingNumber!
        };

        Func<Task> action = async () => await service.CreateAsync(request);

        await action.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Tracking number is required.");
        VerifyNoWritesOrOrderLookups();
    }

    [Fact]
    public async Task CreateAsync_ShouldRejectTrackingNumberLongerThan100Characters()
    {
        var service = CreateService();
        var request = new CreateShipmentRequest
        {
            OrderId = Guid.NewGuid(),
            TrackingNumber = new string('A', 101)
        };

        Func<Task> action = async () => await service.CreateAsync(request);

        await action.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Tracking number cannot be longer than 100 characters.");
        VerifyNoWritesOrOrderLookups();
    }

    [Theory]
    [InlineData("AB\n123")]
    [InlineData("AB\r123")]
    [InlineData("AB\t123")]
    [InlineData("AB\0CD")]
    [InlineData("AB\u007fCD")]
    public async Task CreateAsync_ShouldRejectControlCharactersInTrackingNumber(string trackingNumber)
    {
        var service = CreateService();
        var request = new CreateShipmentRequest
        {
            OrderId = Guid.NewGuid(),
            TrackingNumber = trackingNumber
        };

        Func<Task> action = async () => await service.CreateAsync(request);

        await action.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Tracking number cannot contain control characters.");
        VerifyNoWritesOrOrderLookups();
    }

    [Fact]
    public async Task CreateAsync_ShouldAcceptExactly100CharacterTrackingNumber()
    {
        var service = CreateService();
        var orderId = Guid.NewGuid();
        var request = new CreateShipmentRequest
        {
            OrderId = orderId,
            TrackingNumber = new string('A', 100)
        };

        _shipmentRepositoryMock
            .Setup(repository => repository.GetOrderByIdAsync(orderId))
            .ReturnsAsync((Order?)null);

        Func<Task> action = async () => await service.CreateAsync(request);

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Order does not exist.");

        _shipmentRepositoryMock.Verify(repository => repository.GetOrderByIdAsync(orderId), Times.Once);
        VerifyNoWrites();
    }

    [Fact]
    public async Task UpdateStatusAsync_ShouldRejectNullRequest()
    {
        var service = CreateService();
        Func<Task> action = async () => await service.UpdateStatusAsync(Guid.NewGuid(), null!);

        await action.Should().ThrowAsync<ArgumentNullException>();
        _shipmentRepositoryMock.Verify(
            repository => repository.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        VerifyNoWrites();
    }

    private void VerifyNoWritesOrOrderLookups()
    {
        _shipmentRepositoryMock.Verify(
            repository => repository.GetOrderByIdAsync(It.IsAny<Guid>()), Times.Never);
        VerifyNoWrites();
    }

    private void VerifyNoWrites()
    {
        _shipmentRepositoryMock.Verify(
            repository => repository.AddAsync(It.IsAny<Shipment>()), Times.Never);
        _shipmentRepositoryMock.Verify(
            repository => repository.SaveChangesAsync(), Times.Never);
        _auditLogServiceMock.Verify(
            service => service.LogAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()),
            Times.Never);
    }
}
