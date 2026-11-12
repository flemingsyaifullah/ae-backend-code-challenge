using AeShipManagement.Application.Crew;
using AeShipManagement.Application.FinancialReports;
using AeShipManagement.Application.Ships;
using AeShipManagement.Application.Users;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace AeShipManagement.UnitTests.Api;

public sealed class ApiFactory : WebApplicationFactory<Program>
{
    public const string ApiKey = "unit-test-key";

    public IUserRepository Users { get; } = Substitute.For<IUserRepository>();
    public IShipRepository Ships { get; } = Substitute.For<IShipRepository>();
    public IShipAssignmentRepository Assignments { get; } = Substitute.For<IShipAssignmentRepository>();
    public ICrewRepository Crew { get; } = Substitute.For<ICrewRepository>();
    public IFinancialReportRepository Reports { get; } = Substitute.For<IFinancialReportRepository>();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("Database:ConnectionString", "Server=unused;Database=unused");
        builder.UseSetting("Authentication:ApiKeys:0", ApiKey);
        builder.ConfigureTestServices(services =>
        {
            services.AddScoped(_ => Users);
            services.AddScoped(_ => Ships);
            services.AddScoped(_ => Assignments);
            services.AddScoped(_ => Crew);
            services.AddScoped(_ => Reports);
        });
    }

    public HttpClient CreateAuthorizedClient()
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", ApiKey);
        return client;
    }
}
