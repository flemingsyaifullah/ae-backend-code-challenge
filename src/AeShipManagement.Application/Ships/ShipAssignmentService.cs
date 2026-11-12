using AeShipManagement.Application.Common;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace AeShipManagement.Application.Ships;

public interface IShipAssignmentService
{
    Task<bool> AssignAsync(int userId, string shipCode, CancellationToken cancellationToken);
    Task UnassignAsync(int userId, string shipCode, CancellationToken cancellationToken);
    Task<IReadOnlyList<AssignedShipResponse>> ListShipsByUserAsync(int userId, CancellationToken cancellationToken);
}

public sealed class ShipAssignmentService(
    IShipAssignmentRepository repository,
    ShipCodeValidator shipCodeValidator,
    ILogger<ShipAssignmentService> logger) : IShipAssignmentService
{
    public async Task<bool> AssignAsync(int userId, string shipCode, CancellationToken cancellationToken)
    {
        await ValidateAsync(userId, shipCode, cancellationToken);
        var created = await repository.AssignAsync(userId, shipCode, cancellationToken);
        if (created)
        {
            logger.LogInformation("Ship {ShipCode} assigned to user {UserId}", shipCode, userId);
        }

        return created;
    }

    public async Task UnassignAsync(int userId, string shipCode, CancellationToken cancellationToken)
    {
        await ValidateAsync(userId, shipCode, cancellationToken);
        await repository.UnassignAsync(userId, shipCode, cancellationToken);
        logger.LogInformation("Ship {ShipCode} unassigned from user {UserId}", shipCode, userId);
    }

    public Task<IReadOnlyList<AssignedShipResponse>> ListShipsByUserAsync(int userId, CancellationToken cancellationToken)
    {
        UserIdRules.EnsureValid(userId);
        return repository.ListShipsByUserAsync(userId, cancellationToken);
    }

    private async Task ValidateAsync(int userId, string shipCode, CancellationToken cancellationToken)
    {
        UserIdRules.EnsureValid(userId);
        await shipCodeValidator.ValidateAndThrowAsync(shipCode, cancellationToken);
    }
}
