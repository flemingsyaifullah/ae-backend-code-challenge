using AeShipManagement.Application.Crew;
using AeShipManagement.Application.FinancialReports;

namespace AeShipManagement.UnitTests.Validation;

public sealed class CrewAndReportValidatorTests
{
    private readonly CrewListQueryValidator _crewValidator = new();
    private readonly FinancialReportQueryValidator _reportValidator = new();

    [Theory]
    [InlineData("RankName")]
    [InlineData("rankname")]
    [InlineData("SIGNONDATE")]
    [InlineData("Status")]
    [InlineData(null)]
    public void Crew_sort_accepts_documented_columns_case_insensitively(string? sortBy)
    {
        var result = _crewValidator.Validate(new CrewListQuery("SHIP01", SortBy: sortBy));

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("Salary")]
    [InlineData("RankName; DROP TABLE dbo.Ship")]
    public void Crew_sort_rejects_unknown_columns(string sortBy)
    {
        var result = _crewValidator.Validate(new CrewListQuery("SHIP01", SortBy: sortBy));

        Assert.Contains(result.Errors, e => e.PropertyName == "SortBy");
    }

    [Theory]
    [InlineData("up")]
    [InlineData("ascending")]
    public void Crew_sort_direction_must_be_asc_or_desc(string direction)
    {
        var result = _crewValidator.Validate(new CrewListQuery("SHIP01", SortDirection: direction));

        Assert.Contains(result.Errors, e => e.PropertyName == "SortDirection");
    }

    [Theory]
    [InlineData(0, 20, "PageNumber")]
    [InlineData(1, 0, "PageSize")]
    [InlineData(1, 101, "PageSize")]
    public void Crew_paging_limits_are_enforced(int pageNumber, int pageSize, string property)
    {
        var result = _crewValidator.Validate(new CrewListQuery("SHIP01", pageNumber, pageSize));

        Assert.Contains(result.Errors, e => e.PropertyName == property);
    }

    [Fact]
    public void Crew_search_is_limited_to_100_characters()
    {
        var result = _crewValidator.Validate(new CrewListQuery("SHIP01", Search: new string('x', 101)));

        Assert.Contains(result.Errors, e => e.PropertyName == "Search");
    }

    [Theory]
    [InlineData("2025-07")]
    [InlineData("2024-12")]
    [InlineData(" 2025-01 ")]
    public void Report_period_accepts_year_month(string period)
    {
        var result = _reportValidator.Validate(new FinancialReportQuery("SHIP01", period, FinancialReportType.Detail));

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("2025-13")]
    [InlineData("2025-7")]
    [InlineData("07-2025")]
    [InlineData("2025-07-01")]
    [InlineData("1899-12")]
    public void Report_period_rejects_invalid_values(string? period)
    {
        var result = _reportValidator.Validate(new FinancialReportQuery("SHIP01", period, FinancialReportType.Summary));

        Assert.Contains(result.Errors, e => e.PropertyName == "Period");
    }

    [Fact]
    public void Accounting_period_parses_to_first_day_of_month()
    {
        Assert.True(AccountingPeriod.TryParse("2025-07", out var period));
        Assert.Equal(new DateOnly(2025, 7, 1), period);
        Assert.Equal("2025-07", AccountingPeriod.ToText(period));
    }
}
