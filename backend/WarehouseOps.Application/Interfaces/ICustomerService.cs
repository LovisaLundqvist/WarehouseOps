using WarehouseOps.Application.Dtos;

namespace WarehouseOps.Application.Interfaces;

public interface ICustomerService
{
    Task<List<CustomerDto>> GetAllAsync(string? search);

    Task<CustomerDto?> GetByIdAsync(Guid id);

    Task<CustomerDto> CreateAsync(CreateCustomerRequest request);

    Task<CustomerDto?> UpdateAsync(Guid id, UpdateCustomerRequest request);
}
