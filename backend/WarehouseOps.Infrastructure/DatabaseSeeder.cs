using Microsoft.EntityFrameworkCore;
using WarehouseOps.Domain;

namespace WarehouseOps.Infrastructure;

public static class DatabaseSeeder
{
    public static async Task SeedDemoDataAsync(ApplicationDbContext dbContext)
    {
        if (await dbContext.Products.AnyAsync())
        {
            return;
        }

        var now = DateTime.UtcNow;

        var asusZephyrusId = Guid.Parse("10000000-0000-0000-0000-000000000001");
        var macBookProId = Guid.Parse("10000000-0000-0000-0000-000000000002");
        var thinkPadId = Guid.Parse("10000000-0000-0000-0000-000000000003");
        var samsungMonitorId = Guid.Parse("10000000-0000-0000-0000-000000000004");
        var sonyHeadphonesId = Guid.Parse("10000000-0000-0000-0000-000000000005");
        var logitechMouseId = Guid.Parse("10000000-0000-0000-0000-000000000006");
        var ubiquitiRouterId = Guid.Parse("10000000-0000-0000-0000-000000000007");
        var synologyNasId = Guid.Parse("10000000-0000-0000-0000-000000000008");

        var products = new List<Product>
        {
            new()
            {
                Id = asusZephyrusId,
                Name = "ASUS ROG Zephyrus G14",
                Sku = "LAP-ASUS-ZEP-G14",
                Category = "Laptops",
                Description = "High performance gaming laptop for premium retail channels.",
                Price = 22990,
                CreatedAt = now.AddDays(-10)
            },
            new()
            {
                Id = macBookProId,
                Name = "Apple MacBook Pro 14 M4",
                Sku = "LAP-APPLE-MBP14-M4",
                Category = "Laptops",
                Description = "Professional laptop for business and creative customers.",
                Price = 28990,
                CreatedAt = now.AddDays(-10)
            },
            new()
            {
                Id = thinkPadId,
                Name = "Lenovo ThinkPad X1 Carbon Gen 12",
                Sku = "LAP-LENOVO-X1C-G12",
                Category = "Laptops",
                Description = "Business laptop for enterprise purchasing departments.",
                Price = 24990,
                CreatedAt = now.AddDays(-9)
            },
            new()
            {
                Id = samsungMonitorId,
                Name = "Samsung Odyssey G7 32 Monitor",
                Sku = "MON-SAMSUNG-ODG7-32",
                Category = "Monitors",
                Description = "Curved gaming monitor for electronics retail stock.",
                Price = 6490,
                CreatedAt = now.AddDays(-9)
            },
            new()
            {
                Id = sonyHeadphonesId,
                Name = "Sony WH-1000XM5 Headphones",
                Sku = "ACC-SONY-WH1000XM5",
                Category = "Accessories",
                Description = "Noise cancelling wireless headphones.",
                Price = 3990,
                CreatedAt = now.AddDays(-8)
            },
            new()
            {
                Id = logitechMouseId,
                Name = "Logitech MX Master 3S",
                Sku = "ACC-LOGI-MX3S",
                Category = "Accessories",
                Description = "Wireless productivity mouse for office and retail customers.",
                Price = 1190,
                CreatedAt = now.AddDays(-8)
            },
            new()
            {
                Id = ubiquitiRouterId,
                Name = "Ubiquiti UniFi Dream Machine Pro",
                Sku = "NET-UBNT-UDM-PRO",
                Category = "Networking",
                Description = "Network gateway for small business and prosumer setups.",
                Price = 5490,
                CreatedAt = now.AddDays(-7)
            },
            new()
            {
                Id = synologyNasId,
                Name = "Synology DS923+ NAS",
                Sku = "STO-SYNO-DS923P",
                Category = "Storage",
                Description = "NAS unit for small business storage and backup.",
                Price = 6990,
                CreatedAt = now.AddDays(-7)
            }
        };

        var inventoryItems = new List<InventoryItem>
        {
            new()
            {
                Id = Guid.Parse("20000000-0000-0000-0000-000000000001"),
                ProductId = asusZephyrusId,
                QuantityInStock = 18,
                MinimumStockLevel = 10,
                CreatedAt = now.AddDays(-6)
            },
            new()
            {
                Id = Guid.Parse("20000000-0000-0000-0000-000000000002"),
                ProductId = macBookProId,
                QuantityInStock = 7,
                MinimumStockLevel = 12,
                CreatedAt = now.AddDays(-6)
            },
            new()
            {
                Id = Guid.Parse("20000000-0000-0000-0000-000000000003"),
                ProductId = thinkPadId,
                QuantityInStock = 24,
                MinimumStockLevel = 8,
                CreatedAt = now.AddDays(-6)
            },
            new()
            {
                Id = Guid.Parse("20000000-0000-0000-0000-000000000004"),
                ProductId = samsungMonitorId,
                QuantityInStock = 30,
                MinimumStockLevel = 15,
                CreatedAt = now.AddDays(-6)
            },
            new()
            {
                Id = Guid.Parse("20000000-0000-0000-0000-000000000005"),
                ProductId = sonyHeadphonesId,
                QuantityInStock = 44,
                MinimumStockLevel = 20,
                CreatedAt = now.AddDays(-5)
            },
            new()
            {
                Id = Guid.Parse("20000000-0000-0000-0000-000000000006"),
                ProductId = logitechMouseId,
                QuantityInStock = 65,
                MinimumStockLevel = 25,
                CreatedAt = now.AddDays(-5)
            },
            new()
            {
                Id = Guid.Parse("20000000-0000-0000-0000-000000000007"),
                ProductId = ubiquitiRouterId,
                QuantityInStock = 5,
                MinimumStockLevel = 10,
                CreatedAt = now.AddDays(-5)
            },
            new()
            {
                Id = Guid.Parse("20000000-0000-0000-0000-000000000008"),
                ProductId = synologyNasId,
                QuantityInStock = 11,
                MinimumStockLevel = 6,
                CreatedAt = now.AddDays(-5)
            }
        };

        var elgigantenId = Guid.Parse("30000000-0000-0000-0000-000000000001");
        var powerId = Guid.Parse("30000000-0000-0000-0000-000000000002");
        var inetId = Guid.Parse("30000000-0000-0000-0000-000000000003");
        var dustinId = Guid.Parse("30000000-0000-0000-0000-000000000004");

        var customers = new List<Customer>
        {
            new()
            {
                Id = elgigantenId,
                Name = "Elgiganten Sverige AB",
                Email = "purchasing@elgiganten.example",
                PhoneNumber = "+46 10 100 10 10",
                Address = "Retail procurement, Stockholm",
                CreatedAt = now.AddDays(-4)
            },
            new()
            {
                Id = powerId,
                Name = "Power Sverige AB",
                Email = "procurement@power.example",
                PhoneNumber = "+46 10 200 20 20",
                Address = "Nordic purchasing office, Stockholm",
                CreatedAt = now.AddDays(-4)
            },
            new()
            {
                Id = inetId,
                Name = "Inet AB",
                Email = "b2b@inet.example",
                PhoneNumber = "+46 31 300 30 30",
                Address = "B2B purchasing, Göteborg",
                CreatedAt = now.AddDays(-3)
            },
            new()
            {
                Id = dustinId,
                Name = "Dustin Sverige AB",
                Email = "orders@dustin.example",
                PhoneNumber = "+46 8 400 40 40",
                Address = "Corporate purchasing, Nacka",
                CreatedAt = now.AddDays(-3)
            }
        };

        var orderOneId = Guid.Parse("40000000-0000-0000-0000-000000000001");
        var orderTwoId = Guid.Parse("40000000-0000-0000-0000-000000000002");
        var orderThreeId = Guid.Parse("40000000-0000-0000-0000-000000000003");
        var orderFourId = Guid.Parse("40000000-0000-0000-0000-000000000004");

        var orders = new List<Order>
        {
            new()
            {
                Id = orderOneId,
                CustomerId = elgigantenId,
                Status = OrderStatus.Packed,
                TotalAmount = 121910,
                CreatedAt = now.AddDays(-2)
            },
            new()
            {
                Id = orderTwoId,
                CustomerId = powerId,
                Status = OrderStatus.Processing,
                TotalAmount = 83950,
                CreatedAt = now.AddDays(-2)
            },
            new()
            {
                Id = orderThreeId,
                CustomerId = inetId,
                Status = OrderStatus.Completed,
                TotalAmount = 73880,
                CreatedAt = now.AddDays(-1)
            },
            new()
            {
                Id = orderFourId,
                CustomerId = dustinId,
                Status = OrderStatus.Pending,
                TotalAmount = 32940,
                CreatedAt = now.AddHours(-12)
            }
        };

        var orderItems = new List<OrderItem>
        {
            new()
            {
                Id = Guid.Parse("50000000-0000-0000-0000-000000000001"),
                OrderId = orderOneId,
                ProductId = asusZephyrusId,
                Quantity = 3,
                UnitPrice = 22990,
                CreatedAt = now.AddDays(-2)
            },
            new()
            {
                Id = Guid.Parse("50000000-0000-0000-0000-000000000002"),
                OrderId = orderOneId,
                ProductId = samsungMonitorId,
                Quantity = 8,
                UnitPrice = 6490,
                CreatedAt = now.AddDays(-2)
            },
            new()
            {
                Id = Guid.Parse("50000000-0000-0000-0000-000000000003"),
                OrderId = orderTwoId,
                ProductId = thinkPadId,
                Quantity = 2,
                UnitPrice = 24990,
                CreatedAt = now.AddDays(-2)
            },
            new()
            {
                Id = Guid.Parse("50000000-0000-0000-0000-000000000004"),
                OrderId = orderTwoId,
                ProductId = ubiquitiRouterId,
                Quantity = 4,
                UnitPrice = 5490,
                CreatedAt = now.AddDays(-2)
            },
            new()
            {
                Id = Guid.Parse("50000000-0000-0000-0000-000000000005"),
                OrderId = orderThreeId,
                ProductId = macBookProId,
                Quantity = 2,
                UnitPrice = 28990,
                CreatedAt = now.AddDays(-1)
            },
            new()
            {
                Id = Guid.Parse("50000000-0000-0000-0000-000000000006"),
                OrderId = orderThreeId,
                ProductId = sonyHeadphonesId,
                Quantity = 4,
                UnitPrice = 3990,
                CreatedAt = now.AddDays(-1)
            },
            new()
            {
                Id = Guid.Parse("50000000-0000-0000-0000-000000000007"),
                OrderId = orderFourId,
                ProductId = synologyNasId,
                Quantity = 2,
                UnitPrice = 6990,
                CreatedAt = now.AddHours(-12)
            },
            new()
            {
                Id = Guid.Parse("50000000-0000-0000-0000-000000000008"),
                OrderId = orderFourId,
                ProductId = logitechMouseId,
                Quantity = 10,
                UnitPrice = 1190,
                CreatedAt = now.AddHours(-12)
            }
        };

        var shipments = new List<Shipment>
        {
            new()
            {
                Id = Guid.Parse("60000000-0000-0000-0000-000000000001"),
                OrderId = orderOneId,
                Status = ShipmentStatus.Packed,
                TrackingNumber = "WH-TECH-1001",
                CreatedAt = now.AddDays(-1)
            },
            new()
            {
                Id = Guid.Parse("60000000-0000-0000-0000-000000000002"),
                OrderId = orderThreeId,
                Status = ShipmentStatus.Delivered,
                TrackingNumber = "WH-TECH-1002",
                ShippedDate = now.AddDays(-1),
                DeliveredDate = now.AddHours(-6),
                CreatedAt = now.AddDays(-1)
            }
        };

        var incidents = new List<Incident>
        {
            new()
            {
                Id = Guid.Parse("70000000-0000-0000-0000-000000000001"),
                Title = "Low MacBook Pro stock",
                Description = "MacBook Pro 14 stock is below the minimum level after recent B2B orders.",
                Severity = IncidentSeverity.High,
                RelatedEntityType = IncidentRelatedEntityType.Inventory,
                RelatedEntityId = macBookProId.ToString(),
                Status = IncidentStatus.Open,
                ResolutionNotes = "",
                CreatedAt = now.AddHours(-10)
            },
            new()
            {
                Id = Guid.Parse("70000000-0000-0000-0000-000000000002"),
                Title = "Delayed Ubiquiti supplier batch",
                Description = "Incoming Ubiquiti shipment from supplier is delayed and may affect Power order handling.",
                Severity = IncidentSeverity.Medium,
                RelatedEntityType = IncidentRelatedEntityType.Product,
                RelatedEntityId = ubiquitiRouterId.ToString(),
                Status = IncidentStatus.InProgress,
                ResolutionNotes = "",
                CreatedAt = now.AddHours(-8)
            },
            new()
            {
                Id = Guid.Parse("70000000-0000-0000-0000-000000000003"),
                Title = "Delivered shipment confirmed",
                Description = "Inet shipment was confirmed as delivered after tracking verification.",
                Severity = IncidentSeverity.Low,
                RelatedEntityType = IncidentRelatedEntityType.Shipment,
                RelatedEntityId = "WH-TECH-1002",
                Status = IncidentStatus.Closed,
                ResolutionNotes = "Shipment confirmed with customer and carrier.",
                ClosedAt = now.AddHours(-4),
                CreatedAt = now.AddHours(-7),
                UpdatedAt = now.AddHours(-4)
            }
        };

        var auditLogs = new List<AuditLog>
        {
            new()
            {
                Id = Guid.Parse("80000000-0000-0000-0000-000000000001"),
                EntityName = "Product",
                Action = "Created",
                PerformedBy = "System seed",
                PerformedAt = now.AddDays(-2),
                Changes = "Seeded demo product catalog for technology import operations.",
                CreatedAt = now.AddDays(-2)
            },
            new()
            {
                Id = Guid.Parse("80000000-0000-0000-0000-000000000002"),
                EntityName = "Inventory",
                Action = "Created",
                PerformedBy = "System seed",
                PerformedAt = now.AddDays(-2),
                Changes = "Seeded initial warehouse stock levels for imported technology products.",
                CreatedAt = now.AddDays(-2)
            },
            new()
            {
                Id = Guid.Parse("80000000-0000-0000-0000-000000000003"),
                EntityName = "Order",
                Action = "Created",
                PerformedBy = "System seed",
                PerformedAt = now.AddDays(-1),
                Changes = "Seeded B2B customer orders for Elgiganten, Power, Inet and Dustin.",
                CreatedAt = now.AddDays(-1)
            },
            new()
            {
                Id = Guid.Parse("80000000-0000-0000-0000-000000000004"),
                EntityName = "Incident",
                Action = "Created",
                PerformedBy = "System seed",
                PerformedAt = now.AddHours(-8),
                Changes = "Seeded operational incidents for stock and delivery follow-up.",
                CreatedAt = now.AddHours(-8)
            }
        };

        await dbContext.Products.AddRangeAsync(products);
        await dbContext.InventoryItems.AddRangeAsync(inventoryItems);
        await dbContext.Customers.AddRangeAsync(customers);
        await dbContext.Orders.AddRangeAsync(orders);
        await dbContext.OrderItems.AddRangeAsync(orderItems);
        await dbContext.Shipments.AddRangeAsync(shipments);
        await dbContext.Incidents.AddRangeAsync(incidents);
        await dbContext.AuditLogs.AddRangeAsync(auditLogs);

        await dbContext.SaveChangesAsync();
    }
}
