using FluentAssertions;
using Moq;
using WarehouseOps.Application.Dtos;
using WarehouseOps.Application.Interfaces;
using WarehouseOps.Application.Services;

namespace WarehouseOps.Tests.Services;

public class TransactionalOrderServiceTests
{
    [Fact]
    public async Task CreateAsync_RunsInsideTransaction()
    {
        var inner = new Mock<IOrderService>();
        var transaction = new RecordingTransactionExecutor();
        var service = new TransactionalOrderService(inner.Object, transaction);
        var request = new CreateOrderRequest();
        var expected = new OrderDto { Id = Guid.NewGuid() };

        inner.Setup(orderService => orderService.CreateAsync(request))
            .ReturnsAsync(expected);

        var actual = await service.CreateAsync(request);

        actual.Should().BeSameAs(expected);
        transaction.Executions.Should().Be(1);
        inner.Verify(orderService => orderService.CreateAsync(request), Times.Once);
    }

    [Fact]
    public async Task UpdateStatusAsync_RunsInsideTransaction()
    {
        var inner = new Mock<IOrderService>();
        var transaction = new RecordingTransactionExecutor();
        var service = new TransactionalOrderService(inner.Object, transaction);
        var orderId = Guid.NewGuid();
        var request = new UpdateOrderStatusRequest { Status = "Processing" };
        var expected = new OrderDto { Id = orderId, Status = "Processing" };

        inner.Setup(orderService => orderService.UpdateStatusAsync(orderId, request))
            .ReturnsAsync(expected);

        var actual = await service.UpdateStatusAsync(orderId, request);

        actual.Should().BeSameAs(expected);
        transaction.Executions.Should().Be(1);
        inner.Verify(orderService => orderService.UpdateStatusAsync(orderId, request), Times.Once);
    }

    [Fact]
    public async Task CancelAsync_RunsInsideTransaction()
    {
        var inner = new Mock<IOrderService>();
        var transaction = new RecordingTransactionExecutor();
        var service = new TransactionalOrderService(inner.Object, transaction);
        var orderId = Guid.NewGuid();
        var expected = new OrderDto { Id = orderId, Status = "Cancelled" };

        inner.Setup(orderService => orderService.CancelAsync(orderId))
            .ReturnsAsync(expected);

        var actual = await service.CancelAsync(orderId);

        actual.Should().BeSameAs(expected);
        transaction.Executions.Should().Be(1);
        inner.Verify(orderService => orderService.CancelAsync(orderId), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_DoesNotStartTransaction()
    {
        var inner = new Mock<IOrderService>();
        var transaction = new RecordingTransactionExecutor();
        var service = new TransactionalOrderService(inner.Object, transaction);
        var expected = new List<OrderDto>();

        inner.Setup(orderService => orderService.GetAllAsync()).ReturnsAsync(expected);

        var actual = await service.GetAllAsync();

        actual.Should().BeSameAs(expected);
        transaction.Executions.Should().Be(0);
    }

    [Fact]
    public async Task GetByIdAsync_DoesNotStartTransaction()
    {
        var inner = new Mock<IOrderService>();
        var transaction = new RecordingTransactionExecutor();
        var service = new TransactionalOrderService(inner.Object, transaction);
        var orderId = Guid.NewGuid();
        var expected = new OrderDto { Id = orderId };

        inner.Setup(orderService => orderService.GetByIdAsync(orderId)).ReturnsAsync(expected);

        var actual = await service.GetByIdAsync(orderId);

        actual.Should().BeSameAs(expected);
        transaction.Executions.Should().Be(0);
    }

    [Fact]
    public async Task CreateAsync_PropagatesAuditFailureInsideTransaction()
    {
        var inner = new Mock<IOrderService>();
        var transaction = new RecordingTransactionExecutor();
        var service = new TransactionalOrderService(inner.Object, transaction);
        var request = new CreateOrderRequest();

        inner.Setup(orderService => orderService.CreateAsync(request))
            .ThrowsAsync(new InvalidOperationException("Simulated audit failure."));

        var action = async () => await service.CreateAsync(request);

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
