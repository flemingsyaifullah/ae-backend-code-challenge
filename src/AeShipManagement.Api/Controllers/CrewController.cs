using AeShipManagement.Application.Common;
using AeShipManagement.Application.Crew;
using Microsoft.AspNetCore.Mvc;

namespace AeShipManagement.Api.Controllers;

[ApiController]
[Route("api/v1/ships/{shipCode}/crew")]
[Produces("application/json")]
[Tags("Crew")]
public sealed class CrewController(ICrewService crewService) : ControllerBase
{
    [HttpGet]
    [EndpointSummary("Crew currently Onboard or Relief Due on an active ship")]
    [EndpointDescription(
        "sortBy: RankName, CrewMemberId, FirstName, LastName, Age, Nationality, SignOnDate, Status (default: rank seniority). " +
        "sortDirection: asc or desc. search matches every column except Status, including partial dates such as '05 Apr'.")]
    [ProducesResponseType<PagedResult<CrewMemberResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public Task<PagedResult<CrewMemberResponse>> List(
        string shipCode,
        [FromQuery] int pageNumber = Paging.DefaultPageNumber,
        [FromQuery] int pageSize = Paging.DefaultPageSize,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortDirection = null,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default) =>
        crewService.ListByShipAsync(
            new CrewListQuery(shipCode, pageNumber, pageSize, sortBy, sortDirection, search),
            cancellationToken);
}
