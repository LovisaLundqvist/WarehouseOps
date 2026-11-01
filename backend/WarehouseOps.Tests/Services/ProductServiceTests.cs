using FluentAssertions;
using Moq;
using WarehouseOps.Application.Dtos;
using WarehouseOps.Application.Interfaces;
using WarehouseOps.Application.Services;
using WarehouseOps.Domain;

namespace WarehouseOps.Tests.Services;

public class ProductServiceTests
{
    private readonly Mock<IProductRepository> _productRepositoryMock = new();
    private readonly Mock<IAuditLogService> _auditLogServiceMock = new();
    private readonly ProductService _productService;

    public ProductServiceTests()
    {
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

        _productRepositoryMock.Setup(repo => repo.SkuExistsAsync("SCAN-001", null))
            .ReturnsAsync(false);
        _productRepositoryMock.Setup(repo => repo.AddAsync(It.IsAny<Product>()))
            .Returns(Task.CompletedTask);
        _productRepositoryMock.Setup(repo => repo.SaveChangesAsync())
            .Returns(Task.CompletedTask);
        SetupAuditLog();

        var result = await _productService.CreateAsync(request);

        result.Name.Should().Be("Barcode Scanner");
        result.Sku.Should().Be("SCAN-001");
        result.Category.Should().Be("Equipment");
        result.Description.Should().Be("Wireless scanner");
        result.Price.Should().Be(1399);

        _productRepositoryMock.Verify(repo => repo.AddAsync(It.Is<Product>(product =>
            product.Name == "Barcode Scanner" &&
            product.Sku == "SCAN-001" &&
            product.Category == "Equipment" &&
            product.Description == "Wireless scanner" &&
            product.Price == 1399)), Times.Once);
        _productRepositoryMock.Verify(repo => repo.SaveChangesAsync(), Times.Once);
        _auditLogServiceMock.Verify(audit => audit.LogAsync(
            "Product", "Created", "System",
            $"Created product {result.Id}."), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ShouldThrowArgumentException_WhenNameIsMissing()
    {
        var request = new CreateProductRequest
        {
            Name = " ", Sku = "SCAN-001", Category = "Equipment",
            Description = "Wireless scanner", Price = 1399
        };

        Func<Task> action = async () => await _productService.CreateAsync(request);

        await action.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Product name is required.");
        _productRepositoryMock.Verify(repo => repo.AddAsync(It.IsAny<Product>()), Times.Never);
        _productRepositoryMock.Verify(repo => repo.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_ShouldThrowInvalidOperationException_WhenSkuAlreadyExists()
    {
        var request = new CreateProductRequest
        {
            Name = "Barcode Scanner", Sku = "SCAN-001", Category = "Equipment",
            Description = "Wireless scanner", Price = 1399
        };
        _productRepositoryMock.Setup(repo => repo.SkuExistsAsync("SCAN-001", null))
            .ReturnsAsync(true);

        Func<Task> action = async () => await _productService.CreateAsync(request);

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("A product with this SKU already exists.");
        _productRepositoryMock.Verify(repo => repo.AddAsync(It.IsAny<Product>()), Times.Never);
        _productRepositoryMock.Verify(repo => repo.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnFalse_WhenProductDoesNotExist()
    {
        var id = Guid.NewGuid();
        _productRepositoryMock.Setup(repo => repo.GetByIdAsync(id))
            .ReturnsAsync((Product?)null);

        var result = await _productService.DeleteAsync(id);

        result.Should().BeFalse();
        _productRepositoryMock.Verify(repo => repo.Delete(It.IsAny<Product>()), Times.Never);
        _productRepositoryMock.Verify(repo => repo.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_ShouldThrowInvalidOperationException_WhenProductIsInUse()
    {
        var product = CreateProduct();
        _productRepositoryMock.Setup(repo => repo.GetByIdAsync(product.Id))
            .ReturnsAsync(product);
        _productRepositoryMock.Setup(repo => repo.IsProductInUseAsync(product.Id))
            .ReturnsAsync(true);

        Func<Task> action = async () => await _productService.DeleteAsync(product.Id);

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("This product cannot be deleted because it is used in inventory or order history.");
        _productRepositoryMock.Verify(repo => repo.Delete(It.IsAny<Product>()), Times.Never);
        _productRepositoryMock.Verify(repo => repo.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_ShouldDeleteProduct_WhenProductIsNotInUse()
    {
        var product = CreateProduct();
        SetupDelete(product);

        var result = await _productService.DeleteAsync(product.Id);

        result.Should().BeTrue();
        _productRepositoryMock.Verify(repo => repo.Delete(product), Times.Once);
        _productRepositoryMock.Verify(repo => repo.SaveChangesAsync(), Times.Once);
        _auditLogServiceMock.Verify(audit => audit.LogAsync(
            "Product", "Deleted", "System", $"Deleted product {product.Id}."), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ShouldNotCopyProductFieldsToAuditLog()
    {
        const string sensitiveName = "PRIVATE_PRODUCT_NAME_781";
        const string sensitiveSku = "PRIVATE_SKU_782";
        const string sensitiveCategory = "PRIVATE_CATEGORY_783";
        const string sensitiveDescription = "PRIVATE_DESCRIPTION_784";
        var request = new CreateProductRequest
        {
            Name = sensitiveName,
            Sku = sensitiveSku,
            Category = sensitiveCategory,
            Description = sensitiveDescription,
            Price = 123.45m
        };
        string? auditChanges = null;
        _productRepositoryMock.Setup(repo => repo.SkuExistsAsync(sensitiveSku, null))
            .ReturnsAsync(false);
        _productRepositoryMock.Setup(repo => repo.AddAsync(It.IsAny<Product>()))
            .Returns(Task.CompletedTask);
        _productRepositoryMock.Setup(repo => repo.SaveChangesAsync())
            .Returns(Task.CompletedTask);
        _auditLogServiceMock.Setup(audit => audit.LogAsync(
                "Product", "Created", "System", It.IsAny<string>()))
            .Callback<string, string, string, string>((_, _, _, changes) => auditChanges = changes)
            .Returns(Task.CompletedTask);

        var result = await _productService.CreateAsync(request);

        auditChanges.Should().Be($"Created product {result.Id}.");
        auditChanges.Should().NotContain(sensitiveName);
        auditChanges.Should().NotContain(sensitiveSku);
        auditChanges.Should().NotContain(sensitiveCategory);
        auditChanges.Should().NotContain(sensitiveDescription);
        _productRepositoryMock.Verify(repo => repo.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ShouldLogChangedFieldNamesWithoutValues()
    {
        var product = CreateProduct();
        product.Name = "OLD_PRIVATE_NAME_801";
        product.Sku = "OLD_PRIVATE_SKU_802";
        product.Category = "OLD_PRIVATE_CATEGORY_803";
        product.Description = "OLD_PRIVATE_DESCRIPTION_804";
        product.Price = 50;
        var request = new UpdateProductRequest
        {
            Name = "NEW_PRIVATE_NAME_811",
            Sku = "NEW_PRIVATE_SKU_812",
            Category = "NEW_PRIVATE_CATEGORY_813",
            Description = "NEW_PRIVATE_DESCRIPTION_814",
            Price = 77.50m
        };
        string? auditChanges = null;
        _productRepositoryMock.Setup(repo => repo.GetByIdAsync(product.Id))
            .ReturnsAsync(product);
        _productRepositoryMock.Setup(repo => repo.SkuExistsAsync(request.Sku, product.Id))
            .ReturnsAsync(false);
        _productRepositoryMock.Setup(repo => repo.SaveChangesAsync())
            .Returns(Task.CompletedTask);
        _auditLogServiceMock.Setup(audit => audit.LogAsync(
                "Product", "Updated", "System", It.IsAny<string>()))
            .Callback<string, string, string, string>((_, _, _, changes) => auditChanges = changes)
            .Returns(Task.CompletedTask);

        var result = await _productService.UpdateAsync(product.Id, request);

        result.Should().NotBeNull();
        result!.Name.Should().Be(request.Name);
        result.Sku.Should().Be(request.Sku);
        result.Description.Should().Be(request.Description);
        result.Price.Should().Be(request.Price);
        auditChanges.Should().Be(
            $"Updated product {product.Id}. Changed fields: Name, Sku, Category, Description, Price.");
        auditChanges.Should().NotContain("PRIVATE_NAME_");
        auditChanges.Should().NotContain("PRIVATE_SKU_");
        auditChanges.Should().NotContain("PRIVATE_CATEGORY_");
        auditChanges.Should().NotContain("PRIVATE_DESCRIPTION_");
        auditChanges.Should().NotContain("77.50");
    }

    [Fact]
    public async Task DeleteAsync_ShouldNotCopyProductNameOrSkuToAuditLog()
    {
        var product = CreateProduct();
        product.Name = "DELETE_PRIVATE_NAME_901";
        product.Sku = "DELETE_PRIVATE_SKU_902";
        string? auditChanges = null;
        _productRepositoryMock.Setup(repo => repo.GetByIdAsync(product.Id))
            .ReturnsAsync(product);
        _productRepositoryMock.Setup(repo => repo.IsProductInUseAsync(product.Id))
            .ReturnsAsync(false);
        _productRepositoryMock.Setup(repo => repo.SaveChangesAsync())
            .Returns(Task.CompletedTask);
        _auditLogServiceMock.Setup(audit => audit.LogAsync(
                "Product", "Deleted", "System", It.IsAny<string>()))
            .Callback<string, string, string, string>((_, _, _, changes) => auditChanges = changes)
            .Returns(Task.CompletedTask);

        var result = await _productService.DeleteAsync(product.Id);

        result.Should().BeTrue();
        auditChanges.Should().Be($"Deleted product {product.Id}.");
        auditChanges.Should().NotContain(product.Name);
        auditChanges.Should().NotContain(product.Sku);
        _productRepositoryMock.Verify(repo => repo.Delete(product), Times.Once);
    }

    private void SetupDelete(Product product)
    {
        _productRepositoryMock.Setup(repo => repo.GetByIdAsync(product.Id))
            .ReturnsAsync(product);
        _productRepositoryMock.Setup(repo => repo.IsProductInUseAsync(product.Id))
            .ReturnsAsync(false);
        _productRepositoryMock.Setup(repo => repo.SaveChangesAsync())
            .Returns(Task.CompletedTask);
        SetupAuditLog();
    }

    private void SetupAuditLog()
    {
        _auditLogServiceMock.Setup(audit => audit.LogAsync(
                It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
    }

    private static Product CreateProduct() => new()
    {
        Id = Guid.NewGuid(),
        Name = "Barcode Scanner",
        Sku = "SCAN-001",
        Category = "Equipment",
        Description = "Wireless scanner",
        Price = 1399,
        CreatedAt = DateTime.UtcNow
    };
}
