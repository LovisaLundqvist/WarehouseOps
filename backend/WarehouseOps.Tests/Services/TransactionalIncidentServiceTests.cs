using FluentAssertions;
using Moq;
using WarehouseOps.Application.Dtos;
using WarehouseOps.Application.Interfaces;
using WarehouseOps.Application.Services;
using WarehouseOps.Tests.Helpers;

namespace WarehouseOps.Tests.Services;

public class TransactionalIncidentServiceTests
{
    [Fact]
    public async Task GetAllAsync_DoesNotStartTransaction()
    {
        var inner = new Mock<IIncidentService>();
        var transaction = new RecordingTransactionExecutor();
        var service = new TransactionalIncidentService(inner.Object, transaction);
        var status = "Open";
        var expected = new List<IncidentDto>();

        inner.Setup(x => x.GetAllAsync(status)).ReturnsAsync(expected);

        var actual = await service.GetAllAsync(status);

        actual.Should().BeSameAs(expected);
        transaction.Executions.Should().Be(0);
        inner.Verify(x => x.GetAllAsync(status), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_DoesNotStartTransaction()
    {
        var inner = new Mock<IIncidentService>();
        var transaction = new RecordingTransactionExecutor();
        var service = new TransactionalIncidentService(inner.Object, transaction);
        var id = Guid.NewGuid();
        var expected = new IncidentDto { Id = Guid.NewGuid() };

        inner.Setup(x => x.GetByIdAsync(id)).ReturnsAsync(expected);

        var actual = await service.GetByIdAsync(id);

        actual.Should().BeSameAs(expected);
        transaction.Executions.Should().Be(0);
        inner.Verify(x => x.GetByIdAsync(id), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_UsesTransaction()
    {
        var inner = new Mock<IIncidentService>();
        var transaction = new RecordingTransactionExecutor();
        var service = new TransactionalIncidentService(inner.Object, transaction);
        var request = new CreateIncidentRequest();
        var expected = new IncidentDto { Id = Guid.NewGuid() };

        inner.Setup(x => x.CreateAsync(request)).ReturnsAsync(expected);

        var actual = await service.CreateAsync(request);

        actual.Should().BeSameAs(expected);
        transaction.Executions.Should().Be(1);
        inner.Verify(x => x.CreateAsync(request), Times.Once);
    }

    [Fact]
    public async Task UpdateStatusAsync_UsesTransaction()
    {
        var inner = new Mock<IIncidentService>();
        var transaction = new RecordingTransactionExecutor();
        var service = new TransactionalIncidentService(inner.Object, transaction);
        var id = Guid.NewGuid();
        var request = new UpdateIncidentStatusRequest();
        var expected = new IncidentDto { Id = Guid.NewGuid() };

        inner.Setup(x => x.UpdateStatusAsync(id, request)).ReturnsAsync(expected);

        var actual = await service.UpdateStatusAsync(id, request);

        actual.Should().BeSameAs(expected);
        transaction.Executions.Should().Be(1);
        inner.Verify(x => x.UpdateStatusAsync(id, request), Times.Once);
    }

    [Fact]
    public async Task ResolveAsync_UsesTransaction()
    {
        var inner = new Mock<IIncidentService>();
        var transaction = new RecordingTransactionExecutor();
        var service = new TransactionalIncidentService(inner.Object, transaction);
        var id = Guid.NewGuid();
        var request = new ResolveIncidentRequest();
        var expected = new IncidentDto { Id = Guid.NewGuid() };

        inner.Setup(x => x.ResolveAsync(id, request)).ReturnsAsync(expected);

        var actual = await service.ResolveAsync(id, request);

        actual.Should().BeSameAs(expected);
        transaction.Executions.Should().Be(1);
        inner.Verify(x => x.ResolveAsync(id, request), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_PropagatesAuditFailureInsideTransaction()
    {
        var inner = new Mock<IIncidentService>();
        var transaction = new RecordingTransactionExecutor();
        var service = new TransactionalIncidentService(inner.Object, transaction);
        var request = new CreateIncidentRequest();

        inner.Setup(x => x.CreateAsync(request))
            .ThrowsAsync(new InvalidOperationException("Simulated audit failure."));

        var action = async () => await service.CreateAsync(request);

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Simulated audit failure.");
        transaction.Executions.Should().Be(1);
    }
}
