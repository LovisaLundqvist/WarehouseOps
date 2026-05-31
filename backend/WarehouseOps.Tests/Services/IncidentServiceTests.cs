using FluentAssertions;
using Moq;
using WarehouseOps.Application.Dtos;
using WarehouseOps.Application.Interfaces;
using WarehouseOps.Application.Services;
using WarehouseOps.Domain;

namespace WarehouseOps.Tests.Services;

public class IncidentServiceTests
{
    private readonly Mock<IIncidentRepository> _incidentRepositoryMock;
    private readonly Mock<IAuditLogService> _auditLogServiceMock;
    private readonly IncidentService _incidentService;

    public IncidentServiceTests()
    {
        _incidentRepositoryMock = new Mock<IIncidentRepository>();
        _auditLogServiceMock = new Mock<IAuditLogService>();

        _incidentService = new IncidentService(
            _incidentRepositoryMock.Object,
            _auditLogServiceMock.Object);
    }

    [Fact]
    public async Task CreateAsync_ShouldCreateIncident_WhenRequestIsValid()
    {
        var request = new CreateIncidentRequest
        {
            Title = " Damaged package ",
            Description = " Package was damaged during handling. ",
            Severity = " High ",
            RelatedEntityType = " Order ",
            RelatedEntityId = " ORD-1001 "
        };

        _incidentRepositoryMock
            .Setup(repository => repository.AddAsync(It.IsAny<Incident>()))
            .Returns(Task.CompletedTask);

        _incidentRepositoryMock
            .Setup(repository => repository.SaveChangesAsync())
            .Returns(Task.CompletedTask);

        _auditLogServiceMock
            .Setup(service => service.LogAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var result = await _incidentService.CreateAsync(request);

        result.Title.Should().Be("Damaged package");
        result.Description.Should().Be("Package was damaged during handling.");
        result.Severity.Should().Be("High");
        result.RelatedEntityType.Should().Be("Order");
        result.RelatedEntityId.Should().Be("ORD-1001");
        result.Status.Should().Be("Open");
        result.ResolutionNotes.Should().BeEmpty();
        result.ClosedAt.Should().BeNull();

        _incidentRepositoryMock.Verify(
            repository => repository.AddAsync(It.Is<Incident>(incident =>
                incident.Title == "Damaged package" &&
                incident.Description == "Package was damaged during handling." &&
                incident.Severity == IncidentSeverity.High &&
                incident.RelatedEntityType == IncidentRelatedEntityType.Order &&
                incident.RelatedEntityId == "ORD-1001" &&
                incident.Status == IncidentStatus.Open)),
            Times.Once);

        _incidentRepositoryMock.Verify(
            repository => repository.SaveChangesAsync(),
            Times.Once);

        _auditLogServiceMock.Verify(
            service => service.LogAsync(
                "Incident",
                "Created",
                "System",
                It.Is<string>(changes =>
                    changes.Contains("Created incident") &&
                    changes.Contains("Title=Damaged package") &&
                    changes.Contains("Severity=High"))),
            Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ShouldThrowArgumentException_WhenTitleIsMissing()
    {
        var request = new CreateIncidentRequest
        {
            Title = " ",
            Description = "Package was damaged.",
            Severity = "High",
            RelatedEntityType = "Order",
            RelatedEntityId = "ORD-1001"
        };

        var action = async () => await _incidentService.CreateAsync(request);

        await action.Should()
            .ThrowAsync<ArgumentException>()
            .WithMessage("Incident title is required.");

        _incidentRepositoryMock.Verify(
            repository => repository.AddAsync(It.IsAny<Incident>()),
            Times.Never);

        _incidentRepositoryMock.Verify(
            repository => repository.SaveChangesAsync(),
            Times.Never);
    }

    [Fact]
    public async Task CreateAsync_ShouldThrowArgumentException_WhenDescriptionIsMissing()
    {
        var request = new CreateIncidentRequest
        {
            Title = "Damaged package",
            Description = " ",
            Severity = "High",
            RelatedEntityType = "Order",
            RelatedEntityId = "ORD-1001"
        };

        var action = async () => await _incidentService.CreateAsync(request);

        await action.Should()
            .ThrowAsync<ArgumentException>()
            .WithMessage("Incident description is required.");

        _incidentRepositoryMock.Verify(
            repository => repository.AddAsync(It.IsAny<Incident>()),
            Times.Never);

        _incidentRepositoryMock.Verify(
            repository => repository.SaveChangesAsync(),
            Times.Never);
    }

    [Fact]
    public async Task CreateAsync_ShouldThrowArgumentException_WhenSeverityIsInvalid()
    {
        var request = new CreateIncidentRequest
        {
            Title = "Damaged package",
            Description = "Package was damaged.",
            Severity = "VeryBad",
            RelatedEntityType = "Order",
            RelatedEntityId = "ORD-1001"
        };

        var action = async () => await _incidentService.CreateAsync(request);

        await action.Should()
            .ThrowAsync<ArgumentException>()
            .WithMessage("Incident severity is invalid. Valid severities are Low, Medium, High and Critical.");

        _incidentRepositoryMock.Verify(
            repository => repository.AddAsync(It.IsAny<Incident>()),
            Times.Never);

        _incidentRepositoryMock.Verify(
            repository => repository.SaveChangesAsync(),
            Times.Never);
    }

    [Fact]
    public async Task CreateAsync_ShouldThrowArgumentException_WhenRelatedEntityTypeIsInvalid()
    {
        var request = new CreateIncidentRequest
        {
            Title = "Damaged package",
            Description = "Package was damaged.",
            Severity = "High",
            RelatedEntityType = "UnknownArea",
            RelatedEntityId = "ORD-1001"
        };

        var action = async () => await _incidentService.CreateAsync(request);

        await action.Should()
            .ThrowAsync<ArgumentException>()
            .WithMessage("Related entity type is invalid. Valid types are General, Product, Inventory, Customer, Order and Shipment.");

        _incidentRepositoryMock.Verify(
            repository => repository.AddAsync(It.IsAny<Incident>()),
            Times.Never);

        _incidentRepositoryMock.Verify(
            repository => repository.SaveChangesAsync(),
            Times.Never);
    }

    [Fact]
    public async Task UpdateStatusAsync_ShouldUpdateIncidentStatus_WhenStatusChangeIsValid()
    {
        var incidentId = Guid.NewGuid();

        var incident = CreateIncident(
            incidentId,
            IncidentStatus.Open);

        var request = new UpdateIncidentStatusRequest
        {
            Status = "InProgress"
        };

        _incidentRepositoryMock
            .Setup(repository => repository.GetByIdAsync(incidentId))
            .ReturnsAsync(incident);

        _incidentRepositoryMock
            .Setup(repository => repository.SaveChangesAsync())
            .Returns(Task.CompletedTask);

        _auditLogServiceMock
            .Setup(service => service.LogAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var result = await _incidentService.UpdateStatusAsync(incidentId, request);

        result.Should().NotBeNull();
        result!.Status.Should().Be("InProgress");

        incident.Status.Should().Be(IncidentStatus.InProgress);
        incident.UpdatedAt.Should().NotBeNull();
        incident.ClosedAt.Should().BeNull();

        _incidentRepositoryMock.Verify(
            repository => repository.SaveChangesAsync(),
            Times.Once);

        _auditLogServiceMock.Verify(
            service => service.LogAsync(
                "Incident",
                "Updated",
                "System",
                It.Is<string>(changes =>
                    changes.Contains("Updated incident") &&
                    changes.Contains("status from Open to InProgress"))),
            Times.Once);
    }

    [Fact]
    public async Task UpdateStatusAsync_ShouldSetClosedAt_WhenStatusBecomesClosed()
    {
        var incidentId = Guid.NewGuid();

        var incident = CreateIncident(
            incidentId,
            IncidentStatus.Resolved);

        var request = new UpdateIncidentStatusRequest
        {
            Status = "Closed"
        };

        _incidentRepositoryMock
            .Setup(repository => repository.GetByIdAsync(incidentId))
            .ReturnsAsync(incident);

        _incidentRepositoryMock
            .Setup(repository => repository.SaveChangesAsync())
            .Returns(Task.CompletedTask);

        _auditLogServiceMock
            .Setup(service => service.LogAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var result = await _incidentService.UpdateStatusAsync(incidentId, request);

        result.Should().NotBeNull();
        result!.Status.Should().Be("Closed");
        result.ClosedAt.Should().NotBeNull();

        incident.Status.Should().Be(IncidentStatus.Closed);
        incident.ClosedAt.Should().NotBeNull();
        incident.UpdatedAt.Should().NotBeNull();

        _auditLogServiceMock.Verify(
            service => service.LogAsync(
                "Incident",
                "Closed",
                "System",
                It.Is<string>(changes =>
                    changes.Contains("Updated incident") &&
                    changes.Contains("status from Resolved to Closed"))),
            Times.Once);
    }

    [Fact]
    public async Task UpdateStatusAsync_ShouldReturnNull_WhenIncidentDoesNotExist()
    {
        var incidentId = Guid.NewGuid();

        var request = new UpdateIncidentStatusRequest
        {
            Status = "InProgress"
        };

        _incidentRepositoryMock
            .Setup(repository => repository.GetByIdAsync(incidentId))
            .ReturnsAsync((Incident?)null);

        var result = await _incidentService.UpdateStatusAsync(incidentId, request);

        result.Should().BeNull();

        _incidentRepositoryMock.Verify(
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
        var request = new UpdateIncidentStatusRequest
        {
            Status = "Unknown"
        };

        var action = async () => await _incidentService.UpdateStatusAsync(Guid.NewGuid(), request);

        await action.Should()
            .ThrowAsync<ArgumentException>()
            .WithMessage("Incident status is invalid. Valid statuses are Open, InProgress, Resolved and Closed.");

        _incidentRepositoryMock.Verify(
            repository => repository.GetByIdAsync(It.IsAny<Guid>()),
            Times.Never);

        _incidentRepositoryMock.Verify(
            repository => repository.SaveChangesAsync(),
            Times.Never);
    }

    [Fact]
    public async Task UpdateStatusAsync_ShouldThrowInvalidOperationException_WhenIncidentIsClosed()
    {
        var incidentId = Guid.NewGuid();

        var incident = CreateIncident(
            incidentId,
            IncidentStatus.Closed);

        var request = new UpdateIncidentStatusRequest
        {
            Status = "Open"
        };

        _incidentRepositoryMock
            .Setup(repository => repository.GetByIdAsync(incidentId))
            .ReturnsAsync(incident);

        var action = async () => await _incidentService.UpdateStatusAsync(incidentId, request);

        await action.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("Incident status cannot be changed from Closed to Open.");

        incident.Status.Should().Be(IncidentStatus.Closed);

        _incidentRepositoryMock.Verify(
            repository => repository.SaveChangesAsync(),
            Times.Never);
    }

    [Fact]
    public async Task ResolveAsync_ShouldCloseIncident_WhenResolutionNotesAreValid()
    {
        var incidentId = Guid.NewGuid();

        var incident = CreateIncident(
            incidentId,
            IncidentStatus.InProgress);

        var request = new ResolveIncidentRequest
        {
            ResolutionNotes = " Package was replaced and customer was informed. "
        };

        _incidentRepositoryMock
            .Setup(repository => repository.GetByIdAsync(incidentId))
            .ReturnsAsync(incident);

        _incidentRepositoryMock
            .Setup(repository => repository.SaveChangesAsync())
            .Returns(Task.CompletedTask);

        _auditLogServiceMock
            .Setup(service => service.LogAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var result = await _incidentService.ResolveAsync(incidentId, request);

        result.Should().NotBeNull();
        result!.Status.Should().Be("Closed");
        result.ResolutionNotes.Should().Be("Package was replaced and customer was informed.");
        result.ClosedAt.Should().NotBeNull();

        incident.Status.Should().Be(IncidentStatus.Closed);
        incident.ResolutionNotes.Should().Be("Package was replaced and customer was informed.");
        incident.ClosedAt.Should().NotBeNull();
        incident.UpdatedAt.Should().NotBeNull();

        _incidentRepositoryMock.Verify(
            repository => repository.SaveChangesAsync(),
            Times.Once);

        _auditLogServiceMock.Verify(
            service => service.LogAsync(
                "Incident",
                "Closed",
                "System",
                It.Is<string>(changes =>
                    changes.Contains("Closed incident") &&
                    changes.Contains("Old status=InProgress") &&
                    changes.Contains("Resolution notes=Package was replaced and customer was informed."))),
            Times.Once);
    }

    [Fact]
    public async Task ResolveAsync_ShouldThrowArgumentException_WhenResolutionNotesAreMissing()
    {
        var request = new ResolveIncidentRequest
        {
            ResolutionNotes = " "
        };

        var action = async () => await _incidentService.ResolveAsync(Guid.NewGuid(), request);

        await action.Should()
            .ThrowAsync<ArgumentException>()
            .WithMessage("Resolution notes are required.");

        _incidentRepositoryMock.Verify(
            repository => repository.GetByIdAsync(It.IsAny<Guid>()),
            Times.Never);

        _incidentRepositoryMock.Verify(
            repository => repository.SaveChangesAsync(),
            Times.Never);
    }

    [Fact]
    public async Task ResolveAsync_ShouldReturnNull_WhenIncidentDoesNotExist()
    {
        var incidentId = Guid.NewGuid();

        var request = new ResolveIncidentRequest
        {
            ResolutionNotes = "Package was replaced."
        };

        _incidentRepositoryMock
            .Setup(repository => repository.GetByIdAsync(incidentId))
            .ReturnsAsync((Incident?)null);

        var result = await _incidentService.ResolveAsync(incidentId, request);

        result.Should().BeNull();

        _incidentRepositoryMock.Verify(
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
    public async Task ResolveAsync_ShouldThrowInvalidOperationException_WhenIncidentIsAlreadyClosed()
    {
        var incidentId = Guid.NewGuid();

        var incident = CreateIncident(
            incidentId,
            IncidentStatus.Closed);

        var request = new ResolveIncidentRequest
        {
            ResolutionNotes = "Package was replaced."
        };

        _incidentRepositoryMock
            .Setup(repository => repository.GetByIdAsync(incidentId))
            .ReturnsAsync(incident);

        var action = async () => await _incidentService.ResolveAsync(incidentId, request);

        await action.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("Closed incidents cannot be resolved again.");

        _incidentRepositoryMock.Verify(
            repository => repository.SaveChangesAsync(),
            Times.Never);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnFilteredIncidents_WhenStatusIsValid()
    {
        var incident = CreateIncident(
            Guid.NewGuid(),
            IncidentStatus.Open);

        _incidentRepositoryMock
            .Setup(repository => repository.GetAllAsync(IncidentStatus.Open))
            .ReturnsAsync(new List<Incident> { incident });

        var result = await _incidentService.GetAllAsync("Open");

        result.Should().HaveCount(1);
        result[0].Status.Should().Be("Open");
        result[0].Title.Should().Be("Damaged package");
    }

    private static Incident CreateIncident(Guid incidentId, IncidentStatus status)
    {
        return new Incident
        {
            Id = incidentId,
            Title = "Damaged package",
            Description = "Package was damaged during handling.",
            Severity = IncidentSeverity.High,
            RelatedEntityType = IncidentRelatedEntityType.Order,
            RelatedEntityId = "ORD-1001",
            Status = status,
            ResolutionNotes = "",
            ClosedAt = status == IncidentStatus.Closed ? DateTime.UtcNow : null,
            CreatedAt = DateTime.UtcNow
        };
    }
}
