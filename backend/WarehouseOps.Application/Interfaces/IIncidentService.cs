using WarehouseOps.Application.Dtos;

namespace WarehouseOps.Application.Interfaces;

public interface IIncidentService
{
    Task<List<IncidentDto>> GetAllAsync(string? status);

    Task<IncidentDto?> GetByIdAsync(Guid id);

    Task<IncidentDto> CreateAsync(CreateIncidentRequest request);

    Task<IncidentDto?> UpdateStatusAsync(Guid id, UpdateIncidentStatusRequest request);

    Task<IncidentDto?> ResolveAsync(Guid id, ResolveIncidentRequest request);
}
