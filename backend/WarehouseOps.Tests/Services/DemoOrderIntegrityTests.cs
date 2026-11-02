using FluentAssertions;
using WarehouseOps.Domain;
using WarehouseOps.Infrastructure;

namespace WarehouseOps.Tests.Services;

public class DemoOrderIntegrityTests
{
    [Theory]
    [InlineData(1, 120890)]
    [InlineData(2, 71940)]
    [InlineData(3, 73940)]
    [InlineData(4, 25880)]
    public void CalculateTotal_UsesActualOrderLines(int orderNumber, int expectedTotal)
    {
        var order = CreateOriginalOrder(orderNumber);

        var actual = DemoOrderIntegrity.CalculateTotal(order.OrderItems);

        actual.Should().Be((decimal)expectedTotal);
    }

    [Theory]
    [InlineData(1, 120890)]
    [InlineData(2, 71940)]
    [InlineData(3, 73940)]
    [InlineData(4, 25880)]
    public void TryRepairLegacyOrder_CorrectsExactOriginalDemoOrder(
        int orderNumber,
        int expectedTotal)
    {
        var order = CreateOriginalOrder(orderNumber);

        var wasCorrected = DemoOrderIntegrity.TryRepairLegacyOrder(order);

        wasCorrected.Should().BeTrue();
        order.TotalAmount.Should().Be((decimal)expectedTotal);
        order.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void TryRepairLegacyOrder_DoesNotRepeatCorrection()
    {
        var order = CreateOriginalOrder(1);
        DemoOrderIntegrity.TryRepairLegacyOrder(order).Should().BeTrue();
        var updatedAt = order.UpdatedAt;

        DemoOrderIntegrity.TryRepairLegacyOrder(order).Should().BeFalse();
        order.TotalAmount.Should().Be(120890m);
        order.UpdatedAt.Should().Be(updatedAt);
    }

    [Fact]
    public void TryRepairLegacyOrder_DoesNotChangeEditedQuantity()
    {
        var order = CreateOriginalOrder(2);
        order.OrderItems[0].Quantity++;

        DemoOrderIntegrity.TryRepairLegacyOrder(order).Should().BeFalse();
        order.TotalAmount.Should().Be(83950m);
        order.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public void TryRepairLegacyOrder_DoesNotChangeEditedPrice()
    {
        var order = CreateOriginalOrder(3);
        order.OrderItems[0].UnitPrice++;

        DemoOrderIntegrity.TryRepairLegacyOrder(order).Should().BeFalse();
        order.TotalAmount.Should().Be(73880m);
    }

    [Fact]
    public void TryRepairLegacyOrder_DoesNotChangeUnrelatedOrders()
    {
        var order = CreateOriginalOrder(4);
        order.Id = Guid.NewGuid();

        DemoOrderIntegrity.TryRepairLegacyOrder(order).Should().BeFalse();
        order.TotalAmount.Should().Be(32940m);
    }

    [Fact]
    public void TryRepairLegacyOrder_DoesNotChangeOrderWithMissingLine()
    {
        var order = CreateOriginalOrder(1);
        order.OrderItems.RemoveAt(0);

        DemoOrderIntegrity.TryRepairLegacyOrder(order).Should().BeFalse();
        order.TotalAmount.Should().Be(121910m);
    }

    [Fact]
    public void CalculateTotal_RejectsEmptyOrderLines()
    {
        var action = () => DemoOrderIntegrity.CalculateTotal([]);

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("A seeded order must contain at least one order line.");
    }

    [Fact]
    public void CalculateTotal_RejectsInvalidQuantity()
    {
        var order = CreateOriginalOrder(1);
        order.OrderItems[0].Quantity = 0;
        var action = () => DemoOrderIntegrity.CalculateTotal(order.OrderItems);

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("Seed order lines must have positive quantities and nonnegative prices.");
    }

    private static Order CreateOriginalOrder(int number)
    {
        var oldTotal = number switch
        {
            1 => 121910m,
            2 => 83950m,
            3 => 73880m,
            4 => 32940m,
            _ => throw new ArgumentOutOfRangeException(nameof(number))
        };

        var specifications = number switch
        {
            1 => new[] { (Item: 1, Product: 1, Quantity: 3, Price: 22990m),
                         (Item: 2, Product: 4, Quantity: 8, Price: 6490m) },
            2 => new[] { (Item: 3, Product: 3, Quantity: 2, Price: 24990m),
                         (Item: 4, Product: 7, Quantity: 4, Price: 5490m) },
            3 => new[] { (Item: 5, Product: 2, Quantity: 2, Price: 28990m),
                         (Item: 6, Product: 5, Quantity: 4, Price: 3990m) },
            4 => new[] { (Item: 7, Product: 8, Quantity: 2, Price: 6990m),
                         (Item: 8, Product: 6, Quantity: 10, Price: 1190m) },
            _ => throw new ArgumentOutOfRangeException(nameof(number))
        };

        var orderId = Guid.Parse($"40000000-0000-0000-0000-{number:D12}");

        return new Order
        {
            Id = orderId,
            TotalAmount = oldTotal,
            Status = OrderStatus.Pending,
            OrderItems = specifications.Select(spec => new OrderItem
            {
                Id = Guid.Parse($"50000000-0000-0000-0000-{spec.Item:D12}"),
                OrderId = orderId,
                ProductId = Guid.Parse($"10000000-0000-0000-0000-{spec.Product:D12}"),
                Quantity = spec.Quantity,
                UnitPrice = spec.Price
            }).ToList()
        };
    }
}
