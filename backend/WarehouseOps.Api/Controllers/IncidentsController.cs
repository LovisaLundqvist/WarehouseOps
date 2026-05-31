using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WarehouseOps.Api.Security;
using WarehouseOps.Application.Dtos;
using WarehouseOps.Application.Interfaces;

namespace WarehouseOps.Api.Controllers;

[Authorize(Roles = RoleNames.AllRoles)]
[ApiController]
[Route("api/[controller]")]
public class IncidentsController : ControllerBase
{
    private readonly IIncidentService _incidentService;

    public IncidentsController(IIncidentService incidentService)
    {
        _incidentService = incidentService;
    }

    [HttpGet]
    public async Task<ActionResult<List<IncidentDto>>> GetAll([FromQuery] string? status)
    {
        try
        {
            var incidents = await _incidentService.GetAllAsync(status);

            return Ok(incidents);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<IncidentDto>> GetById(Guid id)
    {
        var incident = await _incidentService.GetByIdAsync(id);

        if (incident is null)
        {
            return NotFound("Incident was not found.");
        }

        return Ok(incident);
    }

    [Authorize(Roles = RoleNames.AllRoles)]
    [HttpPost]
    public async Task<ActionResult<IncidentDto>> Create(CreateIncidentRequest request)
    {
        try
        {
            var incident = await _incidentService.CreateAsync(request);

            return CreatedAtAction(nameof(GetById), new { id = incident.Id }, incident);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    [Authorize(Roles = RoleNames.AdminOrWarehouseStaff)]
    [HttpPut("{id:guid}/status")]
    public async Task<ActionResult<IncidentDto>> UpdateStatus(Guid id, UpdateIncidentStatusRequest request)
    {
        try
        {
            var incident = await _incidentService.UpdateStatusAsync(id, request);

            if (incident is null)
            {
                return NotFound("Incident was not found.");
            }

            return Ok(incident);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(exception.Message);
        }
    }

    [Authorize(Roles = RoleNames.AdminOrWarehouseStaff)]
    [HttpPut("{id:guid}/resolve")]
    public async Task<ActionResult<IncidentDto>> Resolve(Guid id, ResolveIncidentRequest request)
    {
        try
        {
            var incident = await _incidentService.ResolveAsync(id, request);

            if (incident is null)
            {
                return NotFound("Incident was not found.");
            }

            return Ok(incident);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(exception.Message);
        }
    }
}
