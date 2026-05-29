using WarehouseOps.Domain;

namespace WarehouseOps.Application.Interfaces;

public interface IIncidentRepository
{
    Task<List<Incident>> GetAllAsync(IncidentStatus? status);

    Task<Incident?> GetByIdAsync(Guid id);

    Task AddAsync(Incident incident);

    Task SaveChangesAsync();
}
