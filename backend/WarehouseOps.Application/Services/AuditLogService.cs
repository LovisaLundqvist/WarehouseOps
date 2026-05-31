using WarehouseOps.Application.Dtos;
using WarehouseOps.Application.Interfaces;
using WarehouseOps.Domain;

namespace WarehouseOps.Application.Services;

public class AuditLogService : IAuditLogService
{
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly ICurrentUserService _currentUserService;

    public AuditLogService(
        IAuditLogRepository auditLogRepository,
        ICurrentUserService currentUserService)
    {
        _auditLogRepository = auditLogRepository;
        _currentUserService = currentUserService;
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
        var resolvedPerformedBy = ResolvePerformedBy(performedBy);

        var auditLog = new AuditLog
        {
            Id = Guid.NewGuid(),
            EntityName = entityName.Trim(),
            Action = action.Trim(),
            PerformedBy = resolvedPerformedBy,
            PerformedAt = now,
            Changes = changes.Trim(),
            CreatedAt = now
        };

        await _auditLogRepository.AddAsync(auditLog);

        await _auditLogRepository.SaveChangesAsync();
    }

    private string ResolvePerformedBy(string performedBy)
    {
        if (!string.IsNullOrWhiteSpace(performedBy) &&
            !performedBy.Equals("System", StringComparison.OrdinalIgnoreCase))
        {
            return performedBy.Trim();
        }

        return _currentUserService.GetCurrentUserDisplayName();
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
