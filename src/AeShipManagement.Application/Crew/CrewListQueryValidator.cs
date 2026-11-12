using AeShipManagement.Application.Common;
using FluentValidation;

namespace AeShipManagement.Application.Crew;

public sealed class CrewListQueryValidator : AbstractValidator<CrewListQuery>
{
    public const int SearchMaxLength = 100;

    public CrewListQueryValidator()
    {
        RuleFor(x => x.ShipCode).ValidShipCode();
        RuleFor(x => x.PageNumber).ValidPageNumber();
        RuleFor(x => x.PageSize).ValidPageSize();

        RuleFor(x => x.SortBy)
            .Must(value => string.IsNullOrWhiteSpace(value) || CrewSortColumns.Normalize(value) is not null)
            .WithMessage($"Sort column must be one of: {string.Join(", ", CrewSortColumns.All)}.");

        RuleFor(x => x.SortDirection)
            .Must(value => SortDirections.Normalize(value) is not null)
            .WithMessage("Sort direction must be 'asc' or 'desc'.");

        RuleFor(x => x.Search)
            .MaximumLength(SearchMaxLength)
            .WithMessage($"Search text must be at most {SearchMaxLength} characters.");
    }
}
