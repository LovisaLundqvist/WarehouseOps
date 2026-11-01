using FluentAssertions;
using Moq;
using WarehouseOps.Application.Dtos;
using WarehouseOps.Application.Interfaces;
using WarehouseOps.Application.Services;
using WarehouseOps.Domain;

namespace WarehouseOps.Tests.Services;

public class InventoryInputValidationTests
{
    private readonly Mock<IInventoryRepository> _repository = new();
    private readonly Mock<IAuditLogService> _auditLog = new();
    private readonly InventoryService _service;

    public InventoryInputValidationTests()
    {
        _service = new InventoryService(_repository.Object, _auditLog.Object);
    }

    [Fact]
    public async Task CreateAsync_RejectsNullRequest_BeforeRepositoryAccess()
    {
        Func<Task> action = () => _service.CreateAsync(null!);

        await action.Should().ThrowAsync<ArgumentNullException>();
        VerifyNoWrites();
        _repository.Verify(x => x.ProductExistsAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_RejectsNullRequest_BeforeRepositoryAccess()
    {
        Func<Task> action = () => _service.UpdateAsync(Guid.NewGuid(), null!);

        await action.Should().ThrowAsync<ArgumentNullException>();
        VerifyNoWrites();
        _repository.Verify(x => x.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_RejectsNegativeMinimumStockLevel_BeforeRepositoryAccess()
    {
        var request = new CreateInventoryItemRequest
        {
            ProductId = Guid.NewGuid(),
            QuantityInStock = 10,
            MinimumStockLevel = -1
        };

        Func<Task> action = () => _service.CreateAsync(request);

        await action.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Minimum stock level cannot be negative.");
        VerifyNoWrites();
        _repository.Verify(x => x.ProductExistsAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_RejectsNegativeMinimumStockLevel_BeforeRepositoryAccess()
    {
        var request = new UpdateInventoryItemRequest
        {
            QuantityInStock = 10,
            MinimumStockLevel = -1
        };

        Func<Task> action = () => _service.UpdateAsync(Guid.NewGuid(), request);

        await action.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Minimum stock level cannot be negative.");
        VerifyNoWrites();
        _repository.Verify(x => x.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Theory]
    [InlineData(0, 0, true)]
    [InlineData(int.MaxValue, int.MaxValue, true)]
    public async Task CreateAsync_AcceptsIntBoundaryValues(
        int quantity, int minimum, bool expectedLowStock)
    {
        var productId = Guid.NewGuid();
        InventoryItem? created = null;
        var request = new CreateInventoryItemRequest
        {
            ProductId = productId,
            QuantityInStock = quantity,
            MinimumStockLevel = minimum
        };

        _repository.Setup(x => x.ProductExistsAsync(productId)).ReturnsAsync(true);
        _repository.Setup(x => x.InventoryExistsForProductAsync(productId)).ReturnsAsync(false);
        _repository.Setup(x => x.AddAsync(It.IsAny<InventoryItem>()))
            .Callback<InventoryItem>(item => created = item)
            .Returns(Task.CompletedTask);
        _repository.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(() => created);
        SetupWrites();

        var result = await _service.CreateAsync(request);

        result.QuantityInStock.Should().Be(quantity);
        result.MinimumStockLevel.Should().Be(minimum);
        result.IsLowStock.Should().Be(expectedLowStock);
        _repository.Verify(x => x.AddAsync(It.IsAny<InventoryItem>()), Times.Once);
        _repository.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Theory]
    [InlineData(0, 0, true)]
    [InlineData(int.MaxValue, int.MaxValue, true)]
    public async Task UpdateAsync_AcceptsIntBoundaryValues(
        int quantity, int minimum, bool expectedLowStock)
    {
        var id = Guid.NewGuid();
        var inventoryItem = new InventoryItem
        {
            Id = id,
            ProductId = Guid.NewGuid(),
            QuantityInStock = 12,
            MinimumStockLevel = 3,
            CreatedAt = DateTime.UtcNow
        };

        _repository.Setup(x => x.GetByIdAsync(id)).ReturnsAsync(inventoryItem);
        SetupWrites();

        var result = await _service.UpdateAsync(id, new UpdateInventoryItemRequest
        {
            QuantityInStock = quantity,
            MinimumStockLevel = minimum
        });

        result.Should().NotBeNull();
        result!.QuantityInStock.Should().Be(quantity);
        result.MinimumStockLevel.Should().Be(minimum);
        result.IsLowStock.Should().Be(expectedLowStock);
        _repository.Verify(x => x.SaveChangesAsync(), Times.Once);
        _repository.Verify(x => x.AddAsync(It.IsAny<InventoryItem>()), Times.Never);
    }

    private void SetupWrites()
    {
        _repository.Setup(x => x.SaveChangesAsync()).Returns(Task.CompletedTask);
        _auditLog.Setup(x => x.LogAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .Returns(Task.CompletedTask);
    }

    private void VerifyNoWrites()
    {
        _repository.Verify(x => x.AddAsync(It.IsAny<InventoryItem>()), Times.Never);
        _repository.Verify(x => x.SaveChangesAsync(), Times.Never);
        _auditLog.Verify(x => x.LogAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>()), Times.Never);
    }
}
