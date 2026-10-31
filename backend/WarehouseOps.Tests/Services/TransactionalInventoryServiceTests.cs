using FluentAssertions;
using Moq;
using WarehouseOps.Application.Dtos;
using WarehouseOps.Application.Interfaces;
using WarehouseOps.Application.Services;

namespace WarehouseOps.Tests.Services;

public class TransactionalInventoryServiceTests
{
    [Fact]
    public async Task CreateAsync_RunsInsideTransaction()
    {
        var inner = new Mock<IInventoryService>();
        var transaction = new RecordingTransactionExecutor();
        var service = new TransactionalInventoryService(inner.Object, transaction);
        var request = new CreateInventoryItemRequest();
        var expected = new InventoryItemDto { Id = Guid.NewGuid() };

        inner.Setup(inventoryService => inventoryService.CreateAsync(request))
            .ReturnsAsync(expected);

        var actual = await service.CreateAsync(request);

        actual.Should().BeSameAs(expected);
        transaction.Executions.Should().Be(1);
        inner.Verify(inventoryService => inventoryService.CreateAsync(request), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_RunsInsideTransaction()
    {
        var inner = new Mock<IInventoryService>();
        var transaction = new RecordingTransactionExecutor();
        var service = new TransactionalInventoryService(inner.Object, transaction);
        var inventoryId = Guid.NewGuid();
        var request = new UpdateInventoryItemRequest();
        var expected = new InventoryItemDto { Id = inventoryId, QuantityInStock = 6 };

        inner.Setup(inventoryService => inventoryService.UpdateAsync(inventoryId, request))
            .ReturnsAsync(expected);

        var actual = await service.UpdateAsync(inventoryId, request);

        actual.Should().BeSameAs(expected);
        transaction.Executions.Should().Be(1);
        inner.Verify(inventoryService => inventoryService.UpdateAsync(inventoryId, request), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_DoesNotStartTransaction()
    {
        var inner = new Mock<IInventoryService>();
        var transaction = new RecordingTransactionExecutor();
        var service = new TransactionalInventoryService(inner.Object, transaction);
        var expected = new List<InventoryItemDto>();

        inner.Setup(inventoryService => inventoryService.GetAllAsync())
            .ReturnsAsync(expected);

        var actual = await service.GetAllAsync();

        actual.Should().BeSameAs(expected);
        transaction.Executions.Should().Be(0);
    }

    [Fact]
    public async Task GetLowStockAsync_DoesNotStartTransaction()
    {
        var inner = new Mock<IInventoryService>();
        var transaction = new RecordingTransactionExecutor();
        var service = new TransactionalInventoryService(inner.Object, transaction);
        var expected = new List<InventoryItemDto>();

        inner.Setup(inventoryService => inventoryService.GetLowStockAsync())
            .ReturnsAsync(expected);

        var actual = await service.GetLowStockAsync();

        actual.Should().BeSameAs(expected);
        transaction.Executions.Should().Be(0);
    }

    [Fact]
    public async Task GetByIdAsync_DoesNotStartTransaction()
    {
        var inner = new Mock<IInventoryService>();
        var transaction = new RecordingTransactionExecutor();
        var service = new TransactionalInventoryService(inner.Object, transaction);
        var inventoryId = Guid.NewGuid();
        var expected = new InventoryItemDto { Id = inventoryId };

        inner.Setup(inventoryService => inventoryService.GetByIdAsync(inventoryId))
            .ReturnsAsync(expected);

        var actual = await service.GetByIdAsync(inventoryId);

        actual.Should().BeSameAs(expected);
        transaction.Executions.Should().Be(0);
    }

    [Fact]
    public async Task UpdateAsync_PropagatesAuditFailureInsideTransaction()
    {
        var inner = new Mock<IInventoryService>();
        var transaction = new RecordingTransactionExecutor();
        var service = new TransactionalInventoryService(inner.Object, transaction);
        var inventoryId = Guid.NewGuid();
        var request = new UpdateInventoryItemRequest();

        inner.Setup(inventoryService => inventoryService.UpdateAsync(inventoryId, request))
            .ThrowsAsync(new InvalidOperationException("Simulated audit failure."));

        var action = async () => await service.UpdateAsync(inventoryId, request);

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Simulated audit failure.");
        transaction.Executions.Should().Be(1);
    }

    private sealed class RecordingTransactionExecutor : ITransactionExecutor
    {
        public int Executions { get; private set; }

        public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation)
        {
            Executions++;
            return await operation();
        }
    }
}
