using WarehouseOps.Application.Dtos;

namespace WarehouseOps.Application.Interfaces;

public interface IDashboardService
{
    Task<DashboardSummaryDto> GetSummaryAsync();
}
