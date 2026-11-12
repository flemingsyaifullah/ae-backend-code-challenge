using AeShipManagement.Application.Common;
using FluentValidation;

namespace AeShipManagement.Application.Ships;

public sealed class CreateShipRequestValidator : AbstractValidator<CreateShipRequest>
{
    public CreateShipRequestValidator()
    {
        RuleFor(x => x.ShipCode).ValidShipCode();

        RuleFor(x => x.ShipName)
            .NotEmpty().WithMessage("Ship name is required.")
            .MaximumLength(100).WithMessage("Ship name must be at most 100 characters.");

        RuleFor(x => x.FiscalYearCode)
            .NotEmpty().WithMessage("Fiscal year code is required.")
            .Matches("^(0[1-9]|1[0-2])(0[1-9]|1[0-2])$")
            .WithMessage("Fiscal year code must be 4 digits: start month and end month, e.g. '0112' or '0403'.");

        RuleFor(x => x.Status)
            .Must(ShipStatusRules.IsValidOrEmpty)
            .WithMessage("Status must be 'Active' or 'Inactive'.");
    }
}

public sealed class ListShipsQueryValidator : AbstractValidator<ListShipsQuery>
{
    public ListShipsQueryValidator()
    {
        RuleFor(x => x.Status)
            .Must(ShipStatusRules.IsValidOrEmpty)
            .WithMessage("Status must be 'Active' or 'Inactive'.");
        RuleFor(x => x.PageNumber).ValidPageNumber();
        RuleFor(x => x.PageSize).ValidPageSize();
    }
}

public sealed class ShipCodeValidator : AbstractValidator<string?>
{
    public ShipCodeValidator()
    {
        RuleFor(x => x).ValidShipCode().OverridePropertyName("shipCode");
    }
}

internal static class ShipStatusRules
{
    public static bool IsValidOrEmpty(string? status) =>
        string.IsNullOrWhiteSpace(status) || ShipStatuses.All.Contains(status.Trim(), StringComparer.OrdinalIgnoreCase);

    public static string? Normalize(string? status) =>
        string.IsNullOrWhiteSpace(status)
            ? null
            : ShipStatuses.All.First(s => s.Equals(status.Trim(), StringComparison.OrdinalIgnoreCase));
}
