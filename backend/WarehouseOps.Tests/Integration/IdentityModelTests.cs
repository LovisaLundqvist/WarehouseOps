using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WarehouseOps.Domain;
using WarehouseOps.Infrastructure;
using WarehouseOps.Infrastructure.Identity;

namespace WarehouseOps.Tests.Integration;

public class IdentityModelTests
{
    [Fact]
    public void IdentityUserAndRoleTables_AreMapped()
    {
        using var context = CreateContext();

        context.Model.FindEntityType(typeof(ApplicationUser))!
            .GetTableName().Should().Be("AspNetUsers");

        context.Model.FindEntityType(typeof(IdentityRole<Guid>))!
            .GetTableName().Should().Be("AspNetRoles");
    }

    [Fact]
    public void IdentityUser_UsesGuidPrimaryKey_AndLimitsDisplayName()
    {
        using var context = CreateContext();

        var userType = context.Model.FindEntityType(typeof(ApplicationUser));
        userType.Should().NotBeNull();

        userType!.FindPrimaryKey()!.Properties.Single().ClrType
            .Should().Be(typeof(Guid));

        userType.FindProperty(nameof(ApplicationUser.DisplayName))!
            .GetMaxLength().Should().Be(200);
    }

    [Fact]
    public void ExistingBusinessEntities_RemainMapped()
    {
        using var context = CreateContext();

        var businessEntities = new[]
        {
            typeof(Product),
            typeof(InventoryItem),
            typeof(Customer),
            typeof(Order),
            typeof(OrderItem),
            typeof(Shipment),
            typeof(Incident),
            typeof(AuditLog)
        };

        foreach (var entity in businessEntities)
        {
            context.Model.FindEntityType(entity)
                .Should().NotBeNull($"existing entity {entity.Name} must remain mapped");
        }
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=WarehouseOpsIdentityModelTests;Trusted_Connection=True")
            .Options;

        // Accessing the EF model does not connect to SQL Server.
        return new ApplicationDbContext(options);
    }
}
