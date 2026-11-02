using WarehouseOps.Application.Dtos;
using WarehouseOps.Application.Interfaces;
using WarehouseOps.Application.Validation;
using WarehouseOps.Domain;

namespace WarehouseOps.Application.Services;

public class ShipmentService : IShipmentService
{
    private readonly IShipmentRepository _shipmentRepository;
    private readonly IAuditLogService _auditLogService;

    public ShipmentService(IShipmentRepository shipmentRepository, IAuditLogService auditLogService)
    {
        _shipmentRepository = shipmentRepository;
        _auditLogService = auditLogService;
    }

    public async Task<List<ShipmentDto>> GetAllAsync()
    {
        var shipments = await _shipmentRepository.GetAllAsync();

        return shipments
            .Select(MapToDto)
            .ToList();
    }

    public async Task<ShipmentDto?> GetByIdAsync(Guid id)
    {
        var shipment = await _shipmentRepository.GetByIdAsync(id);

        if (shipment is null)
        {
            return null;
        }

        return MapToDto(shipment);
    }

    public async Task<ShipmentDto> CreateAsync(CreateShipmentRequest request)
    {
        ValidateCreateRequest(request);

        var order = await _shipmentRepository.GetOrderByIdAsync(request.OrderId);

        if (order is null)
        {
            throw new InvalidOperationException("Order does not exist.");
        }

        if (order.Status == OrderStatus.Cancelled)
        {
            throw new InvalidOperationException("A shipment cannot be created for a cancelled order.");
        }

        if (order.Status != OrderStatus.Packed)
        {
            throw new InvalidOperationException("An order must be packed before a shipment can be created.");
        }

        var shipmentExists = await _shipmentRepository.ShipmentExistsForOrderAsync(request.OrderId);

        if (shipmentExists)
        {
            throw new InvalidOperationException("This order already has a shipment.");
        }

        var shipment = new Shipment
        {
            Id = Guid.NewGuid(),
            OrderId = request.OrderId,
            Status = ShipmentStatus.Pending,
            TrackingNumber = request.TrackingNumber.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        await _shipmentRepository.AddAsync(shipment);

        await _shipmentRepository.SaveChangesAsync();

        var createdShipment = await _shipmentRepository.GetByIdAsync(shipment.Id);

        if (createdShipment is null)
        {
            throw new InvalidOperationException("Shipment could not be loaded after creation.");
        }

        await _auditLogService.LogAsync(
            "Shipment",
            "Created",
            "System",
            $"Created shipment {createdShipment.Id} for order {createdShipment.OrderId}.");

        return MapToDto(createdShipment);
    }

    public async Task<ShipmentDto?> UpdateStatusAsync(Guid id, UpdateShipmentStatusRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var newStatus = ParseStatus(request.Status);

        var shipment = await _shipmentRepository.GetByIdAsync(id);

        if (shipment is null)
        {
            return null;
        }

        if (!IsValidStatusChange(shipment.Status, newStatus))
        {
            throw new InvalidOperationException($"Shipment status cannot be changed from {shipment.Status} to {newStatus}.");
        }

        if (shipment.Order is null)
        {
            throw new InvalidOperationException("Shipment does not have a valid order.");
        }

        // Idempotent requests must not overwrite shipping or delivery timestamps.
        if (shipment.Status == newStatus)
        {
            return MapToDto(shipment);
        }

        var order = shipment.Order;

        if (order.Status == OrderStatus.Cancelled)
        {
            throw new InvalidOperationException("A cancelled order cannot have an active shipment.");
        }

        // A delayed shipment can be cancelled only if it has never been shipped.
        if (newStatus == ShipmentStatus.Cancelled && shipment.ShippedDate.HasValue)
        {
            throw new InvalidOperationException("A shipped shipment cannot be cancelled.");
        }

        if ((newStatus is ShipmentStatus.Packed or ShipmentStatus.Cancelled) &&
            order.Status != OrderStatus.Packed)
        {
            throw new InvalidOperationException(
                "A shipment can only be packed or cancelled while its order is packed.");
        }

        if (newStatus == ShipmentStatus.Shipped &&
            order.Status is not (OrderStatus.Packed or OrderStatus.Shipped))
        {
            throw new InvalidOperationException("Order must be packed or shipped before shipping its shipment.");
        }

        if (newStatus == ShipmentStatus.Delivered &&
            order.Status is not (OrderStatus.Shipped or OrderStatus.Packed))
        {
            throw new InvalidOperationException("Shipment cannot be delivered for an order that is not ready for delivery.");
        }

        var oldStatus = shipment.Status;
        var oldOrderStatus = order.Status;
        var now = DateTime.UtcNow;

        shipment.Status = newStatus;
        shipment.UpdatedAt = now;

        if (newStatus == ShipmentStatus.Shipped)
        {
            shipment.ShippedDate ??= now;
            order.Status = OrderStatus.Shipped;
            order.UpdatedAt = now;
        }

        if (newStatus == ShipmentStatus.Delivered)
        {
            shipment.DeliveredDate = now;
            order.Status = OrderStatus.Completed;
            order.UpdatedAt = now;
        }

        // Shipment and order changes are tracked in the same EF Core DbContext.
        // TransactionalShipmentService includes the audit entries in one transaction.
        await _shipmentRepository.SaveChangesAsync();

        await _auditLogService.LogAsync(
            "Shipment",
            "Updated",
            "System",
            $"Updated shipment {shipment.Id} status from {oldStatus} to {shipment.Status}.");

        if (oldOrderStatus != order.Status)
        {
            await _auditLogService.LogAsync(
                "Order",
                "Updated",
                "System",
                $"Updated order {order.Id} status from {oldOrderStatus} to {order.Status} after shipment {shipment.Id} status changed.");
        }

        return MapToDto(shipment);
    }

    private static void ValidateCreateRequest(CreateShipmentRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.OrderId == Guid.Empty)
        {
            throw new ArgumentException("Order id is required.");
        }

        if (string.IsNullOrWhiteSpace(request.TrackingNumber))
        {
            throw new ArgumentException("Tracking number is required.");
        }

        var trackingNumber = request.TrackingNumber.Trim();

        if (trackingNumber.Length > 100)
        {
            throw new ArgumentException("Tracking number cannot be longer than 100 characters.");
        }

        if (trackingNumber.Any(char.IsControl))
        {
            throw new ArgumentException("Tracking number cannot contain control characters.");
        }
    }

    private static ShipmentStatus ParseStatus(string status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            throw new ArgumentException("Shipment status is required.");
        }

        if (!EnumNameParser.TryParse(status, out ShipmentStatus shipmentStatus))
        {
            throw new ArgumentException("Shipment status is invalid. Valid statuses are Pending, Packed, Shipped, Delivered, Delayed and Cancelled.");
        }

        return shipmentStatus;
    }

    private static bool IsValidStatusChange(ShipmentStatus currentStatus, ShipmentStatus newStatus)
    {
        if (currentStatus == newStatus)
        {
            return true;
        }

        return currentStatus switch
        {
            ShipmentStatus.Pending => newStatus == ShipmentStatus.Packed || newStatus == ShipmentStatus.Cancelled || newStatus == ShipmentStatus.Delayed,
            ShipmentStatus.Packed => newStatus == ShipmentStatus.Shipped || newStatus == ShipmentStatus.Cancelled || newStatus == ShipmentStatus.Delayed,
            ShipmentStatus.Shipped => newStatus == ShipmentStatus.Delivered || newStatus == ShipmentStatus.Delayed,
            ShipmentStatus.Delayed => newStatus == ShipmentStatus.Shipped || newStatus == ShipmentStatus.Cancelled,
            ShipmentStatus.Delivered => false,
            ShipmentStatus.Cancelled => false,
            _ => false
        };
    }

    private static ShipmentDto MapToDto(Shipment shipment)
    {
        return new ShipmentDto
        {
            Id = shipment.Id,
            OrderId = shipment.OrderId,
            OrderStatus = shipment.Order?.Status.ToString() ?? string.Empty,
            CustomerName = shipment.Order?.Customer?.Name ?? string.Empty,
            Status = shipment.Status.ToString(),
            TrackingNumber = shipment.TrackingNumber,
            ShippedDate = shipment.ShippedDate,
            DeliveredDate = shipment.DeliveredDate,
            CreatedAt = shipment.CreatedAt,
            UpdatedAt = shipment.UpdatedAt
        };
    }
}
