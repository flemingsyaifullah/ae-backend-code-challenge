using System.Globalization;

namespace AeShipManagement.Application.FinancialReports;

public enum FinancialReportType
{
    Detail,
    Summary
}

public sealed record FinancialReportQuery(string ShipCode, string? Period, FinancialReportType ReportType);

public sealed record FinancialReportResponse(
    string ShipCode,
    string ShipName,
    string FiscalYearCode,
    FinancialReportType ReportType,
    string Period,
    string YtdFrom,
    string YtdTo,
    IReadOnlyList<FinancialReportRow> Rows);

public sealed record FinancialReportRow(
    string AccountNumber,
    string CoaDescription,
    string AccountType,
    int Level,
    string? ParentAccountNumber,
    decimal? Actual,
    decimal? Budget,
    decimal? Variance,
    decimal? YtdActual,
    decimal? YtdBudget,
    decimal? YtdVariance);

public static class AccountingPeriod
{
    public const string Format = "yyyy-MM";

    public static bool TryParse(string? value, out DateOnly firstDayOfMonth)
    {
        firstDayOfMonth = default;
        if (string.IsNullOrWhiteSpace(value)
            || !DateOnly.TryParseExact(value.Trim(), Format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)
            || parsed.Year < 1900)
        {
            return false;
        }

        firstDayOfMonth = new DateOnly(parsed.Year, parsed.Month, 1);
        return true;
    }

    public static string ToText(DateOnly period) => period.ToString(Format, CultureInfo.InvariantCulture);
}
