using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AeShipManagement.Application.Common;
using AeShipManagement.Application.Crew;
using AeShipManagement.Application.FinancialReports;
using AeShipManagement.Application.Ships;
using AeShipManagement.Application.Users;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace AeShipManagement.UnitTests.Api;

public sealed class ApiPipelineTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task Request_without_api_key_returns_401_problem_details()
    {
        var response = await factory.CreateClient().GetAsync("/api/v1/ships");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Request_with_wrong_api_key_returns_401()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", "wrong-key");

        var response = await client.GetAsync("/api/v1/ships");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task OpenApi_document_is_public()
    {
        var response = await factory.CreateClient().GetAsync("/openapi/v1.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("X-Api-Key", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Create_user_returns_201_with_location()
    {
        factory.Users.CreateAsync(Arg.Any<CreateUserRequest>(), Arg.Any<CancellationToken>())
            .Returns(new UserResponse(42, "Jane Doe", "jane@example.com", "Viewer", DateTime.UtcNow));

        var response = await factory.CreateAuthorizedClient().PostAsJsonAsync("/api/v1/users",
            new { fullName = "Jane Doe", email = "jane@example.com", roleName = "Viewer" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.EndsWith("/api/v1/users/42", response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task Create_user_with_invalid_body_returns_400_with_field_errors()
    {
        var response = await factory.CreateAuthorizedClient().PostAsJsonAsync("/api/v1/users",
            new { fullName = "", email = "nope" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var errors = (await ReadJson(response)).GetProperty("errors");
        Assert.True(errors.TryGetProperty("fullName", out _));
        Assert.True(errors.TryGetProperty("email", out _));
        Assert.True(errors.TryGetProperty("roleName", out _));
    }

    [Fact]
    public async Task Malformed_json_returns_400()
    {
        var content = new StringContent("{ not json", System.Text.Encoding.UTF8, "application/json");

        var response = await factory.CreateAuthorizedClient().PostAsync("/api/v1/ships", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Duplicate_ship_returns_409_with_procedure_message()
    {
        factory.Ships.CreateAsync(Arg.Is<CreateShipRequest>(r => r.ShipCode == "DUP01"), Arg.Any<CancellationToken>())
            .ThrowsAsync(new ConflictException("A ship with code 'DUP01' already exists."));

        var response = await factory.CreateAuthorizedClient().PostAsJsonAsync("/api/v1/ships",
            new { shipCode = "DUP01", shipName = "Dup", fiscalYearCode = "0112" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var body = await ReadJson(response);
        Assert.Equal("A ship with code 'DUP01' already exists.", body.GetProperty("detail").GetString());
        Assert.True(body.TryGetProperty("traceId", out _));
    }

    [Fact]
    public async Task Unknown_user_returns_404()
    {
        factory.Users.GetByIdAsync(9999, Arg.Any<CancellationToken>())
            .ThrowsAsync(new ResourceNotFoundException("User 9999 was not found."));

        var response = await factory.CreateAuthorizedClient().GetAsync("/api/v1/users/9999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Assigning_a_ship_returns_204()
    {
        factory.Assignments.AssignAsync(5, "SHIP01", Arg.Any<CancellationToken>()).Returns(true);

        var response = await factory.CreateAuthorizedClient().PutAsync("/api/v1/users/5/ships/SHIP01", null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Crew_list_for_inactive_ship_returns_422()
    {
        factory.Crew.ListByShipAsync(Arg.Is<CrewListQuery>(q => q.ShipCode == "SHIP05"), Arg.Any<CancellationToken>())
            .ThrowsAsync(new BusinessRuleException("Ship 'SHIP05' is inactive."));

        var response = await factory.CreateAuthorizedClient().GetAsync("/api/v1/ships/SHIP05/crew");

        Assert.Equal((HttpStatusCode)422, response.StatusCode);
    }

    [Fact]
    public async Task Crew_list_passes_query_parameters_through()
    {
        factory.Crew.ListByShipAsync(Arg.Any<CrewListQuery>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<CrewMemberResponse>([], 2, 5, 0));

        var response = await factory.CreateAuthorizedClient()
            .GetAsync("/api/v1/ships/SHIP01/crew?pageNumber=2&pageSize=5&sortBy=age&sortDirection=desc&search=05%20Apr");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await factory.Crew.Received().ListByShipAsync(
            Arg.Is<CrewListQuery>(q => q.ShipCode == "SHIP01" && q.PageNumber == 2 && q.PageSize == 5
                                       && q.SortBy == "Age" && q.SortDirection == "DESC" && q.Search == "05 Apr"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Non_numeric_page_number_returns_400()
    {
        var response = await factory.CreateAuthorizedClient().GetAsync("/api/v1/ships?pageNumber=abc");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Financial_report_keeps_null_and_zero_distinct_in_json()
    {
        factory.Reports.GetAsync("SHIP01", new DateOnly(2025, 7, 1), FinancialReportType.Detail, Arg.Any<CancellationToken>())
            .Returns(new FinancialReportResponse("SHIP01", "Flying Dutchman", "0112", FinancialReportType.Detail,
                "2025-07", "2025-01", "2025-07",
                [new FinancialReportRow("7540000", "DRY DOCKING", "Child", 3, "7500000", null, 0m, 0m, 184000m, 180000m, 4000m)]));

        var response = await factory.CreateAuthorizedClient().GetAsync("/api/v1/ships/SHIP01/financial-reports/detail?period=2025-07");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await ReadJson(response);
        var row = body.GetProperty("rows")[0];
        Assert.Equal(JsonValueKind.Null, row.GetProperty("actual").ValueKind);
        Assert.Equal(0m, row.GetProperty("budget").GetDecimal());
        Assert.Equal("Detail", body.GetProperty("reportType").GetString());
    }

    [Fact]
    public async Task Financial_report_without_period_returns_400()
    {
        var response = await factory.CreateAuthorizedClient().GetAsync("/api/v1/ships/SHIP01/financial-reports/summary");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.True((await ReadJson(response)).GetProperty("errors").TryGetProperty("period", out _));
    }

    [Fact]
    public async Task Unexpected_failure_returns_500_without_internal_details()
    {
        factory.Users.ListRolesAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Login failed for user 'sa'"));

        var response = await factory.CreateAuthorizedClient().GetAsync("/api/v1/user-roles");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.DoesNotContain("Login failed", await response.Content.ReadAsStringAsync());
    }

    private static async Task<JsonElement> ReadJson(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
}
