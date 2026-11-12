using AeShipManagement.Application.Common;

namespace AeShipManagement.Application.Ships;

public interface IShipRepository
{
    Task<ShipResponse> CreateAsync(CreateShipRequest request, CancellationToken cancellationToken);
    Task<ShipResponse> GetByCodeAsync(string shipCode, CancellationToken cancellationToken);
    Task<PagedResult<ShipResponse>> ListAsync(ListShipsQuery query, CancellationToken cancellationToken);
}

public interface IShipAssignmentRepository
{
    Task<bool> AssignAsync(int userId, string shipCode, CancellationToken cancellationToken);
    Task UnassignAsync(int userId, string shipCode, CancellationToken cancellationToken);
    Task<IReadOnlyList<AssignedShipResponse>> ListShipsByUserAsync(int userId, CancellationToken cancellationToken);
}
