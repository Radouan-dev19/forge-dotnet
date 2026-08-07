using ForgeApiLab.Models;
using ForgeApiLab.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ForgeApiLab.Controllers;

[ApiController]
[Authorize]
[Route("orders")]
public sealed class OrdersController(OrderStore store) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<OrderResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<OrderResponse>>> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string sort = "id",
        CancellationToken cancellationToken = default)
    {
        if (page < 1 || pageSize is < 1 or > 100 || sort is not ("id" or "customer"))
        {
            ModelState.AddModelError("pagination", "Page, taille ou tri invalide.");
            return ValidationProblem(ModelState);
        }

        return Ok(await store.ListAsync(page, pageSize, sort, cancellationToken));
    }

    [HttpGet("{id:int}", Name = nameof(GetById))]
    [ProducesResponseType<OrderResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        OrderResponse? order = await store.FindAsync(id, cancellationToken);
        return order is null ? NotFound() : Ok(order);
    }

    [HttpPost]
    [Authorize(Policy = "OrdersWrite")]
    [ProducesResponseType<OrderResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<OrderResponse>> Create(CreateOrderRequest request, CancellationToken cancellationToken)
    {
        OrderResponse created = await store.AddAsync(request, cancellationToken);
        return CreatedAtRoute(nameof(GetById), new { id = created.Id }, created);
    }
}
