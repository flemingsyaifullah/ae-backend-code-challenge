using AeShipManagement.Application.Common;

namespace AeShipManagement.Application.Crew;

public static class CrewSortColumns
{
    public static readonly IReadOnlyList<string> All =
        ["RankName", "CrewMemberId", "FirstName", "LastName", "Age", "Nationality", "SignOnDate", "Status"];

    public static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? null
            : All.FirstOrDefault(c => c.Equals(value.Trim(), StringComparison.OrdinalIgnoreCase));
}

public static class SortDirections
{
    public const string Ascending = "ASC";
    public const string Descending = "DESC";

    public static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? Ascending : value.Trim().ToUpperInvariant() switch
        {
            Ascending => Ascending,
            Descending => Descending,
            _ => null
        };
}

public sealed record CrewListQuery(
    string ShipCode,
    int PageNumber = Paging.DefaultPageNumber,
    int PageSize = Paging.DefaultPageSize,
    string? SortBy = null,
    string? SortDirection = null,
    string? Search = null);

public sealed record CrewMemberResponse(
    string RankName,
    string CrewMemberId,
    string FirstName,
    string LastName,
    int Age,
    string Nationality,
    DateOnly SignOnDate,
    string SignOnDateDisplay,
    DateOnly EndOfContractDate,
    string Status);
