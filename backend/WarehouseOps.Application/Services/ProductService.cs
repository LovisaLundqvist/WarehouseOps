using WarehouseOps.Application.Dtos;
using WarehouseOps.Application.Interfaces;
using WarehouseOps.Domain;

namespace WarehouseOps.Application.Services;

public class ProductService : IProductService
{
    private readonly IProductRepository _productRepository;

    public ProductService(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<List<ProductDto>> GetAllAsync(string? search, string? category)
    {
        var products = await _productRepository.GetAllAsync(search, category);

        return products
            .Select(MapToDto)
            .ToList();
    }

    public async Task<ProductDto?> GetByIdAsync(Guid id)
    {
        var product = await _productRepository.GetByIdAsync(id);

        if (product is null)
        {
            return null;
        }

        return MapToDto(product);
    }

    public async Task<ProductDto> CreateAsync(CreateProductRequest request)
    {
        ValidateRequest(request.Name, request.Sku, request.Category, request.Price);

        var skuExists = await _productRepository.SkuExistsAsync(request.Sku.Trim());

        if (skuExists)
        {
            throw new InvalidOperationException("A product with this SKU already exists.");
        }

        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Sku = request.Sku.Trim(),
            Category = request.Category.Trim(),
            Description = request.Description.Trim(),
            Price = request.Price,
            CreatedAt = DateTime.UtcNow
        };

        await _productRepository.AddAsync(product);

        await _productRepository.SaveChangesAsync();

        return MapToDto(product);
    }

    public async Task<ProductDto?> UpdateAsync(Guid id, UpdateProductRequest request)
    {
        ValidateRequest(request.Name, request.Sku, request.Category, request.Price);

        var product = await _productRepository.GetByIdAsync(id);

        if (product is null)
        {
            return null;
        }

        var skuExists = await _productRepository.SkuExistsAsync(request.Sku.Trim(), id);

        if (skuExists)
        {
            throw new InvalidOperationException("A product with this SKU already exists.");
        }

        product.Name = request.Name.Trim();
        product.Sku = request.Sku.Trim();
        product.Category = request.Category.Trim();
        product.Description = request.Description.Trim();
        product.Price = request.Price;
        product.UpdatedAt = DateTime.UtcNow;

        await _productRepository.SaveChangesAsync();

        return MapToDto(product);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var product = await _productRepository.GetByIdAsync(id);

        if (product is null)
        {
            return false;
        }

        _productRepository.Delete(product);

        await _productRepository.SaveChangesAsync();

        return true;
    }

    private static void ValidateRequest(string name, string sku, string category, decimal price)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Product name is required.");
        }

        if (string.IsNullOrWhiteSpace(sku))
        {
            throw new ArgumentException("SKU is required.");
        }

        if (string.IsNullOrWhiteSpace(category))
        {
            throw new ArgumentException("Category is required.");
        }

        if (price < 0)
        {
            throw new ArgumentException("Price cannot be negative.");
        }
    }

    private static ProductDto MapToDto(Product product)
    {
        return new ProductDto
        {
            Id = product.Id,
            Name = product.Name,
            Sku = product.Sku,
            Category = product.Category,
            Description = product.Description,
            Price = product.Price,
            CreatedAt = product.CreatedAt,
            UpdatedAt = product.UpdatedAt
        };
    }
}