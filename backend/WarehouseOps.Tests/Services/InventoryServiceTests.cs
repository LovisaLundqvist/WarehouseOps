using FluentAssertions;
using Moq;
using WarehouseOps.Application.Dtos;
using WarehouseOps.Application.Interfaces;
using WarehouseOps.Application.Services;
using WarehouseOps.Domain;

namespace WarehouseOps.Tests.Services;

public class InventoryServiceTests
{
    private readonly Mock<IInventoryRepository> _inventoryRepositoryMock;
    private readonly Mock<IAuditLogService> _auditLogServiceMock;
    private readonly InventoryService _inventoryService;

    public InventoryServiceTests()
    {
        _inventoryRepositoryMock = new Mock<IInventoryRepository>();
        _auditLogServiceMock = new Mock<IAuditLogService>();

        _inventoryService = new InventoryService(
            _inventoryRepositoryMock.Object,
            _auditLogServiceMock.Object);
    }

    [Fact]
    public async Task CreateAsync_ShouldCreateInventoryItem_WhenRequestIsValid()
    {
        var productId = Guid.NewGuid();
        var inventoryItemId = Guid.NewGuid();

        var request = new CreateInventoryItemRequest
        {
            ProductId = productId,
            QuantityInStock = 10,
            MinimumStockLevel = 5
        };

        var product = new Product
        {
            Id = productId,
            Name = "Barcode Scanner",
            Sku = "SCAN-001",
            Category = "Equipment",
            Description = "Wireless scanner",
            Price = 1399,
            CreatedAt = DateTime.UtcNow
        };

        _inventoryRepositoryMock
            .Setup(repository => repository.ProductExistsAsync(productId))
            .ReturnsAsync(true);

        _inventoryRepositoryMock
            .Setup(repository => repository.InventoryExistsForProductAsync(productId))
            .ReturnsAsync(false);

        _inventoryRepositoryMock
            .Setup(repository => repository.AddAsync(It.IsAny<InventoryItem>()))
            .Callback<InventoryItem>(inventoryItem =>
            {
                inventoryItem.Id = inventoryItemId;
            })
            .Returns(Task.CompletedTask);

        _inventoryRepositoryMock
            .Setup(repository => repository.SaveChangesAsync())
            .Returns(Task.CompletedTask);

        _inventoryRepositoryMock
            .Setup(repository => repository.GetByIdAsync(inventoryItemId))
            .ReturnsAsync(new InventoryItem
            {
                Id = inventoryItemId,
                ProductId = productId,
                Product = product,
                QuantityInStock = 10,
                MinimumStockLevel = 5,
                CreatedAt = DateTime.UtcNow
            });

        _auditLogServiceMock
            .Setup(service => service.LogAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var result = await _inventoryService.CreateAsync(request);

        result.Id.Should().Be(inventoryItemId);
        result.ProductId.Should().Be(productId);
        result.ProductName.Should().Be("Barcode Scanner");
        result.ProductSku.Should().Be("SCAN-001");
        result.QuantityInStock.Should().Be(10);
        result.MinimumStockLevel.Should().Be(5);
        result.IsLowStock.Should().BeFalse();

        _inventoryRepositoryMock.Verify(
            repository => repository.AddAsync(It.Is<InventoryItem>(inventoryItem =>
                inventoryItem.ProductId == productId &&
                inventoryItem.QuantityInStock == 10 &&
                inventoryItem.MinimumStockLevel == 5)),
            Times.Once);

        _inventoryRepositoryMock.Verify(
            repository => repository.SaveChangesAsync(),
            Times.Once);

        _auditLogServiceMock.Verify(
            service => service.LogAsync(
                "Inventory",
                "Created",
                "System",
                It.Is<string>(changes => changes.Contains("Created inventory item for product Barcode Scanner"))),
            Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ShouldThrowArgumentException_WhenProductIdIsMissing()
    {
        var request = new CreateInventoryItemRequest
        {
            ProductId = Guid.Empty,
            QuantityInStock = 10,
            MinimumStockLevel = 5
        };

        var action = async () => await _inventoryService.CreateAsync(request);

        await action.Should()
            .ThrowAsync<ArgumentException>()
            .WithMessage("Product id is required.");

        _inventoryRepositoryMock.Verify(
            repository => repository.AddAsync(It.IsAny<InventoryItem>()),
            Times.Never);

        _inventoryRepositoryMock.Verify(
            repository => repository.SaveChangesAsync(),
            Times.Never);
    }

    [Fact]
    public async Task CreateAsync_ShouldThrowArgumentException_WhenQuantityIsNegative()
    {
        var request = new CreateInventoryItemRequest
        {
            ProductId = Guid.NewGuid(),
            QuantityInStock = -1,
            MinimumStockLevel = 5
        };

        var action = async () => await _inventoryService.CreateAsync(request);

        await action.Should()
            .ThrowAsync<ArgumentException>()
            .WithMessage("Quantity in stock cannot be negative.");

        _inventoryRepositoryMock.Verify(
            repository => repository.AddAsync(It.IsAny<InventoryItem>()),
            Times.Never);

        _inventoryRepositoryMock.Verify(
            repository => repository.SaveChangesAsync(),
            Times.Never);
    }

    [Fact]
    public async Task CreateAsync_ShouldThrowInvalidOperationException_WhenProductDoesNotExist()
    {
        var productId = Guid.NewGuid();

        var request = new CreateInventoryItemRequest
        {
            ProductId = productId,
            QuantityInStock = 10,
            MinimumStockLevel = 5
        };

        _inventoryRepositoryMock
            .Setup(repository => repository.ProductExistsAsync(productId))
            .ReturnsAsync(false);

        var action = async () => await _inventoryService.CreateAsync(request);

        await action.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("Product does not exist.");

        _inventoryRepositoryMock.Verify(
            repository => repository.AddAsync(It.IsAny<InventoryItem>()),
            Times.Never);

        _inventoryRepositoryMock.Verify(
            repository => repository.SaveChangesAsync(),
            Times.Never);
    }

    [Fact]
    public async Task CreateAsync_ShouldThrowInvalidOperationException_WhenInventoryAlreadyExistsForProduct()
    {
        var productId = Guid.NewGuid();

        var request = new CreateInventoryItemRequest
        {
            ProductId = productId,
            QuantityInStock = 10,
            MinimumStockLevel = 5
        };

        _inventoryRepositoryMock
            .Setup(repository => repository.ProductExistsAsync(productId))
            .ReturnsAsync(true);

        _inventoryRepositoryMock
            .Setup(repository => repository.InventoryExistsForProductAsync(productId))
            .ReturnsAsync(true);

        var action = async () => await _inventoryService.CreateAsync(request);

        await action.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("This product already has an inventory item.");

        _inventoryRepositoryMock.Verify(
            repository => repository.AddAsync(It.IsAny<InventoryItem>()),
            Times.Never);

        _inventoryRepositoryMock.Verify(
            repository => repository.SaveChangesAsync(),
            Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateInventoryItem_WhenRequestIsValid()
    {
        var inventoryItemId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        var inventoryItem = new InventoryItem
        {
            Id = inventoryItemId,
            ProductId = productId,
            Product = new Product
            {
                Id = productId,
                Name = "Barcode Scanner",
                Sku = "SCAN-001",
                Category = "Equipment",
                Description = "Wireless scanner",
                Price = 1399,
                CreatedAt = DateTime.UtcNow
            },
            QuantityInStock = 5,
            MinimumStockLevel = 2,
            CreatedAt = DateTime.UtcNow
        };

        var request = new UpdateInventoryItemRequest
        {
            QuantityInStock = 12,
            MinimumStockLevel = 4
        };

        _inventoryRepositoryMock
            .Setup(repository => repository.GetByIdAsync(inventoryItemId))
            .ReturnsAsync(inventoryItem);

        _inventoryRepositoryMock
            .Setup(repository => repository.SaveChangesAsync())
            .Returns(Task.CompletedTask);

        _auditLogServiceMock
            .Setup(service => service.LogAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var result = await _inventoryService.UpdateAsync(inventoryItemId, request);

        result.Should().NotBeNull();
        result!.QuantityInStock.Should().Be(12);
        result.MinimumStockLevel.Should().Be(4);
        result.IsLowStock.Should().BeFalse();

        inventoryItem.QuantityInStock.Should().Be(12);
        inventoryItem.MinimumStockLevel.Should().Be(4);
        inventoryItem.UpdatedAt.Should().NotBeNull();

        _inventoryRepositoryMock.Verify(
            repository => repository.SaveChangesAsync(),
            Times.Once);

        _auditLogServiceMock.Verify(
            service => service.LogAsync(
                "Inventory",
                "Updated",
                "System",
                It.Is<string>(changes =>
                    changes.Contains("Updated inventory item") &&
                    changes.Contains("Old values: QuantityInStock=5, MinimumStockLevel=2") &&
                    changes.Contains("New values: QuantityInStock=12, MinimumStockLevel=4"))),
            Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ShouldThrowArgumentException_WhenQuantityIsNegative()
    {
        var request = new UpdateInventoryItemRequest
        {
            QuantityInStock = -1,
            MinimumStockLevel = 4
        };

        var action = async () => await _inventoryService.UpdateAsync(Guid.NewGuid(), request);

        await action.Should()
            .ThrowAsync<ArgumentException>()
            .WithMessage("Quantity in stock cannot be negative.");

        _inventoryRepositoryMock.Verify(
            repository => repository.GetByIdAsync(It.IsAny<Guid>()),
            Times.Never);

        _inventoryRepositoryMock.Verify(
            repository => repository.SaveChangesAsync(),
            Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_ShouldReturnNull_WhenInventoryItemDoesNotExist()
    {
        var inventoryItemId = Guid.NewGuid();

        var request = new UpdateInventoryItemRequest
        {
            QuantityInStock = 12,
            MinimumStockLevel = 4
        };

        _inventoryRepositoryMock
            .Setup(repository => repository.GetByIdAsync(inventoryItemId))
            .ReturnsAsync((InventoryItem?)null);

        var result = await _inventoryService.UpdateAsync(inventoryItemId, request);

        result.Should().BeNull();

        _inventoryRepositoryMock.Verify(
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
    public async Task GetLowStockAsync_ShouldReturnMappedLowStockItems()
    {
        var productId = Guid.NewGuid();

        var lowStockItem = new InventoryItem
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            Product = new Product
            {
                Id = productId,
                Name = "Barcode Scanner",
                Sku = "SCAN-001",
                Category = "Equipment",
                Description = "Wireless scanner",
                Price = 1399,
                CreatedAt = DateTime.UtcNow
            },
            QuantityInStock = 2,
            MinimumStockLevel = 5,
            CreatedAt = DateTime.UtcNow
        };

        _inventoryRepositoryMock
            .Setup(repository => repository.GetLowStockAsync())
            .ReturnsAsync(new List<InventoryItem> { lowStockItem });

        var result = await _inventoryService.GetLowStockAsync();

        result.Should().HaveCount(1);
        result[0].ProductName.Should().Be("Barcode Scanner");
        result[0].ProductSku.Should().Be("SCAN-001");
        result[0].QuantityInStock.Should().Be(2);
        result[0].MinimumStockLevel.Should().Be(5);
        result[0].IsLowStock.Should().BeTrue();
    }
}
