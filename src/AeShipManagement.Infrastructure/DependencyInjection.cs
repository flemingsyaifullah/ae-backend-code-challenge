using AeShipManagement.Application.Crew;
using AeShipManagement.Application.FinancialReports;
using AeShipManagement.Application.Ships;
using AeShipManagement.Application.Users;
using AeShipManagement.Infrastructure.Data;
using AeShipManagement.Infrastructure.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace AeShipManagement.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<DatabaseOptions>()
            .Bind(configuration.GetSection(DatabaseOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<ISqlConnectionFactory, SqlConnectionFactory>();
        services.AddSingleton<IStoredProcedureExecutor, StoredProcedureExecutor>();

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ShipRepository>();
        services.AddScoped<IShipRepository>(sp => sp.GetRequiredService<ShipRepository>());
        services.AddScoped<IShipAssignmentRepository>(sp => sp.GetRequiredService<ShipRepository>());
        services.AddScoped<ICrewRepository, CrewRepository>();
        services.AddScoped<IFinancialReportRepository, FinancialReportRepository>();

        services.AddHealthChecks().AddCheck<DatabaseHealthCheck>("database", HealthStatus.Unhealthy, ["ready"]);

        return services;
    }
}
