using WarehouseOps.Application.Dtos;
using WarehouseOps.Application.Interfaces;
using WarehouseOps.Domain;

namespace WarehouseOps.Application.Services;

public class AuditLogService : IAuditLogService
{
    private readonly IAuditLogRepository _auditLogRepository;

    public AuditLogService(IAuditLogRepository auditLogRepository)
    {
        _auditLogRepository = auditLogRepository;
    }

    public async Task<List<AuditLogDto>> GetAllAsync()
    {
        var auditLogs = await _auditLogRepository.GetAllAsync();

        return auditLogs
            .Select(MapToDto)
            .ToList();
    }

    public async Task<AuditLogDto?> GetByIdAsync(Guid id)
    {
        var auditLog = await _auditLogRepository.GetByIdAsync(id);

        if (auditLog is null)
        {
            return null;
        }

        return MapToDto(auditLog);
    }

    public async Task LogAsync(string entityName, string action, string performedBy, string changes)
    {
        var now = DateTime.UtcNow;

        var auditLog = new AuditLog
        {
            Id = Guid.NewGuid(),
            EntityName = entityName.Trim(),
            Action = action.Trim(),
            PerformedBy = string.IsNullOrWhiteSpace(performedBy) ? "System" : performedBy.Trim(),
            PerformedAt = now,
            Changes = changes.Trim(),
            CreatedAt = now
        };

        await _auditLogRepository.AddAsync(auditLog);

        await _auditLogRepository.SaveChangesAsync();
    }

    private static AuditLogDto MapToDto(AuditLog auditLog)
    {
        return new AuditLogDto
        {
            Id = auditLog.Id,
            EntityName = auditLog.EntityName,
            Action = auditLog.Action,
            PerformedBy = auditLog.PerformedBy,
            PerformedAt = auditLog.PerformedAt,
            Changes = auditLog.Changes,
            CreatedAt = auditLog.CreatedAt,
            UpdatedAt = auditLog.UpdatedAt
        };
    }
}
