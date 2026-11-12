using AeShipManagement.IntegrationTests.Infrastructure;

namespace AeShipManagement.IntegrationTests;

[Collection(DatabaseCollection.Name)]
[Trait("Category", "Integration")]
public sealed class FinancialReportProcedureTests(DatabaseFixture db)
{
    private static readonly DateTime July2025 = new(2025, 7, 1);

    [Fact]
    public async Task Child_rows_match_the_example_in_the_brief()
    {
        var (_, rows) = await db.ReportAsync("SHIP01", July2025);

        var awards = rows.Single(r => r.AccountNumber == "7120000");
        Assert.Equal((null, 1000m, -1000m, 3200m, 3300m, -100m),
            (awards.MonthActual, awards.MonthBudget, awards.MonthVariance, awards.YtdActual, awards.YtdBudget, awards.YtdVariance));

        var scholarships = rows.Single(r => r.AccountNumber == "7135000");
        Assert.Equal((2300m, 2200m, 100m, 6400m, 6300m, 100m),
            (scholarships.MonthActual, scholarships.MonthBudget, scholarships.MonthVariance,
             scholarships.YtdActual, scholarships.YtdBudget, scholarships.YtdVariance));
    }

    [Theory]
    [InlineData("SHIP01", "2025-07-01")]
    [InlineData("SHIP02", "2025-02-01")]
    [InlineData("SHIP03", "2024-12-01")]
    public async Task Every_parent_equals_the_sum_of_its_direct_children(string ship, string period)
    {
        var (_, rows) = await db.ReportAsync(ship, DateTime.Parse(period));

        foreach (var parent in rows.Where(r => r.AccountType == "P"))
        {
            var children = rows.Where(r => r.ParentAccountNumber == parent.AccountNumber).ToList();
            Assert.Equal(children.Sum(c => c.MonthActual ?? 0), parent.MonthActual ?? 0);
            Assert.Equal(children.Sum(c => c.MonthBudget ?? 0), parent.MonthBudget ?? 0);
            Assert.Equal(children.Sum(c => c.YtdActual ?? 0), parent.YtdActual ?? 0);
            Assert.Equal(children.Sum(c => c.YtdBudget ?? 0), parent.YtdBudget ?? 0);
        }
    }

    [Fact]
    public async Task Root_totals_match_raw_transactions_and_budgets()
    {
        var (_, rows) = await db.ReportAsync("SHIP01", July2025);
        var root = rows.Single(r => r.AccountNumber == "7000000");

        var monthActual = await db.ScalarAsync<decimal>("""
            SELECT SUM(t.ActualValue) FROM dbo.AccountTransaction t JOIN dbo.Ship s ON s.ShipId = t.ShipId
            WHERE s.ShipCode = 'SHIP01' AND t.AccountPeriod = '2025-07-01'
            """);
        var ytdBudget = await db.ScalarAsync<decimal>("""
            SELECT SUM(b.BudgetValue) FROM dbo.BudgetEntry b JOIN dbo.Ship s ON s.ShipId = b.ShipId
            WHERE s.ShipCode = 'SHIP01' AND b.AccountPeriod BETWEEN '2025-01-01' AND '2025-07-01'
            """);

        Assert.Equal(monthActual, root.MonthActual);
        Assert.Equal(ytdBudget, root.YtdBudget);
        Assert.Equal(root.MonthActual - root.MonthBudget, root.MonthVariance);
        Assert.Equal(root.YtdActual - root.YtdBudget, root.YtdVariance);
    }

    [Theory]
    [InlineData("SHIP01", "2025-07-01", "2025-01-01")]
    [InlineData("SHIP01", "2025-01-01", "2025-01-01")]
    [InlineData("SHIP02", "2025-02-01", "2024-04-01")]
    [InlineData("SHIP02", "2025-03-01", "2024-04-01")]
    [InlineData("SHIP02", "2025-04-01", "2025-04-01")]
    [InlineData("SHIP02", "2024-12-01", "2024-04-01")]
    public async Task Ytd_range_follows_the_ship_fiscal_year(string ship, string period, string expectedStart)
    {
        var (header, _) = await db.ReportAsync(ship, DateTime.Parse(period));

        Assert.Equal(DateTime.Parse(expectedStart), header.YtdStartPeriod);
        Assert.Equal(DateTime.Parse(period), header.YtdEndPeriod);
    }

    [Fact]
    public async Task Ytd_for_april_march_fiscal_year_spans_two_calendar_years()
    {
        var (_, rows) = await db.ReportAsync("SHIP02", new DateTime(2025, 2, 1));
        var root = rows.Single(r => r.AccountNumber == "7000000");

        var expected = await db.ScalarAsync<decimal>("""
            SELECT SUM(t.ActualValue) FROM dbo.AccountTransaction t JOIN dbo.Ship s ON s.ShipId = t.ShipId
            WHERE s.ShipCode = 'SHIP02' AND t.AccountPeriod BETWEEN '2024-04-01' AND '2025-02-01'
            """);

        Assert.Equal(expected, root.YtdActual);
    }

    [Fact]
    public async Task Recorded_zero_and_missing_value_stay_distinct()
    {
        var (_, rows) = await db.ReportAsync("SHIP01", July2025);
        var dryDocking = rows.Single(r => r.AccountNumber == "7540000");

        Assert.Null(dryDocking.MonthActual);
        Assert.Equal(0m, dryDocking.MonthBudget);
        Assert.Equal(0m, dryDocking.MonthVariance);
    }

    [Fact]
    public async Task Accounts_with_only_zero_or_missing_values_are_excluded()
    {
        var (_, rows) = await db.ReportAsync("SHIP03", July2025);

        Assert.DoesNotContain(rows, r => r.AccountNumber == "7630000");
        Assert.All(rows, r => Assert.True(
            (r.MonthActual ?? 0) != 0 || (r.MonthBudget ?? 0) != 0 || (r.YtdActual ?? 0) != 0 || (r.YtdBudget ?? 0) != 0));
    }

    [Fact]
    public async Task Summary_contains_parent_accounts_only_with_detail_values()
    {
        var (_, detail) = await db.ReportAsync("SHIP01", July2025);
        var (_, summary) = await db.ReportAsync("SHIP01", July2025, summary: true);

        Assert.All(summary, r => Assert.Equal("P", r.AccountType));
        Assert.Equal(detail.Where(r => r.AccountType == "P"), summary);
    }

    [Fact]
    public async Task Rows_follow_the_account_tree_order()
    {
        var (_, rows) = await db.ReportAsync("SHIP01", July2025);

        Assert.Equal("7000000", rows[0].AccountNumber);
        Assert.Equal(1, rows[0].AccountLevel);
        var index = rows.Select((r, i) => (r.AccountNumber, i)).ToDictionary(x => x.AccountNumber, x => x.i);
        Assert.All(rows.Where(r => r.ParentAccountNumber is not null),
            r => Assert.True(index[r.ParentAccountNumber!] < index[r.AccountNumber]));
    }

    [Fact]
    public async Task Period_without_data_returns_header_and_no_rows()
    {
        var (header, rows) = await db.ReportAsync("SHIP01", new DateTime(2023, 6, 1));

        Assert.Equal("SHIP01", header.ShipCode);
        Assert.Empty(rows);
    }

    [Theory]
    [InlineData("SHIP05", "2025-07-01", 50422)]
    [InlineData("NOPE", "2025-07-01", 50404)]
    [InlineData("SHIP01", "2025-07-15", 50400)]
    public async Task Invalid_report_requests_are_rejected(string ship, string period, int expectedError)
    {
        var error = await db.ProcedureErrorAsync("dbo.usp_FinancialReport_Detail",
            new { ShipCode = ship, AccountPeriod = DateTime.Parse(period) });

        Assert.Equal(expectedError, error.Number);
    }
}
