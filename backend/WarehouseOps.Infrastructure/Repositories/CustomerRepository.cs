using Microsoft.EntityFrameworkCore;
using WarehouseOps.Application.Interfaces;
using WarehouseOps.Domain;

namespace WarehouseOps.Infrastructure.Repositories;

public class CustomerRepository : ICustomerRepository
{
    private readonly ApplicationDbContext _context;

    public CustomerRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Customer>> GetAllAsync(string? search)
    {
        var query = _context.Customers.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchText = search.Trim();

            query = query.Where(customer =>
                customer.Name.Contains(searchText) ||
                customer.Email.Contains(searchText) ||
                customer.PhoneNumber.Contains(searchText));
        }

        return await query
            .OrderBy(customer => customer.Name)
            .ToListAsync();
    }

    public async Task<Customer?> GetByIdAsync(Guid id)
    {
        return await _context.Customers
            .FirstOrDefaultAsync(customer => customer.Id == id);
    }

    public async Task<bool> EmailExistsAsync(string email, Guid? excludedCustomerId = null)
    {
        return await _context.Customers
            .AnyAsync(customer =>
                customer.Email == email &&
                (!excludedCustomerId.HasValue || customer.Id != excludedCustomerId.Value));
    }

    public async Task AddAsync(Customer customer)
    {
        await _context.Customers.AddAsync(customer);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
