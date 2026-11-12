using AeShipManagement.Application.Crew;
using AeShipManagement.Application.FinancialReports;
using AeShipManagement.Application.Ships;
using AeShipManagement.Application.Users;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace AeShipManagement.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<CreateUserRequestValidator>(ServiceLifetime.Singleton);
        services.AddSingleton<ShipCodeValidator>();

        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IShipService, ShipService>();
        services.AddScoped<IShipAssignmentService, ShipAssignmentService>();
        services.AddScoped<ICrewService, CrewService>();
        services.AddScoped<IFinancialReportService, FinancialReportService>();

        return services;
    }
}
