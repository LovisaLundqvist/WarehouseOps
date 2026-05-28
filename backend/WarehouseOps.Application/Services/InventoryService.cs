using WarehouseOps.Application.Dtos;
using WarehouseOps.Application.Interfaces;
using WarehouseOps.Domain;

namespace WarehouseOps.Application.Services;

public class InventoryService : IInventoryService
{
    private readonly IInventoryRepository _inventoryRepository;

    public InventoryService(IInventoryRepository inventoryRepository)
    {
        _inventoryRepository = inventoryRepository;
    }

    public async Task<List<InventoryItemDto>> GetAllAsync()
    {
        var inventoryItems = await _inventoryRepository.GetAllAsync();

        return inventoryItems
            .Select(MapToDto)
            .ToList();
    }

    public async Task<List<InventoryItemDto>> GetLowStockAsync()
    {
        var inventoryItems = await _inventoryRepository.GetLowStockAsync();

        return inventoryItems
            .Select(MapToDto)
            .ToList();
    }

    public async Task<InventoryItemDto?> GetByIdAsync(Guid id)
    {
        var inventoryItem = await _inventoryRepository.GetByIdAsync(id);

        if (inventoryItem is null)
        {
            return null;
        }

        return MapToDto(inventoryItem);
    }

    public async Task<InventoryItemDto> CreateAsync(CreateInventoryItemRequest request)
    {
        ValidateCreateRequest(request);

        var productExists = await _inventoryRepository.ProductExistsAsync(request.ProductId);

        if (!productExists)
        {
            throw new InvalidOperationException("Product does not exist.");
        }

        var inventoryExists = await _inventoryRepository.InventoryExistsForProductAsync(request.ProductId);

        if (inventoryExists)
        {
            throw new InvalidOperationException("This product already has an inventory item.");
        }

        var inventoryItem = new InventoryItem
        {
            Id = Guid.NewGuid(),
            ProductId = request.ProductId,
            QuantityInStock = request.QuantityInStock,
            MinimumStockLevel = request.MinimumStockLevel,
            CreatedAt = DateTime.UtcNow
        };

        await _inventoryRepository.AddAsync(inventoryItem);

        await _inventoryRepository.SaveChangesAsync();

        var createdInventoryItem = await _inventoryRepository.GetByIdAsync(inventoryItem.Id);

        if (createdInventoryItem is null)
        {
            throw new InvalidOperationException("Inventory item could not be loaded after creation.");
        }

        return MapToDto(createdInventoryItem);
    }

    public async Task<InventoryItemDto?> UpdateAsync(Guid id, UpdateInventoryItemRequest request)
    {
        ValidateUpdateRequest(request);

        var inventoryItem = await _inventoryRepository.GetByIdAsync(id);

        if (inventoryItem is null)
        {
            return null;
        }

        inventoryItem.QuantityInStock = request.QuantityInStock;
        inventoryItem.MinimumStockLevel = request.MinimumStockLevel;
        inventoryItem.UpdatedAt = DateTime.UtcNow;

        await _inventoryRepository.SaveChangesAsync();

        return MapToDto(inventoryItem);
    }

    private static void ValidateCreateRequest(CreateInventoryItemRequest request)
    {
        if (request.ProductId == Guid.Empty)
        {
            throw new ArgumentException("Product id is required.");
        }

        if (request.QuantityInStock < 0)
        {
            throw new ArgumentException("Quantity in stock cannot be negative.");
        }

        if (request.MinimumStockLevel < 0)
        {
            throw new ArgumentException("Minimum stock level cannot be negative.");
        }
    }

    private static void ValidateUpdateRequest(UpdateInventoryItemRequest request)
    {
        if (request.QuantityInStock < 0)
        {
            throw new ArgumentException("Quantity in stock cannot be negative.");
        }

        if (request.MinimumStockLevel < 0)
        {
            throw new ArgumentException("Minimum stock level cannot be negative.");
        }
    }

    private static InventoryItemDto MapToDto(InventoryItem inventoryItem)
    {
        return new InventoryItemDto
        {
            Id = inventoryItem.Id,
            ProductId = inventoryItem.ProductId,
            ProductName = inventoryItem.Product?.Name ?? string.Empty,
            ProductSku = inventoryItem.Product?.Sku ?? string.Empty,
            QuantityInStock = inventoryItem.QuantityInStock,
            MinimumStockLevel = inventoryItem.MinimumStockLevel,
            IsLowStock = inventoryItem.QuantityInStock <= inventoryItem.MinimumStockLevel,
            CreatedAt = inventoryItem.CreatedAt,
            UpdatedAt = inventoryItem.UpdatedAt
        };
    }
}
