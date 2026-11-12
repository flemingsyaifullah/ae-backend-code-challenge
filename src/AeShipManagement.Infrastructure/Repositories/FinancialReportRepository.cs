using System.Data;
using AeShipManagement.Application.FinancialReports;
using AeShipManagement.Infrastructure.Data;
using Dapper;

namespace AeShipManagement.Infrastructure.Repositories;

internal sealed class FinancialReportRepository(IStoredProcedureExecutor executor) : IFinancialReportRepository
{
    public Task<FinancialReportResponse> GetAsync(
        string shipCode,
        DateOnly accountingPeriod,
        FinancialReportType reportType,
        CancellationToken cancellationToken)
    {
        var procedure = reportType == FinancialReportType.Summary
            ? StoredProcedures.FinancialReportSummary
            : StoredProcedures.FinancialReportDetail;

        var parameters = new DynamicParameters();
        parameters.Add("ShipCode", SqlParameters.Varchar(shipCode, 10));
        parameters.Add("AccountPeriod", accountingPeriod.ToDateTime(), DbType.Date);

        return executor.QueryMultipleAsync(procedure, parameters, async grid =>
        {
            var header = await grid.ReadSingleAsync<HeaderRow>();
            var rows = (await grid.ReadAsync<ReportRow>()).Select(r => new FinancialReportRow(
                r.AccountNumber,
                r.AccountName,
                r.AccountType == "P" ? "Parent" : "Child",
                r.AccountLevel,
                r.ParentAccountNumber,
                r.MonthActual,
                r.MonthBudget,
                r.MonthVariance,
                r.YtdActual,
                r.YtdBudget,
                r.YtdVariance)).ToList();

            return new FinancialReportResponse(
                header.ShipCode,
                header.ShipName,
                header.FiscalYearCode,
                reportType,
                AccountingPeriod.ToText(DateOnly.FromDateTime(header.AccountPeriod)),
                AccountingPeriod.ToText(DateOnly.FromDateTime(header.YtdStartPeriod)),
                AccountingPeriod.ToText(DateOnly.FromDateTime(header.YtdEndPeriod)),
                rows);
        }, cancellationToken);
    }

    private sealed class HeaderRow
    {
        public string ShipCode { get; init; } = string.Empty;
        public string ShipName { get; init; } = string.Empty;
        public string FiscalYearCode { get; init; } = string.Empty;
        public DateTime AccountPeriod { get; init; }
        public DateTime YtdStartPeriod { get; init; }
        public DateTime YtdEndPeriod { get; init; }
    }

    private sealed class ReportRow
    {
        public string AccountNumber { get; init; } = string.Empty;
        public string AccountName { get; init; } = string.Empty;
        public string AccountType { get; init; } = string.Empty;
        public int AccountLevel { get; init; }
        public string? ParentAccountNumber { get; init; }
        public decimal? MonthActual { get; init; }
        public decimal? MonthBudget { get; init; }
        public decimal? MonthVariance { get; init; }
        public decimal? YtdActual { get; init; }
        public decimal? YtdBudget { get; init; }
        public decimal? YtdVariance { get; init; }
    }
}
