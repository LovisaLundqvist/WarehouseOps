using System.Net.Mail;
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
        ArgumentNullException.ThrowIfNull(request);
        ValidateRequest(request.Name, request.Email, request.PhoneNumber, request.Address);

        var name = request.Name.Trim();
        var email = request.Email.Trim();
        var phoneNumber = request.PhoneNumber?.Trim() ?? string.Empty;
        var address = request.Address?.Trim() ?? string.Empty;

        if (await _customerRepository.EmailExistsAsync(email))
        {
            throw new InvalidOperationException("A customer with this email already exists.");
        }

        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            Name = name,
            Email = email,
            PhoneNumber = phoneNumber,
            Address = address,
            CreatedAt = DateTime.UtcNow
        };

        await _customerRepository.AddAsync(customer);
        await _customerRepository.SaveChangesAsync();

        await _auditLogService.LogAsync(
            "Customer",
            "Created",
            "System",
            $"Created customer {customer.Id}.");

        return MapToDto(customer);
    }

    public async Task<CustomerDto?> UpdateAsync(Guid id, UpdateCustomerRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateRequest(request.Name, request.Email, request.PhoneNumber, request.Address);

        var customer = await _customerRepository.GetByIdAsync(id);

        if (customer is null)
        {
            return null;
        }

        var name = request.Name.Trim();
        var email = request.Email.Trim();
        var phoneNumber = request.PhoneNumber?.Trim() ?? string.Empty;
        var address = request.Address?.Trim() ?? string.Empty;

        if (await _customerRepository.EmailExistsAsync(email, id))
        {
            throw new InvalidOperationException("A customer with this email already exists.");
        }

        var changedFields = new List<string>();

        if (customer.Name != name) changedFields.Add("Name");
        if (customer.Email != email) changedFields.Add("Email");
        if (customer.PhoneNumber != phoneNumber) changedFields.Add("PhoneNumber");
        if (customer.Address != address) changedFields.Add("Address");

        customer.Name = name;
        customer.Email = email;
        customer.PhoneNumber = phoneNumber;
        customer.Address = address;
        customer.UpdatedAt = DateTime.UtcNow;

        await _customerRepository.SaveChangesAsync();

        var changedFieldsDescription = changedFields.Count == 0
            ? "None"
            : string.Join(", ", changedFields);

        await _auditLogService.LogAsync(
            "Customer",
            "Updated",
            "System",
            $"Updated customer {customer.Id}. Changed fields: {changedFieldsDescription}.");

        return MapToDto(customer);
    }

    private static void ValidateRequest(string? name, string? email, string? phoneNumber, string? address)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Customer name is required.");
        }

        if (name.Trim().Length > 200)
        {
            throw new ArgumentException("Customer name cannot be longer than 200 characters.");
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Customer email is required.");
        }

        var trimmedEmail = email.Trim();

        if (trimmedEmail.Length > 254)
        {
            throw new ArgumentException("Customer email cannot be longer than 254 characters.");
        }

        if (!MailAddress.TryCreate(trimmedEmail, out var parsedEmail) ||
            !string.Equals(parsedEmail.Address, trimmedEmail, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Customer email must be valid.");
        }

        if ((phoneNumber?.Trim().Length ?? 0) > 50)
        {
            throw new ArgumentException("Customer phone number cannot be longer than 50 characters.");
        }

        if ((address?.Trim().Length ?? 0) > 500)
        {
            throw new ArgumentException("Customer address cannot be longer than 500 characters.");
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
