using WarehouseOps.Application.Dtos;
using WarehouseOps.Application.Interfaces;
using WarehouseOps.Domain;

namespace WarehouseOps.Application.Services;

public class ShipmentService : IShipmentService
{
    private readonly IShipmentRepository _shipmentRepository;

    public ShipmentService(IShipmentRepository shipmentRepository)
    {
        _shipmentRepository = shipmentRepository;
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

        return MapToDto(createdShipment);
    }

    public async Task<ShipmentDto?> UpdateStatusAsync(Guid id, UpdateShipmentStatusRequest request)
    {
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

        shipment.Status = newStatus;
        shipment.UpdatedAt = DateTime.UtcNow;

        if (newStatus == ShipmentStatus.Shipped)
        {
            shipment.ShippedDate = DateTime.UtcNow;
        }

        if (newStatus == ShipmentStatus.Delivered)
        {
            shipment.DeliveredDate = DateTime.UtcNow;
        }

        await _shipmentRepository.SaveChangesAsync();

        return MapToDto(shipment);
    }

    private static void ValidateCreateRequest(CreateShipmentRequest request)
    {
        if (request.OrderId == Guid.Empty)
        {
            throw new ArgumentException("Order id is required.");
        }

        if (string.IsNullOrWhiteSpace(request.TrackingNumber))
        {
            throw new ArgumentException("Tracking number is required.");
        }
    }

    private static ShipmentStatus ParseStatus(string status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            throw new ArgumentException("Shipment status is required.");
        }

        var parsed = Enum.TryParse<ShipmentStatus>(status.Trim(), true, out var shipmentStatus);

        if (!parsed)
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
