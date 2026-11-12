using AeShipManagement.Application.Common;
using AeShipManagement.Application.Ships;
using Microsoft.AspNetCore.Mvc;

namespace AeShipManagement.Api.Controllers;

[ApiController]
[Route("api/v1/ships")]
[Produces("application/json")]
[Tags("Ships")]
public sealed class ShipsController(IShipService shipService) : ControllerBase
{
    [HttpPost]
    [EndpointSummary("Create a ship")]
    [EndpointDescription("Status defaults to 'Active'. FiscalYearCode must exist, e.g. '0112' (Jan-Dec) or '0403' (Apr-Mar).")]
    [ProducesResponseType<ShipResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ShipResponse>> Create([FromBody] CreateShipRequest request, CancellationToken cancellationToken)
    {
        var ship = await shipService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetByCode), new { shipCode = ship.ShipCode }, ship);
    }

    [HttpGet]
    [EndpointSummary("List ships (paged, optional status filter)")]
    [ProducesResponseType<PagedResult<ShipResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public Task<PagedResult<ShipResponse>> List(
        [FromQuery] string? status = null,
        [FromQuery] int pageNumber = Paging.DefaultPageNumber,
        [FromQuery] int pageSize = Paging.DefaultPageSize,
        CancellationToken cancellationToken = default) =>
        shipService.ListAsync(new ListShipsQuery(status, pageNumber, pageSize), cancellationToken);

    [HttpGet("{shipCode}")]
    [EndpointSummary("Get a ship by code")]
    [ProducesResponseType<ShipResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<ShipResponse> GetByCode(string shipCode, CancellationToken cancellationToken) =>
        shipService.GetByCodeAsync(shipCode, cancellationToken);
}
