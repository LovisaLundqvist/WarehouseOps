using WarehouseOps.Domain;

namespace WarehouseOps.Infrastructure;

// The historical demo orders were originally seeded with incorrect totals.
// Corrections are deliberately restricted to the exact original seed rows.
internal static class DemoOrderIntegrity
{
    private sealed record SeedLine(Guid Id, Guid ProductId, int Quantity, decimal UnitPrice);

    private sealed record LegacySeedOrder(Guid Id, decimal IncorrectTotal, SeedLine[] Lines);

    private static readonly LegacySeedOrder[] LegacyOrders =
    [
        new(
            Guid.Parse("40000000-0000-0000-0000-000000000001"),
            121910m,
            [
                new(Guid.Parse("50000000-0000-0000-0000-000000000001"), Guid.Parse("10000000-0000-0000-0000-000000000001"), 3, 22990m),
                new(Guid.Parse("50000000-0000-0000-0000-000000000002"), Guid.Parse("10000000-0000-0000-0000-000000000004"), 8, 6490m)
            ]),
        new(
            Guid.Parse("40000000-0000-0000-0000-000000000002"),
            83950m,
            [
                new(Guid.Parse("50000000-0000-0000-0000-000000000003"), Guid.Parse("10000000-0000-0000-0000-000000000003"), 2, 24990m),
                new(Guid.Parse("50000000-0000-0000-0000-000000000004"), Guid.Parse("10000000-0000-0000-0000-000000000007"), 4, 5490m)
            ]),
        new(
            Guid.Parse("40000000-0000-0000-0000-000000000003"),
            73880m,
            [
                new(Guid.Parse("50000000-0000-0000-0000-000000000005"), Guid.Parse("10000000-0000-0000-0000-000000000002"), 2, 28990m),
                new(Guid.Parse("50000000-0000-0000-0000-000000000006"), Guid.Parse("10000000-0000-0000-0000-000000000005"), 4, 3990m)
            ]),
        new(
            Guid.Parse("40000000-0000-0000-0000-000000000004"),
            32940m,
            [
                new(Guid.Parse("50000000-0000-0000-0000-000000000007"), Guid.Parse("10000000-0000-0000-0000-000000000008"), 2, 6990m),
                new(Guid.Parse("50000000-0000-0000-0000-000000000008"), Guid.Parse("10000000-0000-0000-0000-000000000006"), 10, 1190m)
            ])
    ];

    internal static Guid[] LegacyOrderIds => LegacyOrders.Select(spec => spec.Id).ToArray();

    internal static decimal CalculateTotal(IEnumerable<OrderItem> lines)
    {
        ArgumentNullException.ThrowIfNull(lines);

        decimal sum = 0m;
        var hasLines = false;

        foreach (var line in lines)
        {
            if (line.Quantity <= 0 || line.UnitPrice < 0m)
            {
                throw new InvalidOperationException("Seed order lines must have positive quantities and nonnegative prices.");
            }

            hasLines = true;
            sum = checked(sum + checked(line.UnitPrice * line.Quantity));
        }

        if (!hasLines)
        {
            throw new InvalidOperationException("A seeded order must contain at least one order line.");
        }

        return sum;
    }

    internal static bool TryRepairLegacyOrder(Order order)
    {
        var specification = LegacyOrders.FirstOrDefault(spec => spec.Id == order.Id);
        if (specification is null || order.TotalAmount != specification.IncorrectTotal ||
            order.OrderItems.Count != specification.Lines.Length)
        {
            return false;
        }

        // Check both line identity and contents. Never overwrite a user-edited order.
        foreach (var expected in specification.Lines)
        {
            if (!order.OrderItems.Any(line =>
                    line.Id == expected.Id &&
                    line.ProductId == expected.ProductId &&
                    line.Quantity == expected.Quantity &&
                    line.UnitPrice == expected.UnitPrice))
            {
                return false;
            }
        }

        order.TotalAmount = CalculateTotal(order.OrderItems);
        order.UpdatedAt = DateTime.UtcNow;
        return true;
    }
}
