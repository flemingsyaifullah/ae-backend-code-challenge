using AeShipManagement.Application.Common;
using FluentValidation;

namespace AeShipManagement.Application.FinancialReports;

public interface IFinancialReportRepository
{
    Task<FinancialReportResponse> GetAsync(
        string shipCode,
        DateOnly accountingPeriod,
        FinancialReportType reportType,
        CancellationToken cancellationToken);
}

public interface IFinancialReportService
{
    Task<FinancialReportResponse> GetAsync(FinancialReportQuery query, CancellationToken cancellationToken);
}

public sealed class FinancialReportQueryValidator : AbstractValidator<FinancialReportQuery>
{
    public FinancialReportQueryValidator()
    {
        RuleFor(x => x.ShipCode).ValidShipCode();

        RuleFor(x => x.Period)
            .NotEmpty().WithMessage("Period is required, e.g. '2025-07'.");

        RuleFor(x => x.Period)
            .Must(value => AccountingPeriod.TryParse(value, out _))
            .When(x => !string.IsNullOrWhiteSpace(x.Period))
            .WithMessage("Period must be in yyyy-MM format, e.g. '2025-07'.");

        RuleFor(x => x.ReportType).IsInEnum();
    }
}

public sealed class FinancialReportService(
    IFinancialReportRepository repository,
    IValidator<FinancialReportQuery> validator) : IFinancialReportService
{
    public async Task<FinancialReportResponse> GetAsync(FinancialReportQuery query, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(query, cancellationToken);
        AccountingPeriod.TryParse(query.Period, out var period);
        return await repository.GetAsync(query.ShipCode, period, query.ReportType, cancellationToken);
    }
}
