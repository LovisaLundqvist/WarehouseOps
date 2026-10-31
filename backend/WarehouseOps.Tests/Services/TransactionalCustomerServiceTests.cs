using FluentAssertions;
using Moq;
using WarehouseOps.Application.Dtos;
using WarehouseOps.Application.Interfaces;
using WarehouseOps.Application.Services;
using WarehouseOps.Tests.Helpers;

namespace WarehouseOps.Tests.Services;

public class TransactionalCustomerServiceTests
{
    [Fact]
    public async Task GetAllAsync_DoesNotStartTransaction()
    {
        var inner = new Mock<ICustomerService>();
        var transaction = new RecordingTransactionExecutor();
        var service = new TransactionalCustomerService(inner.Object, transaction);
        var search = "scanner";
        var expected = new List<CustomerDto>();

        inner.Setup(x => x.GetAllAsync(search)).ReturnsAsync(expected);

        var actual = await service.GetAllAsync(search);

        actual.Should().BeSameAs(expected);
        transaction.Executions.Should().Be(0);
        inner.Verify(x => x.GetAllAsync(search), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_DoesNotStartTransaction()
    {
        var inner = new Mock<ICustomerService>();
        var transaction = new RecordingTransactionExecutor();
        var service = new TransactionalCustomerService(inner.Object, transaction);
        var id = Guid.NewGuid();
        var expected = new CustomerDto { Id = Guid.NewGuid() };

        inner.Setup(x => x.GetByIdAsync(id)).ReturnsAsync(expected);

        var actual = await service.GetByIdAsync(id);

        actual.Should().BeSameAs(expected);
        transaction.Executions.Should().Be(0);
        inner.Verify(x => x.GetByIdAsync(id), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_UsesTransaction()
    {
        var inner = new Mock<ICustomerService>();
        var transaction = new RecordingTransactionExecutor();
        var service = new TransactionalCustomerService(inner.Object, transaction);
        var request = new CreateCustomerRequest();
        var expected = new CustomerDto { Id = Guid.NewGuid() };

        inner.Setup(x => x.CreateAsync(request)).ReturnsAsync(expected);

        var actual = await service.CreateAsync(request);

        actual.Should().BeSameAs(expected);
        transaction.Executions.Should().Be(1);
        inner.Verify(x => x.CreateAsync(request), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_UsesTransaction()
    {
        var inner = new Mock<ICustomerService>();
        var transaction = new RecordingTransactionExecutor();
        var service = new TransactionalCustomerService(inner.Object, transaction);
        var id = Guid.NewGuid();
        var request = new UpdateCustomerRequest();
        var expected = new CustomerDto { Id = Guid.NewGuid() };

        inner.Setup(x => x.UpdateAsync(id, request)).ReturnsAsync(expected);

        var actual = await service.UpdateAsync(id, request);

        actual.Should().BeSameAs(expected);
        transaction.Executions.Should().Be(1);
        inner.Verify(x => x.UpdateAsync(id, request), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_PropagatesAuditFailureInsideTransaction()
    {
        var inner = new Mock<ICustomerService>();
        var transaction = new RecordingTransactionExecutor();
        var service = new TransactionalCustomerService(inner.Object, transaction);
        var request = new CreateCustomerRequest();

        inner.Setup(x => x.CreateAsync(request))
            .ThrowsAsync(new InvalidOperationException("Simulated audit failure."));

        var action = async () => await service.CreateAsync(request);

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Simulated audit failure.");
        transaction.Executions.Should().Be(1);
    }
}
