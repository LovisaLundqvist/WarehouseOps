using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WarehouseOps.Api.Security;
using WarehouseOps.Application.Dtos;
using WarehouseOps.Application.Interfaces;

namespace WarehouseOps.Api.Controllers;

[Authorize(Roles = RoleNames.AllRoles)]
[ApiController]
[Route("api/[controller]")]
public class ShipmentsController : ControllerBase
{
    private readonly IShipmentService _shipmentService;

    public ShipmentsController(IShipmentService shipmentService)
    {
        _shipmentService = shipmentService;
    }

    [HttpGet]
    public async Task<ActionResult<List<ShipmentDto>>> GetAll()
    {
        var shipments = await _shipmentService.GetAllAsync();

        return Ok(shipments);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ShipmentDto>> GetById(Guid id)
    {
        var shipment = await _shipmentService.GetByIdAsync(id);

        if (shipment is null)
        {
            return NotFound("Shipment was not found.");
        }

        return Ok(shipment);
    }

    [Authorize(Roles = RoleNames.AdminOrWarehouseStaff)]
    [HttpPost]
    public async Task<ActionResult<ShipmentDto>> Create(CreateShipmentRequest request)
    {
        try
        {
            var shipment = await _shipmentService.CreateAsync(request);

            return CreatedAtAction(nameof(GetById), new { id = shipment.Id }, shipment);
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
    [HttpPut("{id:guid}/status")]
    public async Task<ActionResult<ShipmentDto>> UpdateStatus(Guid id, UpdateShipmentStatusRequest request)
    {
        try
        {
            var shipment = await _shipmentService.UpdateStatusAsync(id, request);

            if (shipment is null)
            {
                return NotFound("Shipment was not found.");
            }

            return Ok(shipment);
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
