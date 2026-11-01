using FluentAssertions;
using Moq;
using WarehouseOps.Application.Dtos;
using WarehouseOps.Application.Interfaces;
using WarehouseOps.Application.Services;
using WarehouseOps.Domain;

namespace WarehouseOps.Tests.Services;

public class IncidentInputValidationTests
{
    private readonly Mock<IIncidentRepository> _repository = new();
    private readonly Mock<IAuditLogService> _auditLog = new();
    private readonly IncidentService _service;

    public IncidentInputValidationTests()
    {
        _service = new IncidentService(_repository.Object, _auditLog.Object);

        _repository
            .Setup(repository => repository.AddAsync(It.IsAny<Incident>()))
            .Returns(Task.CompletedTask);

        _repository
            .Setup(repository => repository.SaveChangesAsync())
            .Returns(Task.CompletedTask);

        _auditLog
            .Setup(service => service.LogAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .Returns(Task.CompletedTask);
    }

    [Fact]
    public async Task CreateAsync_RejectsTitleLongerThan200Characters()
    {
        var request = CreateRequest();
        request.Title = new string('T', 201);

        Func<Task> action = () => _service.CreateAsync(request);

        await action.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Incident title cannot be longer than 200 characters.");

        VerifyNoIncidentWasSaved();
    }

    [Fact]
    public async Task CreateAsync_RejectsDescriptionLongerThan4000Characters()
    {
        var request = CreateRequest();
        request.Description = new string('D', 4001);

        Func<Task> action = () => _service.CreateAsync(request);

        await action.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Incident description cannot be longer than 4000 characters.");

        VerifyNoIncidentWasSaved();
    }

    [Fact]
    public async Task CreateAsync_RejectsRelatedEntityIdLongerThan100Characters()
    {
        var request = CreateRequest();
        request.RelatedEntityId = new string('I', 101);

        Func<Task> action = () => _service.CreateAsync(request);

        await action.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Related entity id cannot be longer than 100 characters.");

        VerifyNoIncidentWasSaved();
    }

    [Fact]
    public async Task CreateAsync_AcceptsTitleAt200CharacterLimitAfterTrimming()
    {
        var request = CreateRequest();
        request.Title = "  " + new string('T', 200) + "  ";

        var result = await _service.CreateAsync(request);

        result.Title.Should().HaveLength(200);
        result.Title.Should().Be(new string('T', 200));
        _repository.Verify(repository => repository.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_AcceptsDescriptionAt4000CharacterLimit()
    {
        var request = CreateRequest();
        request.Description = new string('D', 4000);

        var result = await _service.CreateAsync(request);

        result.Description.Should().HaveLength(4000);
        _repository.Verify(repository => repository.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_AcceptsRelatedEntityIdAt100CharacterLimitAfterTrimming()
    {
        var request = CreateRequest();
        request.RelatedEntityId = "  " + new string('I', 100) + "  ";

        var result = await _service.CreateAsync(request);

        result.RelatedEntityId.Should().HaveLength(100);
        result.RelatedEntityId.Should().Be(new string('I', 100));
        _repository.Verify(repository => repository.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_TreatsNullRelatedEntityIdAsEmpty()
    {
        var request = CreateRequest();
        request.RelatedEntityId = null!;

        var result = await _service.CreateAsync(request);

        result.RelatedEntityId.Should().BeEmpty();
        _repository.Verify(repository => repository.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task ResolveAsync_RejectsResolutionNotesLongerThan4000Characters()
    {
        var request = new ResolveIncidentRequest
        {
            ResolutionNotes = new string('R', 4001)
        };

        Func<Task> action = () => _service.ResolveAsync(Guid.NewGuid(), request);

        await action.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Resolution notes cannot be longer than 4000 characters.");

        _repository.Verify(repository => repository.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        _repository.Verify(repository => repository.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task ResolveAsync_AcceptsResolutionNotesAt4000CharacterLimitAfterTrimming()
    {
        var incidentId = Guid.NewGuid();
        var incident = new Incident
        {
            Id = incidentId,
            Title = "Damaged package",
            Description = "Package was damaged",
            Severity = IncidentSeverity.Medium,
            RelatedEntityType = IncidentRelatedEntityType.General,
            Status = IncidentStatus.InProgress,
            CreatedAt = DateTime.UtcNow
        };

        _repository
            .Setup(repository => repository.GetByIdAsync(incidentId))
            .ReturnsAsync(incident);

        var request = new ResolveIncidentRequest
        {
            ResolutionNotes = "  " + new string('R', 4000) + "  "
        };

        var result = await _service.ResolveAsync(incidentId, request);

        result.Should().NotBeNull();
        result!.ResolutionNotes.Should().HaveLength(4000);
        result.Status.Should().Be("Closed");
        _repository.Verify(repository => repository.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_RejectsNullRequestBeforeRepositoryAccess()
    {
        Func<Task> action = () => _service.CreateAsync(null!);

        await action.Should().ThrowAsync<ArgumentNullException>();
        VerifyNoIncidentWasSaved();
    }

    [Fact]
    public async Task ResolveAsync_RejectsNullRequestBeforeRepositoryAccess()
    {
        Func<Task> action = () => _service.ResolveAsync(Guid.NewGuid(), null!);

        await action.Should().ThrowAsync<ArgumentNullException>();
        _repository.Verify(repository => repository.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        _repository.Verify(repository => repository.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task UpdateStatusAsync_RejectsNullRequestBeforeRepositoryAccess()
    {
        Func<Task> action = () => _service.UpdateStatusAsync(Guid.NewGuid(), null!);

        await action.Should().ThrowAsync<ArgumentNullException>();
        _repository.Verify(repository => repository.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        _repository.Verify(repository => repository.SaveChangesAsync(), Times.Never);
    }

    private static CreateIncidentRequest CreateRequest() => new()
    {
        Title = "Damaged package",
        Description = "The package arrived damaged",
        Severity = "Medium",
        RelatedEntityType = "General",
        RelatedEntityId = string.Empty
    };

    private void VerifyNoIncidentWasSaved()
    {
        _repository.Verify(repository => repository.AddAsync(It.IsAny<Incident>()), Times.Never);
        _repository.Verify(repository => repository.SaveChangesAsync(), Times.Never);
        _auditLog.Verify(service => service.LogAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>()), Times.Never);
    }
}
