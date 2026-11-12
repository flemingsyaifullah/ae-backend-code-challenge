using FluentValidation;

namespace AeShipManagement.Application.Common;

public static class ValidationRules
{
    public const int ShipCodeMaxLength = 10;
    private const string CodePattern = "^[A-Za-z0-9_-]+$";

    public static IRuleBuilderOptions<T, string?> ValidShipCode<T>(this IRuleBuilder<T, string?> rule) =>
        rule.NotEmpty().WithMessage("Ship code is required.")
            .MaximumLength(ShipCodeMaxLength).WithMessage($"Ship code must be at most {ShipCodeMaxLength} characters.")
            .Matches(CodePattern).WithMessage("Ship code may only contain letters, digits, '-' and '_'.");

    public static IRuleBuilderOptions<T, int> ValidPageNumber<T>(this IRuleBuilder<T, int> rule) =>
        rule.GreaterThanOrEqualTo(1).WithMessage("Page number must be 1 or greater.");

    public static IRuleBuilderOptions<T, int> ValidPageSize<T>(this IRuleBuilder<T, int> rule) =>
        rule.InclusiveBetween(1, Paging.MaxPageSize).WithMessage($"Page size must be between 1 and {Paging.MaxPageSize}.");
}
