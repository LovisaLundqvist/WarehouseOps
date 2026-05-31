using FluentAssertions;
using Moq;
using WarehouseOps.Application.Dtos;
using WarehouseOps.Application.Interfaces;
using WarehouseOps.Application.Services;
using WarehouseOps.Domain;

namespace WarehouseOps.Tests.Services;

public class ProductServiceTests
{
    private readonly Mock<IProductRepository> _productRepositoryMock;
    private readonly Mock<IAuditLogService> _auditLogServiceMock;
    private readonly ProductService _productService;

    public ProductServiceTests()
    {
        _productRepositoryMock = new Mock<IProductRepository>();
        _auditLogServiceMock = new Mock<IAuditLogService>();

        _productService = new ProductService(
            _productRepositoryMock.Object,
            _auditLogServiceMock.Object);
    }

    [Fact]
    public async Task CreateAsync_ShouldCreateProduct_WhenRequestIsValid()
    {
        var request = new CreateProductRequest
        {
            Name = " Barcode Scanner ",
            Sku = " SCAN-001 ",
            Category = " Equipment ",
            Description = " Wireless scanner ",
            Price = 1399
        };

        _productRepositoryMock
            .Setup(repository => repository.SkuExistsAsync("SCAN-001", null))
            .ReturnsAsync(false);

        _productRepositoryMock
            .Setup(repository => repository.AddAsync(It.IsAny<Product>()))
            .Returns(Task.CompletedTask);

        _productRepositoryMock
            .Setup(repository => repository.SaveChangesAsync())
            .Returns(Task.CompletedTask);

        _auditLogServiceMock
            .Setup(service => service.LogAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var result = await _productService.CreateAsync(request);

        result.Name.Should().Be("Barcode Scanner");
        result.Sku.Should().Be("SCAN-001");
        result.Category.Should().Be("Equipment");
        result.Description.Should().Be("Wireless scanner");
        result.Price.Should().Be(1399);

        _productRepositoryMock.Verify(
            repository => repository.AddAsync(It.Is<Product>(product =>
                product.Name == "Barcode Scanner" &&
                product.Sku == "SCAN-001" &&
                product.Category == "Equipment" &&
                product.Description == "Wireless scanner" &&
                product.Price == 1399)),
            Times.Once);

        _productRepositoryMock.Verify(
            repository => repository.SaveChangesAsync(),
            Times.Once);

        _auditLogServiceMock.Verify(
            service => service.LogAsync(
                "Product",
                "Created",
                "System",
                It.Is<string>(changes => changes.Contains("Created product Barcode Scanner"))),
            Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ShouldThrowArgumentException_WhenNameIsMissing()
    {
        var request = new CreateProductRequest
        {
            Name = " ",
            Sku = "SCAN-001",
            Category = "Equipment",
            Description = "Wireless scanner",
            Price = 1399
        };

        var action = async () => await _productService.CreateAsync(request);

        await action.Should()
            .ThrowAsync<ArgumentException>()
            .WithMessage("Product name is required.");

        _productRepositoryMock.Verify(
            repository => repository.AddAsync(It.IsAny<Product>()),
            Times.Never);

        _productRepositoryMock.Verify(
            repository => repository.SaveChangesAsync(),
            Times.Never);
    }

    [Fact]
    public async Task CreateAsync_ShouldThrowInvalidOperationException_WhenSkuAlreadyExists()
    {
        var request = new CreateProductRequest
        {
            Name = "Barcode Scanner",
            Sku = "SCAN-001",
            Category = "Equipment",
            Description = "Wireless scanner",
            Price = 1399
        };

        _productRepositoryMock
            .Setup(repository => repository.SkuExistsAsync("SCAN-001", null))
            .ReturnsAsync(true);

        var action = async () => await _productService.CreateAsync(request);

        await action.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("A product with this SKU already exists.");

        _productRepositoryMock.Verify(
            repository => repository.AddAsync(It.IsAny<Product>()),
            Times.Never);

        _productRepositoryMock.Verify(
            repository => repository.SaveChangesAsync(),
            Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnFalse_WhenProductDoesNotExist()
    {
        var productId = Guid.NewGuid();

        _productRepositoryMock
            .Setup(repository => repository.GetByIdAsync(productId))
            .ReturnsAsync((Product?)null);

        var result = await _productService.DeleteAsync(productId);

        result.Should().BeFalse();

        _productRepositoryMock.Verify(
            repository => repository.Delete(It.IsAny<Product>()),
            Times.Never);

        _productRepositoryMock.Verify(
            repository => repository.SaveChangesAsync(),
            Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_ShouldThrowInvalidOperationException_WhenProductIsInUse()
    {
        var productId = Guid.NewGuid();

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

        _productRepositoryMock
            .Setup(repository => repository.GetByIdAsync(productId))
            .ReturnsAsync(product);

        _productRepositoryMock
            .Setup(repository => repository.IsProductInUseAsync(productId))
            .ReturnsAsync(true);

        var action = async () => await _productService.DeleteAsync(productId);

        await action.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("This product cannot be deleted because it is used in inventory or order history.");

        _productRepositoryMock.Verify(
            repository => repository.Delete(It.IsAny<Product>()),
            Times.Never);

        _productRepositoryMock.Verify(
            repository => repository.SaveChangesAsync(),
            Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_ShouldDeleteProduct_WhenProductIsNotInUse()
    {
        var productId = Guid.NewGuid();

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

        _productRepositoryMock
            .Setup(repository => repository.GetByIdAsync(productId))
            .ReturnsAsync(product);

        _productRepositoryMock
            .Setup(repository => repository.IsProductInUseAsync(productId))
            .ReturnsAsync(false);

        _productRepositoryMock
            .Setup(repository => repository.SaveChangesAsync())
            .Returns(Task.CompletedTask);

        _auditLogServiceMock
            .Setup(service => service.LogAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var result = await _productService.DeleteAsync(productId);

        result.Should().BeTrue();

        _productRepositoryMock.Verify(
            repository => repository.Delete(product),
            Times.Once);

        _productRepositoryMock.Verify(
            repository => repository.SaveChangesAsync(),
            Times.Once);

        _auditLogServiceMock.Verify(
            service => service.LogAsync(
                "Product",
                "Deleted",
                "System",
                It.Is<string>(changes => changes.Contains("Deleted product Barcode Scanner"))),
            Times.Once);
    }
}
