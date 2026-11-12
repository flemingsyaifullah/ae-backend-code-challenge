using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;

namespace AeShipManagement.IntegrationTests.Infrastructure;

public sealed record CrewRow(
    string RankName,
    string CrewMemberId,
    string FirstName,
    string LastName,
    int Age,
    string Nationality,
    DateTime SignOnDate,
    string SignOnDateDisplay,
    DateTime EndOfContractDate,
    string Status);

public sealed record ReportHeader(
    string ShipCode,
    string ShipName,
    string FiscalYearCode,
    DateTime AccountPeriod,
    DateTime YtdStartPeriod,
    DateTime YtdEndPeriod);

public sealed record ReportRow(
    string AccountNumber,
    string AccountName,
    string AccountType,
    int AccountLevel,
    string? ParentAccountNumber,
    decimal? MonthActual,
    decimal? MonthBudget,
    decimal? MonthVariance,
    decimal? YtdActual,
    decimal? YtdBudget,
    decimal? YtdVariance);

public static class Procedures
{
    public static async Task<(IReadOnlyList<CrewRow> Rows, int Total)> CrewListAsync(
        this DatabaseFixture db,
        string shipCode,
        DateTime? asOf = null,
        int pageNumber = 1,
        int pageSize = 100,
        string? sortBy = null,
        string? sortDirection = null,
        string? search = null)
    {
        await using var connection = await db.OpenAsync();
        var parameters = new DynamicParameters(new
        {
            ShipCode = shipCode,
            PageNumber = pageNumber,
            PageSize = pageSize,
            SortBy = sortBy,
            SortDirection = sortDirection,
            SearchTerm = search,
            AsOfDate = asOf
        });
        parameters.Add("TotalCount", dbType: DbType.Int32, direction: ParameterDirection.Output);

        var rows = (await connection.QueryAsync<CrewRow>("dbo.usp_Crew_ListByShip", parameters,
            commandType: CommandType.StoredProcedure)).AsList();
        return (rows, parameters.Get<int>("TotalCount"));
    }

    public static async Task<(ReportHeader Header, IReadOnlyList<ReportRow> Rows)> ReportAsync(
        this DatabaseFixture db, string shipCode, DateTime period, bool summary = false)
    {
        await using var connection = await db.OpenAsync();
        await using var grid = await connection.QueryMultipleAsync(
            summary ? "dbo.usp_FinancialReport_Summary" : "dbo.usp_FinancialReport_Detail",
            new { ShipCode = shipCode, AccountPeriod = period },
            commandType: CommandType.StoredProcedure);
        var header = await grid.ReadSingleAsync<ReportHeader>();
        var rows = (await grid.ReadAsync<ReportRow>()).AsList();
        return (header, rows);
    }

    public static async Task<T> ScalarAsync<T>(this DatabaseFixture db, string sql, object? parameters = null)
    {
        await using var connection = await db.OpenAsync();
        return await connection.ExecuteScalarAsync<T>(sql, parameters) ?? throw new InvalidOperationException("No value.");
    }

    public static async Task ExecuteSqlAsync(this DatabaseFixture db, string sql, object? parameters = null)
    {
        await using var connection = await db.OpenAsync();
        await connection.ExecuteAsync(sql, parameters);
    }

    public static async Task<SqlException> ProcedureErrorAsync(this DatabaseFixture db, string procedure, object parameters)
    {
        await using var connection = await db.OpenAsync();
        return await Assert.ThrowsAsync<SqlException>(() =>
            connection.ExecuteAsync(procedure, parameters, commandType: CommandType.StoredProcedure));
    }
}
