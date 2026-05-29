using Microsoft.AspNetCore.Mvc;
using WarehouseOps.Application.Dtos;
using WarehouseOps.Application.Interfaces;

namespace WarehouseOps.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuditLogsController : ControllerBase
{
    private readonly IAuditLogService _auditLogService;

    public AuditLogsController(IAuditLogService auditLogService)
    {
        _auditLogService = auditLogService;
    }

    [HttpGet]
    public async Task<ActionResult<List<AuditLogDto>>> GetAll()
    {
        var auditLogs = await _auditLogService.GetAllAsync();

        return Ok(auditLogs);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AuditLogDto>> GetById(Guid id)
    {
        var auditLog = await _auditLogService.GetByIdAsync(id);

        if (auditLog is null)
        {
            return NotFound("Audit log was not found.");
        }

        return Ok(auditLog);
    }
}
