using System.Data;
using AeShipManagement.Application.Common;
using AeShipManagement.Application.Crew;
using AeShipManagement.Infrastructure.Data;
using Dapper;

namespace AeShipManagement.Infrastructure.Repositories;

internal sealed class CrewRepository(IStoredProcedureExecutor executor) : ICrewRepository
{
    public async Task<PagedResult<CrewMemberResponse>> ListByShipAsync(CrewListQuery query, CancellationToken cancellationToken)
    {
        var parameters = new DynamicParameters();
        parameters.Add("ShipCode", SqlParameters.Varchar(query.ShipCode, 10));
        parameters.Add("PageNumber", query.PageNumber);
        parameters.Add("PageSize", query.PageSize);
        parameters.Add("SortBy", SqlParameters.Varchar(query.SortBy, 20));
        parameters.Add("SortDirection", SqlParameters.Varchar(query.SortDirection, 4));
        parameters.Add("SearchTerm", SqlParameters.NVarchar(query.Search, 100));
        parameters.Add("TotalCount", dbType: DbType.Int32, direction: ParameterDirection.Output);

        var rows = await executor.QueryAsync<CrewRow>(StoredProcedures.CrewListByShip, parameters, cancellationToken);
        var items = rows.Select(r => new CrewMemberResponse(
            r.RankName,
            r.CrewMemberId,
            r.FirstName,
            r.LastName,
            r.Age,
            r.Nationality,
            DateOnly.FromDateTime(r.SignOnDate),
            r.SignOnDateDisplay,
            DateOnly.FromDateTime(r.EndOfContractDate),
            r.Status)).ToList();

        return new PagedResult<CrewMemberResponse>(items, query.PageNumber, query.PageSize, parameters.Get<int>("TotalCount"));
    }

    private sealed class CrewRow
    {
        public string RankName { get; init; } = string.Empty;
        public string CrewMemberId { get; init; } = string.Empty;
        public string FirstName { get; init; } = string.Empty;
        public string LastName { get; init; } = string.Empty;
        public int Age { get; init; }
        public string Nationality { get; init; } = string.Empty;
        public DateTime SignOnDate { get; init; }
        public string SignOnDateDisplay { get; init; } = string.Empty;
        public DateTime EndOfContractDate { get; init; }
        public string Status { get; init; } = string.Empty;
    }
}
