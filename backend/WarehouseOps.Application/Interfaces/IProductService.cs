using WarehouseOps.Application.Dtos;

namespace WarehouseOps.Application.Interfaces;

public interface IProductService
{
    Task<List<ProductDto>> GetAllAsync(string? search, string? category);

    Task<ProductDto?> GetByIdAsync(Guid id);

    Task<ProductDto> CreateAsync(CreateProductRequest request);

    Task<ProductDto?> UpdateAsync(Guid id, UpdateProductRequest request);

    Task<bool> DeleteAsync(Guid id);
}