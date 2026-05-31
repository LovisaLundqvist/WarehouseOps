using WarehouseOps.Domain;

namespace WarehouseOps.Application.Interfaces;

public interface IAuditLogRepository
{
    Task<List<AuditLog>> GetAllAsync();

    Task<AuditLog?> GetByIdAsync(Guid id);

    Task AddAsync(AuditLog auditLog);

    Task SaveChangesAsync();
}
