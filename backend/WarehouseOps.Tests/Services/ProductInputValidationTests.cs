using FluentAssertions;
using Moq;
using WarehouseOps.Application.Dtos;
using WarehouseOps.Application.Interfaces;
using WarehouseOps.Application.Services;
using WarehouseOps.Domain;

namespace WarehouseOps.Tests.Services;

public class ProductInputValidationTests
{
    private readonly Mock<IProductRepository> _productRepository = new();
    private readonly Mock<IAuditLogService> _auditLogService = new();
    private readonly ProductService _service;

    public ProductInputValidationTests()
    {
        _service = new ProductService(_productRepository.Object, _auditLogService.Object);
    }

    [Fact]
    public async Task CreateAsync_RejectsSkuLongerThanDatabaseLimit()
    {
        var request = ValidCreateRequest();
        request.Sku = new string('S', 101);

        var action = () => _service.CreateAsync(request);

        await action.Should().ThrowAsync<ArgumentException>()
            .WithMessage("SKU cannot be longer than 100 characters.");
        VerifyNoPersistence();
    }

    [Fact]
    public async Task UpdateAsync_RejectsSkuLongerThanDatabaseLimit()
    {
        var request = ValidUpdateRequest();
        request.Sku = new string('S', 101);

        var action = () => _service.UpdateAsync(Guid.NewGuid(), request);

        await action.Should().ThrowAsync<ArgumentException>()
            .WithMessage("SKU cannot be longer than 100 characters.");
        _productRepository.Verify(repository => repository.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        VerifyNoPersistence();
    }

    [Fact]
    public async Task CreateAsync_RejectsMoreThanTwoPriceDecimals()
    {
        var request = ValidCreateRequest();
        request.Price = 19.999m;

        var action = () => _service.CreateAsync(request);

        await action.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Price cannot have more than two decimal places.");
        VerifyNoPersistence();
    }

    [Fact]
    public async Task UpdateAsync_RejectsPriceLargerThanDecimal18Scale2()
    {
        var request = ValidUpdateRequest();
        request.Price = 10000000000000000m;

        var action = () => _service.UpdateAsync(Guid.NewGuid(), request);

        await action.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Price exceeds the maximum allowed value.");
        _productRepository.Verify(repository => repository.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        VerifyNoPersistence();
    }

    [Fact]
    public async Task CreateAsync_AcceptsHundredCharacterSkuAndNullOptionalDescription()
    {
        var request = ValidCreateRequest();
        request.Sku = $" {new string('S', 100)} ";
        request.Description = null!;

        _productRepository.Setup(repository => repository.SkuExistsAsync(new string('S', 100), null))
            .ReturnsAsync(false);
        _productRepository.Setup(repository => repository.AddAsync(It.IsAny<Product>()))
            .Returns(Task.CompletedTask);
        _productRepository.Setup(repository => repository.SaveChangesAsync())
            .Returns(Task.CompletedTask);
        _auditLogService.Setup(service => service.LogAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var result = await _service.CreateAsync(request);

        result.Sku.Should().HaveLength(100);
        result.Description.Should().BeEmpty();
        _productRepository.Verify(repository => repository.AddAsync(It.Is<Product>(product =>
            product.Sku.Length == 100 && product.Description == string.Empty)), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_RejectsNullRequest()
    {
        var action = () => _service.CreateAsync(null!);

        await action.Should().ThrowAsync<ArgumentNullException>();
        VerifyNoPersistence();
    }

    private void VerifyNoPersistence()
    {
        _productRepository.Verify(repository => repository.AddAsync(It.IsAny<Product>()), Times.Never);
        _productRepository.Verify(repository => repository.SaveChangesAsync(), Times.Never);
        _auditLogService.Verify(service => service.LogAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    private static CreateProductRequest ValidCreateRequest() => new()
    {
        Name = "Scanner",
        Sku = "SCAN-001",
        Category = "Equipment",
        Description = "Barcode scanner",
        Price = 1299.95m
    };

    private static UpdateProductRequest ValidUpdateRequest() => new()
    {
        Name = "Scanner",
        Sku = "SCAN-001",
        Category = "Equipment",
        Description = "Barcode scanner",
        Price = 1299.95m
    };
}
