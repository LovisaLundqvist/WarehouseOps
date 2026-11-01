using FluentAssertions;
using Moq;
using WarehouseOps.Application.Dtos;
using WarehouseOps.Application.Interfaces;
using WarehouseOps.Application.Services;
using WarehouseOps.Domain;

namespace WarehouseOps.Tests.Services;

public class OrderInputValidationTests
{
    private readonly Mock<IOrderRepository> _repository = new();
    private readonly Mock<IAuditLogService> _auditLog = new();
    private readonly OrderService _service;

    public OrderInputValidationTests()
    {
        _service = new OrderService(_repository.Object, _auditLog.Object);
    }

    [Fact]
    public async Task CreateAsync_RejectsNullRequest()
    {
        Func<Task> action = async () => await _service.CreateAsync(null!);

        await action.Should().ThrowAsync<ArgumentNullException>();
        VerifyNothingWasSaved();
    }

    [Fact]
    public async Task CreateAsync_RejectsNullItemsList()
    {
        var request = new CreateOrderRequest
        {
            CustomerId = Guid.NewGuid(),
            Items = null!
        };

        Func<Task> action = async () => await _service.CreateAsync(request);

        await action.Should().ThrowAsync<ArgumentException>()
            .WithMessage("An order must contain at least one order item.");
        VerifyNothingWasSaved();
    }

    [Fact]
    public async Task CreateAsync_RejectsEmptyItemsList()
    {
        var request = new CreateOrderRequest { CustomerId = Guid.NewGuid() };

        Func<Task> action = async () => await _service.CreateAsync(request);

        await action.Should().ThrowAsync<ArgumentException>()
            .WithMessage("An order must contain at least one order item.");
        VerifyNothingWasSaved();
    }

    [Fact]
    public async Task CreateAsync_RejectsNullOrderItem()
    {
        var request = new CreateOrderRequest
        {
            CustomerId = Guid.NewGuid(),
            Items = [null!]
        };

        Func<Task> action = async () => await _service.CreateAsync(request);

        await action.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Order items cannot be null.");
        VerifyNothingWasSaved();
    }

    [Fact]
    public async Task CreateAsync_RejectsMissingProductId()
    {
        var request = NewRequest((Guid.Empty, 1));

        Func<Task> action = async () => await _service.CreateAsync(request);

        await action.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Product id is required.");
        VerifyNothingWasSaved();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task CreateAsync_RejectsNonPositiveQuantity(int quantity)
    {
        var request = NewRequest((Guid.NewGuid(), quantity));

        Func<Task> action = async () => await _service.CreateAsync(request);

        await action.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Quantity must be greater than zero.");
        VerifyNothingWasSaved();
    }

    [Fact]
    public async Task CreateAsync_RejectsLineAmountBeyondSqlDecimalLimit()
    {
        var productId = Guid.NewGuid();
        var request = NewRequest((productId, 2));
        var inventory = ArrangeProduct(productId, 9999999999999999.99m, 3);

        Func<Task> action = async () => await _service.CreateAsync(request);

        await action.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Order total exceeds the maximum allowed value.");
        inventory.QuantityInStock.Should().Be(3);
        inventory.UpdatedAt.Should().BeNull();
        VerifyNothingWasSaved();
    }

    [Fact]
    public async Task CreateAsync_RejectsCombinedAmountBeyondSqlDecimalLimit()
    {
        var firstId = Guid.NewGuid();
        var secondId = Guid.NewGuid();
        var request = NewRequest((firstId, 1), (secondId, 1));
        var firstInventory = ArrangeProduct(firstId, 6000000000000000m, 5);
        var secondInventory = ArrangeProduct(secondId, 5000000000000000m, 5);

        Func<Task> action = async () => await _service.CreateAsync(request);

        await action.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Order total exceeds the maximum allowed value.");
        firstInventory.QuantityInStock.Should().Be(5);
        secondInventory.QuantityInStock.Should().Be(5);
        firstInventory.UpdatedAt.Should().BeNull();
        secondInventory.UpdatedAt.Should().BeNull();
        VerifyNothingWasSaved();
    }

    [Fact]
    public async Task CreateAsync_DoesNotChangeEarlierInventory_WhenLaterItemHasInsufficientStock()
    {
        var firstId = Guid.NewGuid();
        var secondId = Guid.NewGuid();
        var request = NewRequest((firstId, 2), (secondId, 2));
        var firstInventory = ArrangeProduct(firstId, 100m, 10);
        var secondInventory = ArrangeProduct(secondId, 100m, 1);

        Func<Task> action = async () => await _service.CreateAsync(request);

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Not enough stock for product Test product.");
        firstInventory.QuantityInStock.Should().Be(10);
        secondInventory.QuantityInStock.Should().Be(1);
        firstInventory.UpdatedAt.Should().BeNull();
        secondInventory.UpdatedAt.Should().BeNull();
        VerifyNothingWasSaved();
    }

    [Fact]
    public async Task UpdateStatusAsync_RejectsNullRequest()
    {
        Func<Task> action = async () => await _service.UpdateStatusAsync(Guid.NewGuid(), null!);

        await action.Should().ThrowAsync<ArgumentNullException>();
        VerifyNothingWasSaved();
    }

    private static CreateOrderRequest NewRequest(params (Guid ProductId, int Quantity)[] items)
    {
        return new CreateOrderRequest
        {
            CustomerId = Guid.NewGuid(),
            Items = items.Select(item => new CreateOrderItemRequest
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity
            }).ToList()
        };
    }

    private InventoryItem ArrangeProduct(Guid productId, decimal price, int stock)
    {
        var product = new Product
        {
            Id = productId,
            Name = "Test product",
            Sku = productId.ToString(),
            Category = "Equipment",
            Price = price,
            CreatedAt = DateTime.UtcNow
        };
        var inventory = new InventoryItem
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            Product = product,
            QuantityInStock = stock,
            MinimumStockLevel = 0,
            CreatedAt = DateTime.UtcNow
        };

        _repository.Setup(repo => repo.CustomerExistsAsync(It.IsAny<Guid>())).ReturnsAsync(true);
        _repository.Setup(repo => repo.GetProductByIdAsync(productId)).ReturnsAsync(product);
        _repository.Setup(repo => repo.GetInventoryByProductIdAsync(productId)).ReturnsAsync(inventory);
        return inventory;
    }

    private void VerifyNothingWasSaved()
    {
        _repository.Verify(repo => repo.AddAsync(It.IsAny<Order>()), Times.Never);
        _repository.Verify(repo => repo.SaveChangesAsync(), Times.Never);
        _auditLog.Verify(log => log.LogAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }
}
