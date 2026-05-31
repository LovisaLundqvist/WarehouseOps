using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WarehouseOps.Api.Security;
using WarehouseOps.Application.Dtos;
using WarehouseOps.Application.Interfaces;

namespace WarehouseOps.Api.Controllers;

[Authorize(Roles = RoleNames.AllRoles)]
[ApiController]
[Route("api/[controller]")]
public class InventoryController : ControllerBase
{
    private readonly IInventoryService _inventoryService;

    public InventoryController(IInventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    [HttpGet]
    public async Task<ActionResult<List<InventoryItemDto>>> GetAll()
    {
        var inventoryItems = await _inventoryService.GetAllAsync();

        return Ok(inventoryItems);
    }

    [HttpGet("low-stock")]
    public async Task<ActionResult<List<InventoryItemDto>>> GetLowStock()
    {
        var inventoryItems = await _inventoryService.GetLowStockAsync();

        return Ok(inventoryItems);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<InventoryItemDto>> GetById(Guid id)
    {
        var inventoryItem = await _inventoryService.GetByIdAsync(id);

        if (inventoryItem is null)
        {
            return NotFound("Inventory item was not found.");
        }

        return Ok(inventoryItem);
    }

    [Authorize(Roles = RoleNames.AdminOrWarehouseStaff)]
    [HttpPost]
    public async Task<ActionResult<InventoryItemDto>> Create(CreateInventoryItemRequest request)
    {
        try
        {
            var inventoryItem = await _inventoryService.CreateAsync(request);

            return CreatedAtAction(nameof(GetById), new { id = inventoryItem.Id }, inventoryItem);
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
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<InventoryItemDto>> Update(Guid id, UpdateInventoryItemRequest request)
    {
        try
        {
            var inventoryItem = await _inventoryService.UpdateAsync(id, request);

            if (inventoryItem is null)
            {
                return NotFound("Inventory item was not found.");
            }

            return Ok(inventoryItem);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }
}
