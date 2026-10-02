using Microsoft.AspNetCore.Mvc;
using OrderManagement.API.Middleware;
using OrderManagement.Model.DTOs.Requests;
using OrderManagement.Model.Interfaces;

namespace OrderManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public sealed class OrdersController(IOrderService orderService) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request, CancellationToken ct)
    {
        var idempotencyKey = HttpContext.Items[IdempotencyMiddleware.ItemKey]?.ToString()
            ?? throw new InvalidOperationException("IdempotencyMiddleware must be registered.");

        var order = await orderService.CreateOrderAsync(request, idempotencyKey, ct);

        return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, order);
    }

    [HttpGet("{id:guid}", Name = nameof(GetOrder))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrder(Guid id, CancellationToken ct)
    {
        var order = await orderService.GetOrderByIdAsync(id, ct);
        return Ok(order);
    }
    
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListOrders([FromQuery] ListOrdersRequest request, CancellationToken ct)
    {
        var result = await orderService.ListOrdersAsync(request, ct);
        return Ok(result);
    }
    
    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateOrderStatusRequest request, CancellationToken ct)
    {
        var order = await orderService.UpdateOrderStatusAsync(id, request, ct);
        return Ok(order);
    }
    
    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CancelOrder(Guid id, CancellationToken ct)
    {
        var order = await orderService.CancelOrderAsync(id, ct);
        return Ok(order);
    }
}
