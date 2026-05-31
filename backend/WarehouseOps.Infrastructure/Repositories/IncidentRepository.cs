using Microsoft.EntityFrameworkCore;
using WarehouseOps.Application.Interfaces;
using WarehouseOps.Domain;

namespace WarehouseOps.Infrastructure.Repositories;

public class IncidentRepository : IIncidentRepository
{
    private readonly ApplicationDbContext _context;

    public IncidentRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Incident>> GetAllAsync(IncidentStatus? status)
    {
        var query = _context.Incidents.AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(incident => incident.Status == status.Value);
        }

        return await query
            .OrderByDescending(incident => incident.CreatedAt)
            .ToListAsync();
    }

    public async Task<Incident?> GetByIdAsync(Guid id)
    {
        return await _context.Incidents
            .FirstOrDefaultAsync(incident => incident.Id == id);
    }

    public async Task AddAsync(Incident incident)
    {
        await _context.Incidents.AddAsync(incident);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
