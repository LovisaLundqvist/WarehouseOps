using Microsoft.EntityFrameworkCore;
using WarehouseOps.Domain;

namespace WarehouseOps.Infrastructure;

public class ApplicationDbContext : DbContext
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
}