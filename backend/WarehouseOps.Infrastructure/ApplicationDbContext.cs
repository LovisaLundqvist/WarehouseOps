using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WarehouseOps.Domain;
using WarehouseOps.Infrastructure.Identity;

namespace WarehouseOps.Infrastructure;

public class ApplicationDbContext
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Product> Products => Set<Product>();

    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();

    public DbSet<Customer> Customers => Set<Customer>();

    public DbSet<Order> Orders => Set<Order>();

    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    public DbSet<Shipment> Shipments => Set<Shipment>();

    public DbSet<Incident> Incidents => Set<Incident>();

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Identity maps its user, role, claim, token and login tables here.
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ApplicationUser>()
            .Property(user => user.DisplayName)
            .HasMaxLength(200)
            .IsRequired();

        modelBuilder.Entity<Product>()
            .Property(product => product.Price)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Product>()
            .Property(product => product.Sku)
            .HasMaxLength(100);

        modelBuilder.Entity<Product>()
            .HasIndex(product => product.Sku)
            .IsUnique();

        modelBuilder.Entity<Customer>()
            .Property(customer => customer.Email)
            .HasMaxLength(254);

        modelBuilder.Entity<Customer>()
            .HasIndex(customer => customer.Email)
            .IsUnique();

        modelBuilder.Entity<InventoryItem>()
            .HasIndex(inventoryItem => inventoryItem.ProductId)
            .IsUnique();

        modelBuilder.Entity<InventoryItem>()
            .Property(inventoryItem => inventoryItem.RowVersion)
            .IsRowVersion();

        modelBuilder.Entity<Shipment>()
            .HasIndex(shipment => shipment.OrderId)
            .IsUnique();

        modelBuilder.Entity<Order>()
            .Property(order => order.TotalAmount)
            .HasPrecision(18, 2);

        modelBuilder.Entity<OrderItem>()
            .Property(orderItem => orderItem.UnitPrice)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Incident>()
            .Property(incident => incident.Severity)
            .HasDefaultValue(IncidentSeverity.Medium);

        modelBuilder.Entity<Incident>()
            .Property(incident => incident.RelatedEntityType)
            .HasDefaultValue(IncidentRelatedEntityType.General);

        modelBuilder.Entity<Incident>()
            .Property(incident => incident.RelatedEntityId)
            .HasMaxLength(100);
    }
}
