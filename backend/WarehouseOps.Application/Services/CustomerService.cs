using WarehouseOps.Application.Dtos;
using WarehouseOps.Application.Interfaces;
using WarehouseOps.Domain;

namespace WarehouseOps.Application.Services;

public class CustomerService : ICustomerService
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IAuditLogService _auditLogService;

    public CustomerService(ICustomerRepository customerRepository, IAuditLogService auditLogService)
    {
        _customerRepository = customerRepository;
        _auditLogService = auditLogService;
    }

    public async Task<List<CustomerDto>> GetAllAsync(string? search)
    {
        var customers = await _customerRepository.GetAllAsync(search);

        return customers
            .Select(MapToDto)
            .ToList();
    }

    public async Task<CustomerDto?> GetByIdAsync(Guid id)
    {
        var customer = await _customerRepository.GetByIdAsync(id);

        if (customer is null)
        {
            return null;
        }

        return MapToDto(customer);
    }

    public async Task<CustomerDto> CreateAsync(CreateCustomerRequest request)
    {
        ValidateRequest(request.Name, request.Email);

        var trimmedEmail = request.Email.Trim();

        var emailExists = await _customerRepository.EmailExistsAsync(trimmedEmail);

        if (emailExists)
        {
            throw new InvalidOperationException("A customer with this email already exists.");
        }

        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Email = trimmedEmail,
            PhoneNumber = request.PhoneNumber.Trim(),
            Address = request.Address.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        await _customerRepository.AddAsync(customer);

        await _customerRepository.SaveChangesAsync();

        await _auditLogService.LogAsync(
            "Customer",
            "Created",
            "System",
            $"Created customer {customer.Name} with email {customer.Email}.");

        return MapToDto(customer);
    }

    public async Task<CustomerDto?> UpdateAsync(Guid id, UpdateCustomerRequest request)
    {
        ValidateRequest(request.Name, request.Email);

        var customer = await _customerRepository.GetByIdAsync(id);

        if (customer is null)
        {
            return null;
        }

        var trimmedEmail = request.Email.Trim();

        var emailExists = await _customerRepository.EmailExistsAsync(trimmedEmail, id);

        if (emailExists)
        {
            throw new InvalidOperationException("A customer with this email already exists.");
        }

        var oldName = customer.Name;
        var oldEmail = customer.Email;
        var oldPhoneNumber = customer.PhoneNumber;
        var oldAddress = customer.Address;

        customer.Name = request.Name.Trim();
        customer.Email = trimmedEmail;
        customer.PhoneNumber = request.PhoneNumber.Trim();
        customer.Address = request.Address.Trim();
        customer.UpdatedAt = DateTime.UtcNow;

        await _customerRepository.SaveChangesAsync();

        await _auditLogService.LogAsync(
            "Customer",
            "Updated",
            "System",
            $"Updated customer {customer.Id}. Old values: Name={oldName}, Email={oldEmail}, PhoneNumber={oldPhoneNumber}, Address={oldAddress}. New values: Name={customer.Name}, Email={customer.Email}, PhoneNumber={customer.PhoneNumber}, Address={customer.Address}.");

        return MapToDto(customer);
    }

    private static void ValidateRequest(string name, string email)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Customer name is required.");
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Customer email is required.");
        }

        if (!email.Contains('@'))
        {
            throw new ArgumentException("Customer email must be valid.");
        }
    }

    private static CustomerDto MapToDto(Customer customer)
    {
        return new CustomerDto
        {
            Id = customer.Id,
            Name = customer.Name,
            Email = customer.Email,
            PhoneNumber = customer.PhoneNumber,
            Address = customer.Address,
            CreatedAt = customer.CreatedAt,
            UpdatedAt = customer.UpdatedAt
        };
    }
}
