using AeShipManagement.IntegrationTests.Infrastructure;

namespace AeShipManagement.IntegrationTests;

[Collection(DatabaseCollection.Name)]
[Trait("Category", "Integration")]
public sealed class CrewListProcedureTests(DatabaseFixture db) : IAsyncLifetime
{
    private const string Ship = "ITCREW";
    private static readonly DateTime AsOf = new(2030, 6, 15);

    public Task InitializeAsync() => db.ExecuteSqlAsync("""
        IF NOT EXISTS (SELECT 1 FROM dbo.Ship WHERE ShipCode = 'ITCREW')
        BEGIN
            INSERT INTO dbo.Ship (ShipCode, ShipName, FiscalYearCode, Status) VALUES ('ITCREW', N'Boundary Tester', '0112', 'Active');

            DECLARE @ShipId INT = (SELECT ShipId FROM dbo.Ship WHERE ShipCode = 'ITCREW');
            DECLARE @Greek SMALLINT = (SELECT NationalityId FROM dbo.Nationality WHERE NationalityName = N'Greek');
            DECLARE @AB SMALLINT = (SELECT RankId FROM dbo.[Rank] WHERE RankCode = 'AB');

            INSERT INTO dbo.CrewMember (CrewMemberId, FirstName, LastName, BirthDate, NationalityId)
            VALUES ('IT01', N'Eoc', N'Today', '1980-07-30', @Greek),
                   ('IT02', N'Eoc', N'PlusThirty', '2000-02-29', @Greek),
                   ('IT03', N'Eoc', N'PlusThirtyOne', '1990-01-01', @Greek),
                   ('IT04', N'SignOn', N'Today', '1990-01-01', @Greek),
                   ('IT05', N'SignOn', N'Tomorrow', '1990-01-01', @Greek),
                   ('IT06', N'Signed', N'OffPast', '1990-01-01', @Greek),
                   ('IT07', N'Signed', N'OffFuture', '1990-01-01', @Greek);

            INSERT INTO dbo.CrewServiceHistory (CrewMemberId, ShipId, RankId, SignOnDate, SignOffDate, EndOfContractDate)
            VALUES ('IT01', @ShipId, @AB, '2030-01-01', NULL,         '2030-06-15'),
                   ('IT02', @ShipId, @AB, '2030-01-01', NULL,         '2030-05-16'),
                   ('IT03', @ShipId, @AB, '2030-01-01', NULL,         '2030-05-15'),
                   ('IT04', @ShipId, @AB, '2030-06-15', NULL,         '2030-12-15'),
                   ('IT05', @ShipId, @AB, '2030-06-16', NULL,         '2030-12-16'),
                   ('IT06', @ShipId, @AB, '2030-01-01', '2030-06-01', '2030-06-01'),
                   ('IT07', @ShipId, @AB, '2030-01-01', '2030-07-01', '2030-07-01');
        END
        """);

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Status_rules_are_applied_at_each_date_boundary()
    {
        var (rows, total) = await db.CrewListAsync(Ship, AsOf, sortBy: "CrewMemberId");

        Assert.Equal(4, total);
        Assert.Equal(
            [("IT01", "Onboard"), ("IT02", "Onboard"), ("IT03", "Relief Due"), ("IT04", "Onboard")],
            rows.Select(r => (r.CrewMemberId, r.Status)));
    }

    [Fact]
    public async Task Planned_and_signed_off_crew_are_excluded_even_with_future_sign_off()
    {
        var (rows, _) = await db.CrewListAsync(Ship, AsOf);

        Assert.DoesNotContain(rows, r => r.CrewMemberId is "IT05" or "IT06" or "IT07");
    }

    [Theory]
    [InlineData("2030-07-29", "IT01", 49)]
    [InlineData("2030-07-30", "IT01", 50)]
    [InlineData("2030-02-27", "IT02", 29)]
    [InlineData("2030-02-28", "IT02", 30)]
    public async Task Age_is_counted_in_completed_years(string asOf, string crewId, int expectedAge)
    {
        var (rows, _) = await db.CrewListAsync(Ship, DateTime.Parse(asOf));

        Assert.Equal(expectedAge, rows.Single(r => r.CrewMemberId == crewId).Age);
    }

    [Fact]
    public async Task Readme_sample_crew_are_relief_due_today_and_onboard_in_may_2025()
    {
        var (today, _) = await db.CrewListAsync("SHIP01", search: "Philip");
        var (may2025, _) = await db.CrewListAsync("SHIP01", new DateTime(2025, 5, 1), search: "Philip");

        Assert.All(today, r => Assert.Equal("Relief Due", r.Status));
        Assert.All(may2025, r => Assert.Equal("Onboard", r.Status));
        Assert.Equal(["CREW001", "CREW002"], may2025.Select(r => r.CrewMemberId).Order());
    }

    [Fact]
    public async Task Search_matches_partial_display_date()
    {
        var (rows, total) = await db.CrewListAsync("SHIP01", search: "05 Apr");

        Assert.Equal(1, total);
        Assert.Equal("CREW001", rows[0].CrewMemberId);
        Assert.Equal("05 Apr 2025", rows[0].SignOnDateDisplay);
    }

    [Theory]
    [InlineData("Greek")]
    [InlineData("Master")]
    [InlineData("CREW00")]
    [InlineData("phil")]
    public async Task Search_matches_text_columns_case_insensitively(string term)
    {
        var (rows, _) = await db.CrewListAsync("SHIP01", search: term);

        Assert.NotEmpty(rows);
    }

    [Theory]
    [InlineData("%")]
    [InlineData("_")]
    [InlineData("[A-Z]")]
    [InlineData("'; DROP TABLE dbo.Ship; --")]
    public async Task Search_treats_input_as_literal_text(string term)
    {
        var (rows, total) = await db.CrewListAsync("SHIP01", search: term);

        Assert.Empty(rows);
        Assert.Equal(0, total);
        Assert.True(await db.ScalarAsync<int>("SELECT COUNT(*) FROM dbo.Ship") >= 6);
    }

    [Fact]
    public async Task Paging_returns_requested_slice_and_full_total()
    {
        var (all, total) = await db.CrewListAsync("SHIP01", pageSize: 100);
        var (page2, total2) = await db.CrewListAsync("SHIP01", pageNumber: 2, pageSize: 5);
        var (beyond, total3) = await db.CrewListAsync("SHIP01", pageNumber: 99, pageSize: 5);

        Assert.Equal(all.Count, total);
        Assert.Equal(total, total2);
        Assert.Equal(total, total3);
        Assert.Equal(all.Skip(5).Take(5).Select(r => r.CrewMemberId), page2.Select(r => r.CrewMemberId));
        Assert.Empty(beyond);
    }

    [Theory]
    [InlineData("RankName")]
    [InlineData("CrewMemberId")]
    [InlineData("FirstName")]
    [InlineData("LastName")]
    [InlineData("Age")]
    [InlineData("Nationality")]
    [InlineData("SignOnDate")]
    [InlineData("Status")]
    public async Task Every_documented_column_sorts_in_both_directions(string column)
    {
        var (ascending, _) = await db.CrewListAsync("SHIP01", sortBy: column, sortDirection: "ASC");
        var (descending, _) = await db.CrewListAsync("SHIP01", sortBy: column, sortDirection: "DESC");

        var ascKeys = ascending.Select(r => SortKey(r, column)).ToList();
        var descKeys = descending.Select(r => SortKey(r, column)).ToList();
        Assert.Equal(ascKeys.Order(Comparer<IComparable>.Default), ascKeys);
        Assert.Equal(descKeys.OrderDescending(Comparer<IComparable>.Default), descKeys);
    }

    [Fact]
    public async Task Default_order_follows_rank_seniority()
    {
        var (rows, _) = await db.CrewListAsync("SHIP01", pageSize: 3);

        Assert.Equal(["Master", "Chief Engineer", "Chief Officer"], rows.Select(r => r.RankName));
    }

    [Theory]
    [InlineData("SHIP05", 50422)]
    [InlineData("NOPE", 50404)]
    public async Task Inactive_or_unknown_ship_is_rejected(string shipCode, int expectedError)
    {
        var error = await db.ProcedureErrorAsync("dbo.usp_Crew_ListByShip", new { ShipCode = shipCode });

        Assert.Equal(expectedError, error.Number);
    }

    [Theory]
    [InlineData("Salary", "ASC", 1, 20)]
    [InlineData("RankName; DROP TABLE dbo.Ship", "ASC", 1, 20)]
    [InlineData(null, "SIDEWAYS", 1, 20)]
    [InlineData(null, "ASC", 0, 20)]
    [InlineData(null, "ASC", 1, 101)]
    public async Task Invalid_paging_or_sorting_is_rejected(string? sortBy, string direction, int pageNumber, int pageSize)
    {
        var error = await db.ProcedureErrorAsync("dbo.usp_Crew_ListByShip", new
        {
            ShipCode = "SHIP01",
            SortBy = sortBy,
            SortDirection = direction,
            PageNumber = pageNumber,
            PageSize = pageSize
        });

        Assert.Equal(50400, error.Number);
    }

    private static IComparable SortKey(CrewRow row, string column) => column switch
    {
        "RankName" => row.RankName.ToUpperInvariant(),
        "CrewMemberId" => row.CrewMemberId.ToUpperInvariant(),
        "FirstName" => row.FirstName.ToUpperInvariant(),
        "LastName" => row.LastName.ToUpperInvariant(),
        "Age" => row.Age,
        "Nationality" => row.Nationality.ToUpperInvariant(),
        "SignOnDate" => row.SignOnDate,
        "Status" => row.Status.ToUpperInvariant(),
        _ => throw new ArgumentOutOfRangeException(nameof(column))
    };
}
