using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AeShipManagement.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AeShipManagement.IntegrationTests;

[Collection(DatabaseCollection.Name)]
[Trait("Category", "Integration")]
public sealed class ApiEndToEndTests : IDisposable
{
    private const string ApiKey = "integration-test-key";
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public ApiEndToEndTests(DatabaseFixture db)
    {
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("Database:ConnectionString", db.ConnectionString);
            builder.UseSetting("Authentication:ApiKeys:0", ApiKey);
        });
        _client = _factory.CreateClient();
        _client.DefaultRequestHeaders.Add("X-Api-Key", ApiKey);
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [Fact]
    public async Task Health_endpoint_reports_database_healthy()
    {
        var response = await _factory.CreateClient().GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task User_ship_assignment_flow_works_end_to_end()
    {
        var email = $"flow.{Guid.NewGuid():N}@example.com";
        var created = await _client.PostAsJsonAsync("/api/v1/users", new { fullName = "Flow Tester", email, roleName = "Superintendent" });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var userId = (await Json(created)).GetProperty("userId").GetInt32();

        var fetched = await _client.GetAsync(created.Headers.Location);
        Assert.Equal(HttpStatusCode.OK, fetched.StatusCode);

        var duplicate = await _client.PostAsJsonAsync("/api/v1/users", new { fullName = "Again", email = email.ToUpperInvariant(), roleName = "Viewer" });
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);

        Assert.Equal(HttpStatusCode.NoContent, (await _client.PutAsync($"/api/v1/users/{userId}/ships/SHIP01", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await _client.PutAsync($"/api/v1/users/{userId}/ships/SHIP01", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await _client.PutAsync($"/api/v1/users/{userId}/ships/SHIP05", null)).StatusCode);

        var ships = await Json(await _client.GetAsync($"/api/v1/users/{userId}/ships"));
        Assert.Equal(["SHIP01", "SHIP05"], ships.EnumerateArray().Select(s => s.GetProperty("shipCode").GetString()));

        Assert.Equal(HttpStatusCode.NoContent, (await _client.DeleteAsync($"/api/v1/users/{userId}/ships/SHIP05")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.DeleteAsync($"/api/v1/users/{userId}/ships/SHIP05")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.PutAsync($"/api/v1/users/{userId}/ships/NOPE", null)).StatusCode);
    }

    [Fact]
    public async Task Ship_can_be_created_and_listed_by_status()
    {
        var code = $"E2E{Random.Shared.Next(10000, 99999)}";
        var created = await _client.PostAsJsonAsync("/api/v1/ships",
            new { shipCode = code.ToLowerInvariant(), shipName = "End To End", fiscalYearCode = "0403", status = "inactive" });

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var body = await Json(created);
        Assert.Equal(code, body.GetProperty("shipCode").GetString());
        Assert.Equal("Inactive", body.GetProperty("status").GetString());

        var inactive = await Json(await _client.GetAsync("/api/v1/ships?status=Inactive&pageSize=100"));
        Assert.Contains(inactive.GetProperty("items").EnumerateArray(), s => s.GetProperty("shipCode").GetString() == code);
        Assert.All(inactive.GetProperty("items").EnumerateArray(), s => Assert.Equal("Inactive", s.GetProperty("status").GetString()));

        var unknownFiscalYear = await _client.PostAsJsonAsync("/api/v1/ships",
            new { shipCode = "E2EFY", shipName = "Bad FY", fiscalYearCode = "0201" });
        Assert.Equal(HttpStatusCode.BadRequest, unknownFiscalYear.StatusCode);
    }

    [Fact]
    public async Task Crew_list_supports_paging_sorting_and_search()
    {
        var page = await Json(await _client.GetAsync("/api/v1/ships/SHIP01/crew?pageNumber=1&pageSize=5&sortBy=age&sortDirection=desc"));

        Assert.Equal(5, page.GetProperty("items").GetArrayLength());
        Assert.True(page.GetProperty("totalCount").GetInt32() >= 20);
        var ages = page.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("age").GetInt32()).ToList();
        Assert.Equal(ages.OrderDescending(), ages);

        var search = await Json(await _client.GetAsync("/api/v1/ships/SHIP01/crew?search=05%20Apr"));
        var match = Assert.Single(search.GetProperty("items").EnumerateArray());
        Assert.Equal("CREW001", match.GetProperty("crewMemberId").GetString());
        Assert.Equal("2025-04-05", match.GetProperty("signOnDate").GetString());
    }

    [Fact]
    public async Task Crew_list_for_inactive_ship_returns_422()
    {
        var response = await _client.GetAsync("/api/v1/ships/SHIP05/crew");

        Assert.Equal((HttpStatusCode)422, response.StatusCode);
        Assert.Contains("inactive", (await Json(response)).GetProperty("detail").GetString());
    }

    [Fact]
    public async Task Financial_report_detail_returns_fiscal_year_ytd_and_nulls()
    {
        var report = await Json(await _client.GetAsync("/api/v1/ships/SHIP02/financial-reports/detail?period=2025-02"));

        Assert.Equal("2024-04", report.GetProperty("ytdFrom").GetString());
        Assert.Equal("2025-02", report.GetProperty("ytdTo").GetString());

        var ship01 = await Json(await _client.GetAsync("/api/v1/ships/SHIP01/financial-reports/detail?period=2025-07"));
        var awards = ship01.GetProperty("rows").EnumerateArray().Single(r => r.GetProperty("accountNumber").GetString() == "7120000");
        Assert.Equal(JsonValueKind.Null, awards.GetProperty("actual").ValueKind);
        Assert.Equal(1000m, awards.GetProperty("budget").GetDecimal());
        Assert.Equal(-100m, awards.GetProperty("ytdVariance").GetDecimal());
    }

    [Fact]
    public async Task Financial_report_summary_returns_parent_accounts_only()
    {
        var report = await Json(await _client.GetAsync("/api/v1/ships/SHIP01/financial-reports/summary?period=2025-07"));

        Assert.Equal("Summary", report.GetProperty("reportType").GetString());
        Assert.All(report.GetProperty("rows").EnumerateArray(), r => Assert.Equal("Parent", r.GetProperty("accountType").GetString()));
    }

    [Theory]
    [InlineData("/api/v1/ships/NOPE/financial-reports/detail?period=2025-07", HttpStatusCode.NotFound)]
    [InlineData("/api/v1/ships/SHIP01/financial-reports/detail?period=July", HttpStatusCode.BadRequest)]
    [InlineData("/api/v1/users/0", HttpStatusCode.BadRequest)]
    [InlineData("/api/v1/users/999999", HttpStatusCode.NotFound)]
    [InlineData("/api/v1/ships/WAY_TOO_LONG_CODE", HttpStatusCode.BadRequest)]
    public async Task Error_responses_use_meaningful_status_codes(string url, HttpStatusCode expected)
    {
        var response = await _client.GetAsync(url);

        Assert.Equal(expected, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    private static async Task<JsonElement> Json(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
}
