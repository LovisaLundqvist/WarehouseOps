using FluentAssertions;
using Moq;
using WarehouseOps.Application.Dtos;
using WarehouseOps.Application.Interfaces;
using WarehouseOps.Application.Services;
using WarehouseOps.Domain;

namespace WarehouseOps.Tests.Services;

public class IncidentServiceTests
{
    private readonly Mock<IIncidentRepository> _incidentRepositoryMock = new();
    private readonly Mock<IAuditLogService> _auditLogServiceMock = new();
    private readonly IncidentService _incidentService;

    public IncidentServiceTests()
    {
        _incidentService = new IncidentService(
            _incidentRepositoryMock.Object,
            _auditLogServiceMock.Object);
    }

    [Fact]
    public async Task CreateAsync_ShouldCreateIncident_WhenRequestIsValid()
    {
        SetupCreate();

        var result = await _incidentService.CreateAsync(ValidRequest());

        result.Title.Should().Be("Damaged package");
        result.Description.Should().Be("Package was damaged during handling.");
        result.Severity.Should().Be("High");
        result.RelatedEntityType.Should().Be("Order");
        result.RelatedEntityId.Should().Be("ORD-1001");
        result.Status.Should().Be("Open");
        result.ResolutionNotes.Should().BeEmpty();
        result.ClosedAt.Should().BeNull();

        _incidentRepositoryMock.Verify(repository => repository.AddAsync(
            It.Is<Incident>(incident =>
                incident.Title == "Damaged package" &&
                incident.Description == "Package was damaged during handling." &&
                incident.Severity == IncidentSeverity.High &&
                incident.RelatedEntityType == IncidentRelatedEntityType.Order &&
                incident.RelatedEntityId == "ORD-1001" &&
                incident.Status == IncidentStatus.Open)), Times.Once);
        _incidentRepositoryMock.Verify(repository => repository.SaveChangesAsync(), Times.Once);
        _auditLogServiceMock.Verify(service => service.LogAsync(
            "Incident", "Created", "System",
            It.Is<string>(changes => changes.Contains("Created incident") &&
                changes.Contains("Severity=High") &&
                changes.Contains("RelatedEntityType=Order") &&
                !changes.Contains("Title="))), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ShouldThrowArgumentException_WhenTitleIsMissing()
    {
        var request = ValidRequest();
        request.Title = " ";

        Func<Task> action = () => _incidentService.CreateAsync(request);

        await action.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Incident title is required.");
        VerifyNoWrite();
    }

    [Fact]
    public async Task CreateAsync_ShouldThrowArgumentException_WhenDescriptionIsMissing()
    {
        var request = ValidRequest();
        request.Description = " ";

        Func<Task> action = () => _incidentService.CreateAsync(request);

        await action.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Incident description is required.");
        VerifyNoWrite();
    }

    [Fact]
    public async Task CreateAsync_ShouldThrowArgumentException_WhenSeverityIsInvalid()
    {
        var request = ValidRequest();
        request.Severity = "VeryBad";

        Func<Task> action = () => _incidentService.CreateAsync(request);

        await action.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Incident severity is invalid. Valid severities are Low, Medium, High and Critical.");
        VerifyNoWrite();
    }

    [Fact]
    public async Task CreateAsync_ShouldThrowArgumentException_WhenRelatedEntityTypeIsInvalid()
    {
        var request = ValidRequest();
        request.RelatedEntityType = "UnknownArea";

        Func<Task> action = () => _incidentService.CreateAsync(request);

        await action.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Related entity type is invalid. Valid types are General, Product, Inventory, Customer, Order and Shipment.");
        VerifyNoWrite();
    }

    [Fact]
    public async Task UpdateStatusAsync_ShouldUpdateIncidentStatus_WhenStatusChangeIsValid()
    {
        var incident = CreateIncident(IncidentStatus.Open);
        SetupExisting(incident);

        var result = await _incidentService.UpdateStatusAsync(incident.Id,
            new UpdateIncidentStatusRequest { Status = "InProgress" });

        result.Should().NotBeNull();
        result!.Status.Should().Be("InProgress");
        incident.Status.Should().Be(IncidentStatus.InProgress);
        incident.UpdatedAt.Should().NotBeNull();
        incident.ClosedAt.Should().BeNull();
        _incidentRepositoryMock.Verify(repository => repository.SaveChangesAsync(), Times.Once);
        _auditLogServiceMock.Verify(service => service.LogAsync(
            "Incident", "Updated", "System",
            It.Is<string>(changes => changes.Contains("status from Open to InProgress"))), Times.Once);
    }

    [Fact]
    public async Task UpdateStatusAsync_ShouldSetClosedAt_WhenStatusBecomesClosed()
    {
        var incident = CreateIncident(IncidentStatus.Resolved);
        SetupExisting(incident);

        var result = await _incidentService.UpdateStatusAsync(incident.Id,
            new UpdateIncidentStatusRequest { Status = "Closed" });

        result.Should().NotBeNull();
        result!.Status.Should().Be("Closed");
        result.ClosedAt.Should().NotBeNull();
        incident.Status.Should().Be(IncidentStatus.Closed);
        incident.UpdatedAt.Should().NotBeNull();
        _incidentRepositoryMock.Verify(repository => repository.SaveChangesAsync(), Times.Once);
        _auditLogServiceMock.Verify(service => service.LogAsync(
            "Incident", "Closed", "System",
            It.Is<string>(changes => changes.Contains("status from Resolved to Closed"))), Times.Once);
    }

    [Fact]
    public async Task UpdateStatusAsync_ShouldReturnNull_WhenIncidentDoesNotExist()
    {
        var id = Guid.NewGuid();
        _incidentRepositoryMock.Setup(repository => repository.GetByIdAsync(id))
            .ReturnsAsync((Incident?)null);

        var result = await _incidentService.UpdateStatusAsync(id,
            new UpdateIncidentStatusRequest { Status = "InProgress" });

        result.Should().BeNull();
        VerifyNoWrite();
    }

    [Fact]
    public async Task UpdateStatusAsync_ShouldThrowArgumentException_WhenStatusIsInvalid()
    {
        Func<Task> action = () => _incidentService.UpdateStatusAsync(Guid.NewGuid(),
            new UpdateIncidentStatusRequest { Status = "Unknown" });

        await action.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Incident status is invalid. Valid statuses are Open, InProgress, Resolved and Closed.");
        _incidentRepositoryMock.Verify(repository => repository.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        VerifyNoWrite();
    }

    [Fact]
    public async Task UpdateStatusAsync_ShouldThrowInvalidOperationException_WhenIncidentIsClosed()
    {
        var incident = CreateIncident(IncidentStatus.Closed);
        SetupExisting(incident);

        Func<Task> action = () => _incidentService.UpdateStatusAsync(incident.Id,
            new UpdateIncidentStatusRequest { Status = "Open" });

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Incident status cannot be changed from Closed to Open.");
        incident.Status.Should().Be(IncidentStatus.Closed);
        VerifyNoWrite();
    }

    [Fact]
    public async Task ResolveAsync_ShouldCloseIncident_WhenResolutionNotesAreValid()
    {
        var incident = CreateIncident(IncidentStatus.InProgress);
        SetupExisting(incident);

        var result = await _incidentService.ResolveAsync(incident.Id,
            new ResolveIncidentRequest { ResolutionNotes = " Package was replaced and customer was informed. " });

        result.Should().NotBeNull();
        result!.Status.Should().Be("Closed");
        result.ResolutionNotes.Should().Be("Package was replaced and customer was informed.");
        result.ClosedAt.Should().NotBeNull();
        incident.Status.Should().Be(IncidentStatus.Closed);
        incident.UpdatedAt.Should().NotBeNull();
        _incidentRepositoryMock.Verify(repository => repository.SaveChangesAsync(), Times.Once);
        _auditLogServiceMock.Verify(service => service.LogAsync(
            "Incident", "Closed", "System",
            It.Is<string>(changes =>
                changes.Contains("Closed incident") &&
                changes.Contains("Old status=InProgress") &&
                changes.Contains("New status=Closed") &&
                !changes.Contains("Resolution notes="))), Times.Once);
    }

    [Fact]
    public async Task ResolveAsync_ShouldThrowArgumentException_WhenResolutionNotesAreMissing()
    {
        Func<Task> action = () => _incidentService.ResolveAsync(Guid.NewGuid(),
            new ResolveIncidentRequest { ResolutionNotes = " " });

        await action.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Resolution notes are required.");
        _incidentRepositoryMock.Verify(repository => repository.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        VerifyNoWrite();
    }

    [Fact]
    public async Task ResolveAsync_ShouldReturnNull_WhenIncidentDoesNotExist()
    {
        var id = Guid.NewGuid();
        _incidentRepositoryMock.Setup(repository => repository.GetByIdAsync(id))
            .ReturnsAsync((Incident?)null);

        var result = await _incidentService.ResolveAsync(id,
            new ResolveIncidentRequest { ResolutionNotes = "Package was replaced." });

        result.Should().BeNull();
        VerifyNoWrite();
    }

    [Fact]
    public async Task ResolveAsync_ShouldThrowInvalidOperationException_WhenIncidentIsAlreadyClosed()
    {
        var incident = CreateIncident(IncidentStatus.Closed);
        SetupExisting(incident);

        Func<Task> action = () => _incidentService.ResolveAsync(incident.Id,
            new ResolveIncidentRequest { ResolutionNotes = "Package was replaced." });

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Closed incidents cannot be resolved again.");
        VerifyNoWrite();
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnFilteredIncidents_WhenStatusIsValid()
    {
        var incident = CreateIncident(IncidentStatus.Open);
        _incidentRepositoryMock.Setup(repository => repository.GetAllAsync(IncidentStatus.Open))
            .ReturnsAsync(new List<Incident> { incident });

        var result = await _incidentService.GetAllAsync("Open");

        result.Should().HaveCount(1);
        result[0].Status.Should().Be("Open");
        result[0].Title.Should().Be("Damaged package");
    }

    [Fact]
    public async Task CreateAsync_ShouldNotCopySensitiveTitleOrReferenceIntoAuditLog()
    {
        SetupCreate();
        var request = ValidRequest();
        request.Title = "Customer alice@example.com reported damage";
        request.RelatedEntityId = "alice@example.com";
        string? logChanges = null;
        _auditLogServiceMock.Setup(service => service.LogAsync(
                "Incident", "Created", "System", It.IsAny<string>()))
            .Callback<string, string, string, string>((_, _, _, changes) => logChanges = changes)
            .Returns(Task.CompletedTask);

        var result = await _incidentService.CreateAsync(request);

        result.Title.Should().Contain("alice@example.com");
        logChanges.Should().NotBeNull();
        logChanges.Should().Contain(result.Id.ToString());
        logChanges.Should().NotContain("alice@example.com");
        logChanges.Should().NotContain("Title=");
        _incidentRepositoryMock.Verify(repository => repository.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task ResolveAsync_ShouldNotCopySensitiveResolutionNotesIntoAuditLog()
    {
        var incident = CreateIncident(IncidentStatus.InProgress);
        SetupExisting(incident);
        string? logChanges = null;
        _auditLogServiceMock.Setup(service => service.LogAsync(
                "Incident", "Closed", "System", It.IsAny<string>()))
            .Callback<string, string, string, string>((_, _, _, changes) => logChanges = changes)
            .Returns(Task.CompletedTask);

        var result = await _incidentService.ResolveAsync(incident.Id,
            new ResolveIncidentRequest { ResolutionNotes = "Contact alice@example.com about replacement" });

        result!.ResolutionNotes.Should().Contain("alice@example.com");
        logChanges.Should().NotBeNull();
        logChanges.Should().Contain(incident.Id.ToString());
        logChanges.Should().Contain("New status=Closed");
        logChanges.Should().NotContain("alice@example.com");
        _incidentRepositoryMock.Verify(repository => repository.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateStatusAsync_ShouldLogStatusWithoutIncidentFreeText()
    {
        var incident = CreateIncident(IncidentStatus.Open);
        incident.Title = "alice@example.com";
        SetupExisting(incident);
        string? logChanges = null;
        _auditLogServiceMock.Setup(service => service.LogAsync(
                "Incident", "Updated", "System", It.IsAny<string>()))
            .Callback<string, string, string, string>((_, _, _, changes) => logChanges = changes)
            .Returns(Task.CompletedTask);

        var result = await _incidentService.UpdateStatusAsync(incident.Id,
            new UpdateIncidentStatusRequest { Status = "InProgress" });

        result!.Status.Should().Be("InProgress");
        logChanges.Should().NotBeNull();
        logChanges.Should().Contain("status from Open to InProgress");
        logChanges.Should().NotContain("alice@example.com");
    }

    private void SetupCreate()
    {
        _incidentRepositoryMock.Setup(repository => repository.AddAsync(It.IsAny<Incident>()))
            .Returns(Task.CompletedTask);
        _incidentRepositoryMock.Setup(repository => repository.SaveChangesAsync())
            .Returns(Task.CompletedTask);
        SetupAudit();
    }

    private void SetupExisting(Incident incident)
    {
        _incidentRepositoryMock.Setup(repository => repository.GetByIdAsync(incident.Id))
            .ReturnsAsync(incident);
        _incidentRepositoryMock.Setup(repository => repository.SaveChangesAsync())
            .Returns(Task.CompletedTask);
        SetupAudit();
    }

    private void SetupAudit()
    {
        _auditLogServiceMock.Setup(service => service.LogAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
    }

    private void VerifyNoWrite()
    {
        _incidentRepositoryMock.Verify(repository => repository.AddAsync(It.IsAny<Incident>()), Times.Never);
        _incidentRepositoryMock.Verify(repository => repository.SaveChangesAsync(), Times.Never);
        _auditLogServiceMock.Verify(service => service.LogAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    private static CreateIncidentRequest ValidRequest()
    {
        return new CreateIncidentRequest
        {
            Title = " Damaged package ",
            Description = " Package was damaged during handling. ",
            Severity = " High ",
            RelatedEntityType = " Order ",
            RelatedEntityId = " ORD-1001 "
        };
    }

    private static Incident CreateIncident(IncidentStatus status)
    {
        return new Incident
        {
            Id = Guid.NewGuid(),
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
