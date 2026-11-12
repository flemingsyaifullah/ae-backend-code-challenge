using AeShipManagement.Application.Common;
using AeShipManagement.Application.Ships;
using AeShipManagement.Application.Users;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace AeShipManagement.Api.Controllers;

[ApiController]
[Route("api/v1/users")]
[Produces("application/json")]
[Tags("Users")]
public sealed class UsersController(IUserService userService, IShipAssignmentService assignmentService) : ControllerBase
{
    [HttpPost]
    [EndpointSummary("Create a user")]
    [ProducesResponseType<UserResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserResponse>> Create([FromBody] CreateUserRequest request, CancellationToken cancellationToken)
    {
        var user = await userService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { userId = user.UserId }, user);
    }

    [HttpGet]
    [EndpointSummary("List users (paged)")]
    [ProducesResponseType<PagedResult<UserResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public Task<PagedResult<UserResponse>> List(
        [FromQuery] int pageNumber = Paging.DefaultPageNumber,
        [FromQuery] int pageSize = Paging.DefaultPageSize,
        CancellationToken cancellationToken = default) =>
        userService.ListAsync(new ListUsersQuery(pageNumber, pageSize), cancellationToken);

    [HttpGet("{userId:int}")]
    [EndpointSummary("Get a user by id")]
    [ProducesResponseType<UserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<UserResponse> GetById(int userId, CancellationToken cancellationToken) =>
        userService.GetByIdAsync(userId, cancellationToken);

    [HttpGet("{userId:int}/ships")]
    [EndpointSummary("List the ships assigned to a user")]
    [ProducesResponseType<IReadOnlyList<AssignedShipResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<IReadOnlyList<AssignedShipResponse>> ListShips(int userId, CancellationToken cancellationToken) =>
        assignmentService.ListShipsByUserAsync(userId, cancellationToken);

    [HttpPut("{userId:int}/ships/{shipCode}")]
    [EndpointSummary("Assign a ship to a user")]
    [EndpointDescription("Idempotent: assigning a ship that is already assigned succeeds without creating a duplicate.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<NoContent> AssignShip(int userId, string shipCode, CancellationToken cancellationToken)
    {
        await assignmentService.AssignAsync(userId, shipCode, cancellationToken);
        return TypedResults.NoContent();
    }

    [HttpDelete("{userId:int}/ships/{shipCode}")]
    [EndpointSummary("Remove a ship assignment from a user")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<NoContent> UnassignShip(int userId, string shipCode, CancellationToken cancellationToken)
    {
        await assignmentService.UnassignAsync(userId, shipCode, cancellationToken);
        return TypedResults.NoContent();
    }
}
