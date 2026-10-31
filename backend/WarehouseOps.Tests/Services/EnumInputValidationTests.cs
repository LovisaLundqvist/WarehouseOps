using FluentAssertions;
using Moq;
using WarehouseOps.Application.Dtos;
using WarehouseOps.Application.Interfaces;
using WarehouseOps.Application.Services;
using WarehouseOps.Application.Validation;
using WarehouseOps.Domain;

namespace WarehouseOps.Tests.Services;

public class EnumInputValidationTests
{
    [Theory]
    [InlineData("0")]
    [InlineData("1")]
    [InlineData("999")]
    public async Task OrderStatus_RejectsNumericValues(string status)
    {
        var repository = new Mock<IOrderRepository>();
        var auditLog = new Mock<IAuditLogService>();
        var service = new OrderService(repository.Object, auditLog.Object);

        Func<Task> action = async () => await service.UpdateStatusAsync(
            Guid.NewGuid(), new UpdateOrderStatusRequest { Status = status });

        await action.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Order status is invalid.*");

        repository.Verify(x => x.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        repository.Verify(x => x.SaveChangesAsync(), Times.Never);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("1")]
    [InlineData("999")]
    public async Task ShipmentStatus_RejectsNumericValues(string status)
    {
        var repository = new Mock<IShipmentRepository>();
        var auditLog = new Mock<IAuditLogService>();
        var service = new ShipmentService(repository.Object, auditLog.Object);

        Func<Task> action = async () => await service.UpdateStatusAsync(
            Guid.NewGuid(), new UpdateShipmentStatusRequest { Status = status });

        await action.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Shipment status is invalid.*");

        repository.Verify(x => x.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        repository.Verify(x => x.SaveChangesAsync(), Times.Never);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("1")]
    [InlineData("999")]
    public async Task IncidentStatus_RejectsNumericValues(string status)
    {
        var repository = new Mock<IIncidentRepository>();
        var auditLog = new Mock<IAuditLogService>();
        var service = new IncidentService(repository.Object, auditLog.Object);

        Func<Task> action = async () => await service.UpdateStatusAsync(
            Guid.NewGuid(), new UpdateIncidentStatusRequest { Status = status });

        await action.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Incident status is invalid.*");

        repository.Verify(x => x.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        repository.Verify(x => x.SaveChangesAsync(), Times.Never);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("1")]
    [InlineData("999")]
    public async Task IncidentSeverity_RejectsNumericValues(string severity)
    {
        var repository = new Mock<IIncidentRepository>();
        var auditLog = new Mock<IAuditLogService>();
        var service = new IncidentService(repository.Object, auditLog.Object);

        var request = new CreateIncidentRequest
        {
            Title = "Damaged parcel",
            Description = "Outer packaging damaged",
            Severity = severity,
            RelatedEntityType = "General"
        };

        Func<Task> action = async () => await service.CreateAsync(request);

        await action.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Incident severity is invalid.*");

        repository.Verify(x => x.AddAsync(It.IsAny<Incident>()), Times.Never);
        repository.Verify(x => x.SaveChangesAsync(), Times.Never);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("1")]
    [InlineData("999")]
    public async Task IncidentRelatedEntityType_RejectsNumericValues(string relatedEntityType)
    {
        var repository = new Mock<IIncidentRepository>();
        var auditLog = new Mock<IAuditLogService>();
        var service = new IncidentService(repository.Object, auditLog.Object);

        var request = new CreateIncidentRequest
        {
            Title = "Damaged parcel",
            Description = "Outer packaging damaged",
            Severity = "Medium",
            RelatedEntityType = relatedEntityType
        };

        Func<Task> action = async () => await service.CreateAsync(request);

        await action.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Related entity type is invalid.*");

        repository.Verify(x => x.AddAsync(It.IsAny<Incident>()), Times.Never);
        repository.Verify(x => x.SaveChangesAsync(), Times.Never);
    }

    [Theory]
    [InlineData(" Low ", "Low")]
    [InlineData("high", "High")]
    [InlineData("CRITICAL", "Critical")]
    public void EnumNameParser_AcceptsNamesIgnoringCaseAndWhitespace(string input, string expected)
    {
        var isValid = EnumNameParser.TryParse<IncidentSeverity>(input, out var severity);

        isValid.Should().BeTrue();
        severity.ToString().Should().Be(expected);
    }

    [Fact]
    public void EnumNameParser_RejectsEmptyValue()
    {
        EnumNameParser.TryParse<OrderStatus>("  ", out _).Should().BeFalse();
    }
}
