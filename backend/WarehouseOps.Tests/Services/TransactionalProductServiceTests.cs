using FluentAssertions;
using Moq;
using WarehouseOps.Application.Dtos;
using WarehouseOps.Application.Interfaces;
using WarehouseOps.Application.Services;
using WarehouseOps.Tests.Helpers;

namespace WarehouseOps.Tests.Services;

public class TransactionalProductServiceTests
{
    [Fact]
    public async Task GetAllAsync_DoesNotStartTransaction()
    {
        var inner = new Mock<IProductService>();
        var transaction = new RecordingTransactionExecutor();
        var service = new TransactionalProductService(inner.Object, transaction);
        var search = "scanner";
        var category = "equipment";
        var expected = new List<ProductDto>();

        inner.Setup(x => x.GetAllAsync(search, category)).ReturnsAsync(expected);

        var actual = await service.GetAllAsync(search, category);

        actual.Should().BeSameAs(expected);
        transaction.Executions.Should().Be(0);
        inner.Verify(x => x.GetAllAsync(search, category), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_DoesNotStartTransaction()
    {
        var inner = new Mock<IProductService>();
        var transaction = new RecordingTransactionExecutor();
        var service = new TransactionalProductService(inner.Object, transaction);
        var id = Guid.NewGuid();
        var expected = new ProductDto { Id = Guid.NewGuid() };

        inner.Setup(x => x.GetByIdAsync(id)).ReturnsAsync(expected);

        var actual = await service.GetByIdAsync(id);

        actual.Should().BeSameAs(expected);
        transaction.Executions.Should().Be(0);
        inner.Verify(x => x.GetByIdAsync(id), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_UsesTransaction()
    {
        var inner = new Mock<IProductService>();
        var transaction = new RecordingTransactionExecutor();
        var service = new TransactionalProductService(inner.Object, transaction);
        var request = new CreateProductRequest();
        var expected = new ProductDto { Id = Guid.NewGuid() };

        inner.Setup(x => x.CreateAsync(request)).ReturnsAsync(expected);

        var actual = await service.CreateAsync(request);

        actual.Should().BeSameAs(expected);
        transaction.Executions.Should().Be(1);
        inner.Verify(x => x.CreateAsync(request), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_UsesTransaction()
    {
        var inner = new Mock<IProductService>();
        var transaction = new RecordingTransactionExecutor();
        var service = new TransactionalProductService(inner.Object, transaction);
        var id = Guid.NewGuid();
        var request = new UpdateProductRequest();
        var expected = new ProductDto { Id = Guid.NewGuid() };

        inner.Setup(x => x.UpdateAsync(id, request)).ReturnsAsync(expected);

        var actual = await service.UpdateAsync(id, request);

        actual.Should().BeSameAs(expected);
        transaction.Executions.Should().Be(1);
        inner.Verify(x => x.UpdateAsync(id, request), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_UsesTransaction()
    {
        var inner = new Mock<IProductService>();
        var transaction = new RecordingTransactionExecutor();
        var service = new TransactionalProductService(inner.Object, transaction);
        var id = Guid.NewGuid();
        var expected = true;

        inner.Setup(x => x.DeleteAsync(id)).ReturnsAsync(expected);

        var actual = await service.DeleteAsync(id);

        actual.Should().Be(expected);
        transaction.Executions.Should().Be(1);
        inner.Verify(x => x.DeleteAsync(id), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_PropagatesAuditFailureInsideTransaction()
    {
        var inner = new Mock<IProductService>();
        var transaction = new RecordingTransactionExecutor();
        var service = new TransactionalProductService(inner.Object, transaction);
        var request = new CreateProductRequest();

        inner.Setup(x => x.CreateAsync(request))
            .ThrowsAsync(new InvalidOperationException("Simulated audit failure."));

        var action = async () => await service.CreateAsync(request);

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Simulated audit failure.");
        transaction.Executions.Should().Be(1);
    }
}
