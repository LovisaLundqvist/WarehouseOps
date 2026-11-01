using WarehouseOps.Application.Dtos;
using WarehouseOps.Application.Interfaces;
using WarehouseOps.Domain;

namespace WarehouseOps.Application.Services;

public class ProductService : IProductService
{
    private readonly IProductRepository _productRepository;
    private readonly IAuditLogService _auditLogService;

    public ProductService(IProductRepository productRepository, IAuditLogService auditLogService)
    {
        _productRepository = productRepository;
        _auditLogService = auditLogService;
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
        ArgumentNullException.ThrowIfNull(request);
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
            Description = request.Description?.Trim() ?? string.Empty,
            Price = request.Price,
            CreatedAt = DateTime.UtcNow
        };

        await _productRepository.AddAsync(product);

        await _productRepository.SaveChangesAsync();

        await _auditLogService.LogAsync(
            "Product",
            "Created",
            "System",
            $"Created product {product.Id}.");

        return MapToDto(product);
    }

    public async Task<ProductDto?> UpdateAsync(Guid id, UpdateProductRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
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

        var name = request.Name.Trim();
        var sku = request.Sku.Trim();
        var category = request.Category.Trim();
        var description = request.Description?.Trim() ?? string.Empty;

        // Store field names, not user-provided values, in the audit trail.
        var changedFields = new List<string>();

        if (product.Name != name) changedFields.Add("Name");
        if (product.Sku != sku) changedFields.Add("Sku");
        if (product.Category != category) changedFields.Add("Category");
        if (product.Description != description) changedFields.Add("Description");
        if (product.Price != request.Price) changedFields.Add("Price");

        product.Name = name;
        product.Sku = sku;
        product.Category = category;
        product.Description = description;
        product.Price = request.Price;
        product.UpdatedAt = DateTime.UtcNow;

        await _productRepository.SaveChangesAsync();

        await _auditLogService.LogAsync(
            "Product",
            "Updated",
            "System",
            $"Updated product {product.Id}. Changed fields: {(changedFields.Count == 0 ? "None" : string.Join(", ", changedFields))}.");

        return MapToDto(product);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var product = await _productRepository.GetByIdAsync(id);

        if (product is null)
        {
            return false;
        }

        var isProductInUse = await _productRepository.IsProductInUseAsync(id);

        if (isProductInUse)
        {
            throw new InvalidOperationException("This product cannot be deleted because it is used in inventory or order history.");
        }

        _productRepository.Delete(product);

        await _productRepository.SaveChangesAsync();

        await _auditLogService.LogAsync(
            "Product",
            "Deleted",
            "System",
            $"Deleted product {product.Id}.");

        return true;
    }

    private static void ValidateRequest(string? name, string? sku, string? category, decimal price)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Product name is required.");
        }

        if (string.IsNullOrWhiteSpace(sku))
        {
            throw new ArgumentException("SKU is required.");
        }

        if (sku.Trim().Length > 100)
        {
            throw new ArgumentException("SKU cannot be longer than 100 characters.");
        }

        if (string.IsNullOrWhiteSpace(category))
        {
            throw new ArgumentException("Category is required.");
        }

        if (price < 0)
        {
            throw new ArgumentException("Price cannot be negative.");
        }

        if (price > 9999999999999999.99m)
        {
            throw new ArgumentException("Price exceeds the maximum allowed value.");
        }

        if (decimal.Round(price, 2) != price)
        {
            throw new ArgumentException("Price cannot have more than two decimal places.");
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
