using AeShipManagement.Application.Users;
using Microsoft.AspNetCore.Mvc;

namespace AeShipManagement.Api.Controllers;

[ApiController]
[Route("api/v1/user-roles")]
[Produces("application/json")]
[Tags("Users")]
public sealed class UserRolesController(IUserService userService) : ControllerBase
{
    [HttpGet]
    [EndpointSummary("List the roles a user can have")]
    [ProducesResponseType<IReadOnlyList<UserRoleResponse>>(StatusCodes.Status200OK)]
    public Task<IReadOnlyList<UserRoleResponse>> List(CancellationToken cancellationToken) =>
        userService.ListRolesAsync(cancellationToken);
}
