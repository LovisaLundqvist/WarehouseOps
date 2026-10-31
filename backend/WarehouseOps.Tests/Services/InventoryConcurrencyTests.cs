using FluentAssertions;
using Moq;
using WarehouseOps.Application.Dtos;
using WarehouseOps.Application.Exceptions;
using WarehouseOps.Application.Interfaces;
using WarehouseOps.Application.Services;
using WarehouseOps.Domain;

namespace WarehouseOps.Tests.Services;

public class InventoryConcurrencyTests
{
    [Fact]
    public async Task CreateOrderAsync_ShouldNotAudit_WhenInventorySaveConflicts()
    {
        var customerId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        var product = new Product
        {
            Id = productId,
            Name = "Barcode Scanner",
            Sku = "SCANNER-100",
            Category = "Equipment",
            Price = 100,
            CreatedAt = DateTime.UtcNow
        };

        var inventoryItem = new InventoryItem
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            Product = product,
            QuantityInStock = 5,
            MinimumStockLevel = 1,
            CreatedAt = DateTime.UtcNow
        };

        var orderRepository = new Mock<IOrderRepository>();
        var auditLogService = new Mock<IAuditLogService>();

        orderRepository.Setup(repository => repository.CustomerExistsAsync(customerId))
            .ReturnsAsync(true);
        orderRepository.Setup(repository => repository.GetProductByIdAsync(productId))
            .ReturnsAsync(product);
        orderRepository.Setup(repository => repository.GetInventoryByProductIdAsync(productId))
            .ReturnsAsync(inventoryItem);
        orderRepository.Setup(repository => repository.AddAsync(It.IsAny<Order>()))
            .Returns(Task.CompletedTask);
        orderRepository.Setup(repository => repository.SaveChangesAsync())
            .ThrowsAsync(new InventoryConcurrencyException(new Exception("Simulated conflict")));

        var service = new OrderService(orderRepository.Object, auditLogService.Object);

        var request = new CreateOrderRequest
        {
            CustomerId = customerId,
            Items =
            [
                new CreateOrderItemRequest
                {
                    ProductId = productId,
                    Quantity = 4
                }
            ]
        };

        var action = async () => await service.CreateAsync(request);

        await action.Should().ThrowAsync<InventoryConcurrencyException>()
            .WithMessage("Inventory was changed by another operation. Refresh and try again.");

        auditLogService.Verify(
            audit => audit.LogAsync(
                It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateInventoryAsync_ShouldNotAudit_WhenInventorySaveConflicts()
    {
        var inventoryId = Guid.NewGuid();
        var repository = new Mock<IInventoryRepository>();
        var auditLogService = new Mock<IAuditLogService>();

        repository.Setup(item => item.GetByIdAsync(inventoryId))
            .ReturnsAsync(new InventoryItem
            {
                Id = inventoryId,
                ProductId = Guid.NewGuid(),
                QuantityInStock = 5,
                MinimumStockLevel = 2,
                CreatedAt = DateTime.UtcNow
            });
        repository.Setup(item => item.SaveChangesAsync())
            .ThrowsAsync(new InventoryConcurrencyException(new Exception("Simulated conflict")));

        var service = new InventoryService(repository.Object, auditLogService.Object);
        var request = new UpdateInventoryItemRequest
        {
            QuantityInStock = 4,
            MinimumStockLevel = 2
        };

        var action = async () => await service.UpdateAsync(inventoryId, request);

        await action.Should().ThrowAsync<InventoryConcurrencyException>();

        auditLogService.Verify(
            audit => audit.LogAsync(
                It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }
}
