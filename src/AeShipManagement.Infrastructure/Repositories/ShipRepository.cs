using System.Data;
using AeShipManagement.Application.Common;
using AeShipManagement.Application.Ships;
using AeShipManagement.Infrastructure.Data;
using Dapper;

namespace AeShipManagement.Infrastructure.Repositories;

internal sealed class ShipRepository(IStoredProcedureExecutor executor) : IShipRepository, IShipAssignmentRepository
{
    public async Task<ShipResponse> CreateAsync(CreateShipRequest request, CancellationToken cancellationToken)
    {
        var row = await executor.QuerySingleAsync<ShipRow>(StoredProcedures.ShipCreate, new
        {
            ShipCode = SqlParameters.Varchar(request.ShipCode, 10),
            ShipName = SqlParameters.NVarchar(request.ShipName, 100),
            FiscalYearCode = SqlParameters.Char(request.FiscalYearCode, 4),
            Status = SqlParameters.Varchar(request.Status, 10)
        }, cancellationToken);
        return row.ToResponse();
    }

    public async Task<ShipResponse> GetByCodeAsync(string shipCode, CancellationToken cancellationToken)
    {
        var row = await executor.QuerySingleAsync<ShipRow>(StoredProcedures.ShipGetByCode,
            new { ShipCode = SqlParameters.Varchar(shipCode, 10) }, cancellationToken);
        return row.ToResponse();
    }

    public async Task<PagedResult<ShipResponse>> ListAsync(ListShipsQuery query, CancellationToken cancellationToken)
    {
        var parameters = new DynamicParameters();
        parameters.Add("Status", SqlParameters.Varchar(query.Status, 10));
        parameters.Add("PageNumber", query.PageNumber);
        parameters.Add("PageSize", query.PageSize);
        parameters.Add("TotalCount", dbType: DbType.Int32, direction: ParameterDirection.Output);

        var rows = await executor.QueryAsync<ShipRow>(StoredProcedures.ShipList, parameters, cancellationToken);
        var items = rows.Select(r => r.ToResponse()).ToList();
        return new PagedResult<ShipResponse>(items, query.PageNumber, query.PageSize, parameters.Get<int>("TotalCount"));
    }

    public async Task<bool> AssignAsync(int userId, string shipCode, CancellationToken cancellationToken)
    {
        var parameters = new DynamicParameters();
        parameters.Add("UserId", userId);
        parameters.Add("ShipCode", SqlParameters.Varchar(shipCode, 10));
        parameters.Add("Created", dbType: DbType.Boolean, direction: ParameterDirection.Output);

        await executor.ExecuteAsync(StoredProcedures.UserShipAssign, parameters, cancellationToken);
        return parameters.Get<bool>("Created");
    }

    public Task UnassignAsync(int userId, string shipCode, CancellationToken cancellationToken) =>
        executor.ExecuteAsync(StoredProcedures.UserShipUnassign,
            new { UserId = userId, ShipCode = SqlParameters.Varchar(shipCode, 10) }, cancellationToken);

    public async Task<IReadOnlyList<AssignedShipResponse>> ListShipsByUserAsync(int userId, CancellationToken cancellationToken)
    {
        var rows = await executor.QueryAsync<AssignedShipRow>(StoredProcedures.UserShipListShipsByUser,
            new { UserId = userId }, cancellationToken);
        return rows.Select(r => new AssignedShipResponse(
            r.ShipCode, r.ShipName, r.FiscalYearCode, r.FiscalYearDescription, r.Status, r.AssignedAt.AsUtc())).ToList();
    }

    private sealed class ShipRow
    {
        public string ShipCode { get; init; } = string.Empty;
        public string ShipName { get; init; } = string.Empty;
        public string FiscalYearCode { get; init; } = string.Empty;
        public string FiscalYearDescription { get; init; } = string.Empty;
        public string Status { get; init; } = string.Empty;
        public DateTime CreatedAt { get; init; }

        public ShipResponse ToResponse() =>
            new(ShipCode, ShipName, FiscalYearCode, FiscalYearDescription, Status, CreatedAt.AsUtc());
    }

    private sealed class AssignedShipRow
    {
        public string ShipCode { get; init; } = string.Empty;
        public string ShipName { get; init; } = string.Empty;
        public string FiscalYearCode { get; init; } = string.Empty;
        public string FiscalYearDescription { get; init; } = string.Empty;
        public string Status { get; init; } = string.Empty;
        public DateTime AssignedAt { get; init; }
    }
}
