using FluentAssertions;
using Moq;
using WarehouseOps.Application.Dtos;
using WarehouseOps.Application.Interfaces;
using WarehouseOps.Application.Services;
using WarehouseOps.Domain;

namespace WarehouseOps.Tests.Services;

public class CustomerServiceSecurityTests
{
    private readonly Mock<ICustomerRepository> _repository = new();
    private readonly Mock<IAuditLogService> _auditLog = new();

    private CustomerService CreateService()
    {
        _repository.Setup(x => x.AddAsync(It.IsAny<Customer>()))
            .Returns(Task.CompletedTask);
        _repository.Setup(x => x.SaveChangesAsync())
            .Returns(Task.CompletedTask);
        _auditLog.Setup(x => x.LogAsync(
                It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        return new CustomerService(_repository.Object, _auditLog.Object);
    }

    private static CreateCustomerRequest ValidRequest() => new()
    {
        Name = "Nordic Retail AB",
        Email = "contact@nordic.example",
        PhoneNumber = "0701234567",
        Address = "Warehouse Street 10"
    };

    [Fact]
    public async Task Create_DoesNotWriteContactInformationToAuditLog()
    {
        var service = CreateService();
        var request = ValidRequest();
        string? changes = null;

        _auditLog.Setup(x => x.LogAsync("Customer", "Created", "System", It.IsAny<string>()))
            .Callback<string, string, string, string>((_, _, _, value) => changes = value)
            .Returns(Task.CompletedTask);

        var created = await service.CreateAsync(request);

        created.Email.Should().Be(request.Email);
        created.PhoneNumber.Should().Be(request.PhoneNumber);
        changes.Should().Be($"Created customer {created.Id}.");
        changes.Should().NotContain(request.Name)
            .And.NotContain(request.Email)
            .And.NotContain(request.PhoneNumber)
            .And.NotContain(request.Address);
    }

    [Fact]
    public async Task Update_LogsChangedFieldNamesWithoutOldOrNewValues()
    {
        var service = CreateService();
        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            Name = "Old Company",
            Email = "old@example.com",
            PhoneNumber = "111111",
            Address = "Old Road 1"
        };
        _repository.Setup(x => x.GetByIdAsync(customer.Id)).ReturnsAsync(customer);

        string? changes = null;
        _auditLog.Setup(x => x.LogAsync("Customer", "Updated", "System", It.IsAny<string>()))
            .Callback<string, string, string, string>((_, _, _, value) => changes = value)
            .Returns(Task.CompletedTask);

        var request = new UpdateCustomerRequest
        {
            Name = "New Company",
            Email = "new@example.com",
            PhoneNumber = "222222",
            Address = "New Road 2"
        };

        var result = await service.UpdateAsync(customer.Id, request);

        result.Should().NotBeNull();
        result!.Email.Should().Be("new@example.com");
        changes.Should().Be($"Updated customer {customer.Id}. Changed fields: Name, Email, PhoneNumber, Address.");
        foreach (var value in new[]
                 {
                     "Old Company", "New Company", "old@example.com", "new@example.com",
                     "111111", "222222", "Old Road 1", "New Road 2"
                 })
        {
            changes.Should().NotContain(value);
        }
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("person@")]
    [InlineData("name@@example.com")]
    public async Task Create_RejectsMalformedEmail(string email)
    {
        var service = CreateService();
        var request = ValidRequest();
        request.Email = email;

        var act = async () => await service.CreateAsync(request);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Customer email must be valid.");
        _repository.Verify(x => x.AddAsync(It.IsAny<Customer>()), Times.Never);
    }

    [Fact]
    public async Task Create_RejectsNameLongerThan200Characters()
    {
        var service = CreateService();
        var request = ValidRequest();
        request.Name = new string('A', 201);

        var act = async () => await service.CreateAsync(request);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Customer name cannot be longer than 200 characters.");
    }

    [Fact]
    public async Task Create_RejectsEmailLongerThan254Characters()
    {
        var service = CreateService();
        var request = ValidRequest();
        request.Email = new string('a', 243) + "@example.com";

        var act = async () => await service.CreateAsync(request);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Customer email cannot be longer than 254 characters.");
    }

    [Fact]
    public async Task Create_RejectsPhoneLongerThan50Characters()
    {
        var service = CreateService();
        var request = ValidRequest();
        request.PhoneNumber = new string('1', 51);

        var act = async () => await service.CreateAsync(request);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Customer phone number cannot be longer than 50 characters.");
    }

    [Fact]
    public async Task Update_RejectsAddressLongerThan500CharactersBeforeDatabaseRead()
    {
        var service = CreateService();
        var request = new UpdateCustomerRequest
        {
            Name = "Nordic Retail AB",
            Email = "contact@nordic.example",
            PhoneNumber = "123456",
            Address = new string('A', 501)
        };

        var act = async () => await service.UpdateAsync(Guid.NewGuid(), request);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Customer address cannot be longer than 500 characters.");
        _repository.Verify(x => x.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task Update_WithoutChangedFields_DoesNotLogContactValues()
    {
        var service = CreateService();
        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            Name = "Nordic Retail AB",
            Email = "contact@nordic.example",
            PhoneNumber = "123",
            Address = "Address"
        };
        _repository.Setup(x => x.GetByIdAsync(customer.Id)).ReturnsAsync(customer);
        string? changes = null;
        _auditLog.Setup(x => x.LogAsync("Customer", "Updated", "System", It.IsAny<string>()))
            .Callback<string, string, string, string>((_, _, _, value) => changes = value)
            .Returns(Task.CompletedTask);

        await service.UpdateAsync(customer.Id, new UpdateCustomerRequest
        {
            Name = customer.Name,
            Email = customer.Email,
            PhoneNumber = customer.PhoneNumber,
            Address = customer.Address
        });

        changes.Should().Be($"Updated customer {customer.Id}. Changed fields: None.");
    }
}
