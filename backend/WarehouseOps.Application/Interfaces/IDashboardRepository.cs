using WarehouseOps.Application.Dtos;

namespace WarehouseOps.Application.Interfaces;

public interface IDashboardRepository
{
    Task<DashboardSummaryDto> GetSummaryAsync();
}
