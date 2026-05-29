using WarehouseOps.Application.Dtos;

namespace WarehouseOps.Application.Interfaces;

public interface IAuditLogService
{
    Task<List<AuditLogDto>> GetAllAsync();

    Task<AuditLogDto?> GetByIdAsync(Guid id);

    Task LogAsync(string entityName, string action, string performedBy, string changes);
}
