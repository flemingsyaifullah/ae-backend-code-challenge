using AeShipManagement.Application.Common;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace AeShipManagement.Application.Ships;

public interface IShipService
{
    Task<ShipResponse> CreateAsync(CreateShipRequest request, CancellationToken cancellationToken);
    Task<ShipResponse> GetByCodeAsync(string shipCode, CancellationToken cancellationToken);
    Task<PagedResult<ShipResponse>> ListAsync(ListShipsQuery query, CancellationToken cancellationToken);
}

public sealed class ShipService(
    IShipRepository repository,
    IValidator<CreateShipRequest> createValidator,
    IValidator<ListShipsQuery> listValidator,
    ShipCodeValidator shipCodeValidator,
    ILogger<ShipService> logger) : IShipService
{
    public async Task<ShipResponse> CreateAsync(CreateShipRequest request, CancellationToken cancellationToken)
    {
        await createValidator.ValidateAndThrowAsync(request, cancellationToken);
        var normalized = request with { Status = ShipStatusRules.Normalize(request.Status) ?? ShipStatuses.Active };
        var ship = await repository.CreateAsync(normalized, cancellationToken);
        logger.LogInformation("Ship {ShipCode} created with fiscal year {FiscalYearCode}", ship.ShipCode, ship.FiscalYearCode);
        return ship;
    }

    public async Task<ShipResponse> GetByCodeAsync(string shipCode, CancellationToken cancellationToken)
    {
        await shipCodeValidator.ValidateAndThrowAsync(shipCode, cancellationToken);
        return await repository.GetByCodeAsync(shipCode, cancellationToken);
    }

    public async Task<PagedResult<ShipResponse>> ListAsync(ListShipsQuery query, CancellationToken cancellationToken)
    {
        await listValidator.ValidateAndThrowAsync(query, cancellationToken);
        return await repository.ListAsync(query with { Status = ShipStatusRules.Normalize(query.Status) }, cancellationToken);
    }
}
