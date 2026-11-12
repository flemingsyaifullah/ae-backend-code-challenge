using AeShipManagement.Application.Common;
using FluentValidation;

namespace AeShipManagement.Application.Crew;

public interface ICrewRepository
{
    Task<PagedResult<CrewMemberResponse>> ListByShipAsync(CrewListQuery query, CancellationToken cancellationToken);
}

public interface ICrewService
{
    Task<PagedResult<CrewMemberResponse>> ListByShipAsync(CrewListQuery query, CancellationToken cancellationToken);
}

public sealed class CrewService(ICrewRepository repository, IValidator<CrewListQuery> validator) : ICrewService
{
    public async Task<PagedResult<CrewMemberResponse>> ListByShipAsync(CrewListQuery query, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(query, cancellationToken);

        var normalized = query with
        {
            SortBy = CrewSortColumns.Normalize(query.SortBy),
            SortDirection = SortDirections.Normalize(query.SortDirection),
            Search = string.IsNullOrWhiteSpace(query.Search) ? null : query.Search.Trim()
        };

        return await repository.ListByShipAsync(normalized, cancellationToken);
    }
}
