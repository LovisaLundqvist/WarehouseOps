using WarehouseOps.Application.Dtos;
using WarehouseOps.Application.Interfaces;
using WarehouseOps.Domain;

namespace WarehouseOps.Application.Services;

public class IncidentService : IIncidentService
{
    private readonly IIncidentRepository _incidentRepository;
    private readonly IAuditLogService _auditLogService;

    public IncidentService(IIncidentRepository incidentRepository, IAuditLogService auditLogService)
    {
        _incidentRepository = incidentRepository;
        _auditLogService = auditLogService;
    }

    public async Task<List<IncidentDto>> GetAllAsync(string? status)
    {
        IncidentStatus? incidentStatus = null;

        if (!string.IsNullOrWhiteSpace(status))
        {
            incidentStatus = ParseStatus(status);
        }

        var incidents = await _incidentRepository.GetAllAsync(incidentStatus);

        return incidents
            .Select(MapToDto)
            .ToList();
    }

    public async Task<IncidentDto?> GetByIdAsync(Guid id)
    {
        var incident = await _incidentRepository.GetByIdAsync(id);

        if (incident is null)
        {
            return null;
        }

        return MapToDto(incident);
    }

    public async Task<IncidentDto> CreateAsync(CreateIncidentRequest request)
    {
        ValidateCreateRequest(request);

        var incident = new Incident
        {
            Id = Guid.NewGuid(),
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            Severity = ParseSeverity(request.Severity),
            RelatedEntityType = ParseRelatedEntityType(request.RelatedEntityType),
            RelatedEntityId = request.RelatedEntityId.Trim(),
            Status = IncidentStatus.Open,
            CreatedAt = DateTime.UtcNow
        };

        await _incidentRepository.AddAsync(incident);

        await _incidentRepository.SaveChangesAsync();

        await _auditLogService.LogAsync(
            "Incident",
            "Created",
            "System",
            $"Created incident {incident.Id}. Title={incident.Title}, Severity={incident.Severity}, RelatedEntityType={incident.RelatedEntityType}, RelatedEntityId={incident.RelatedEntityId}.");

        return MapToDto(incident);
    }

    public async Task<IncidentDto?> UpdateStatusAsync(Guid id, UpdateIncidentStatusRequest request)
    {
        var newStatus = ParseStatus(request.Status);

        var incident = await _incidentRepository.GetByIdAsync(id);

        if (incident is null)
        {
            return null;
        }

        if (!IsValidStatusChange(incident.Status, newStatus))
        {
            throw new InvalidOperationException($"Incident status cannot be changed from {incident.Status} to {newStatus}.");
        }

        var oldStatus = incident.Status;

        incident.Status = newStatus;
        incident.UpdatedAt = DateTime.UtcNow;

        if (newStatus == IncidentStatus.Closed)
        {
            incident.ClosedAt = DateTime.UtcNow;
        }

        await _incidentRepository.SaveChangesAsync();

        var action = newStatus == IncidentStatus.Closed ? "Closed" : "Updated";

        await _auditLogService.LogAsync(
            "Incident",
            action,
            "System",
            $"Updated incident {incident.Id} status from {oldStatus} to {incident.Status}.");

        return MapToDto(incident);
    }

    public async Task<IncidentDto?> ResolveAsync(Guid id, ResolveIncidentRequest request)
    {
        ValidateResolveRequest(request);

        var incident = await _incidentRepository.GetByIdAsync(id);

        if (incident is null)
        {
            return null;
        }

        if (incident.Status == IncidentStatus.Closed)
        {
            throw new InvalidOperationException("Closed incidents cannot be resolved again.");
        }

        var oldStatus = incident.Status;

        incident.Status = IncidentStatus.Closed;
        incident.ResolutionNotes = request.ResolutionNotes.Trim();
        incident.ClosedAt = DateTime.UtcNow;
        incident.UpdatedAt = DateTime.UtcNow;

        await _incidentRepository.SaveChangesAsync();

        await _auditLogService.LogAsync(
            "Incident",
            "Closed",
            "System",
            $"Closed incident {incident.Id}. Old status={oldStatus}. Resolution notes={incident.ResolutionNotes}.");

        return MapToDto(incident);
    }

    private static void ValidateCreateRequest(CreateIncidentRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            throw new ArgumentException("Incident title is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Description))
        {
            throw new ArgumentException("Incident description is required.");
        }

        if (request.RelatedEntityId.Length > 100)
        {
            throw new ArgumentException("Related entity id cannot be longer than 100 characters.");
        }
    }

    private static void ValidateResolveRequest(ResolveIncidentRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ResolutionNotes))
        {
            throw new ArgumentException("Resolution notes are required.");
        }
    }

    private static IncidentStatus ParseStatus(string status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            throw new ArgumentException("Incident status is required.");
        }

        var parsed = Enum.TryParse<IncidentStatus>(status.Trim(), true, out var incidentStatus);

        if (!parsed)
        {
            throw new ArgumentException("Incident status is invalid. Valid statuses are Open, InProgress, Resolved and Closed.");
        }

        return incidentStatus;
    }

    private static IncidentSeverity ParseSeverity(string severity)
    {
        if (string.IsNullOrWhiteSpace(severity))
        {
            return IncidentSeverity.Medium;
        }

        var parsed = Enum.TryParse<IncidentSeverity>(severity.Trim(), true, out var incidentSeverity);

        if (!parsed)
        {
            throw new ArgumentException("Incident severity is invalid. Valid severities are Low, Medium, High and Critical.");
        }

        return incidentSeverity;
    }

    private static IncidentRelatedEntityType ParseRelatedEntityType(string relatedEntityType)
    {
        if (string.IsNullOrWhiteSpace(relatedEntityType))
        {
            return IncidentRelatedEntityType.General;
        }

        var parsed = Enum.TryParse<IncidentRelatedEntityType>(
            relatedEntityType.Trim(),
            true,
            out var parsedRelatedEntityType);

        if (!parsed)
        {
            throw new ArgumentException("Related entity type is invalid. Valid types are General, Product, Inventory, Customer, Order and Shipment.");
        }

        return parsedRelatedEntityType;
    }

    private static bool IsValidStatusChange(IncidentStatus currentStatus, IncidentStatus newStatus)
    {
        if (currentStatus == newStatus)
        {
            return true;
        }

        return currentStatus switch
        {
            IncidentStatus.Open => newStatus == IncidentStatus.InProgress || newStatus == IncidentStatus.Resolved || newStatus == IncidentStatus.Closed,
            IncidentStatus.InProgress => newStatus == IncidentStatus.Resolved || newStatus == IncidentStatus.Closed || newStatus == IncidentStatus.Open,
            IncidentStatus.Resolved => newStatus == IncidentStatus.Closed || newStatus == IncidentStatus.InProgress,
            IncidentStatus.Closed => false,
            _ => false
        };
    }

    private static IncidentDto MapToDto(Incident incident)
    {
        return new IncidentDto
        {
            Id = incident.Id,
            Title = incident.Title,
            Description = incident.Description,
            Severity = incident.Severity.ToString(),
            RelatedEntityType = incident.RelatedEntityType.ToString(),
            RelatedEntityId = incident.RelatedEntityId,
            Status = incident.Status.ToString(),
            ResolutionNotes = incident.ResolutionNotes,
            ClosedAt = incident.ClosedAt,
            CreatedAt = incident.CreatedAt,
            UpdatedAt = incident.UpdatedAt
        };
    }
}
