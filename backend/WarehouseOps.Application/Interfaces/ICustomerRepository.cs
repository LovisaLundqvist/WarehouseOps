using WarehouseOps.Domain;

namespace WarehouseOps.Application.Interfaces;

public interface ICustomerRepository
{
    Task<List<Customer>> GetAllAsync(string? search);

    Task<Customer?> GetByIdAsync(Guid id);

    Task<bool> EmailExistsAsync(string email, Guid? excludedCustomerId = null);

    Task AddAsync(Customer customer);

    Task SaveChangesAsync();
}
