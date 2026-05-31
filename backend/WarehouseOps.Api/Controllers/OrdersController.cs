using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WarehouseOps.Api.Security;
using WarehouseOps.Application.Dtos;
using WarehouseOps.Application.Interfaces;

namespace WarehouseOps.Api.Controllers;

[Authorize(Roles = RoleNames.AllRoles)]
[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    [HttpGet]
    public async Task<ActionResult<List<OrderDto>>> GetAll()
    {
        var orders = await _orderService.GetAllAsync();

        return Ok(orders);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<OrderDto>> GetById(Guid id)
    {
        var order = await _orderService.GetByIdAsync(id);

        if (order is null)
        {
            return NotFound("Order was not found.");
        }

        return Ok(order);
    }

    [Authorize(Roles = RoleNames.AdminOrWarehouseStaff)]
    [HttpPost]
    public async Task<ActionResult<OrderDto>> Create(CreateOrderRequest request)
    {
        try
        {
            var order = await _orderService.CreateAsync(request);

            return CreatedAtAction(nameof(GetById), new { id = order.Id }, order);
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
    public async Task<ActionResult<OrderDto>> UpdateStatus(Guid id, UpdateOrderStatusRequest request)
    {
        try
        {
            var order = await _orderService.UpdateStatusAsync(id, request);

            if (order is null)
            {
                return NotFound("Order was not found.");
            }

            return Ok(order);
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
    [HttpPut("{id:guid}/cancel")]
    public async Task<ActionResult<OrderDto>> Cancel(Guid id)
    {
        try
        {
            var order = await _orderService.CancelAsync(id);

            if (order is null)
            {
                return NotFound("Order was not found.");
            }

            return Ok(order);
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(exception.Message);
        }
    }
}
