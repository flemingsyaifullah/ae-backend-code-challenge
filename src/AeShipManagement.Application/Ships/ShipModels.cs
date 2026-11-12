using AeShipManagement.Application.Common;

namespace AeShipManagement.Application.Ships;

public static class ShipStatuses
{
    public const string Active = "Active";
    public const string Inactive = "Inactive";
    public static readonly IReadOnlyList<string> All = [Active, Inactive];
}

public sealed record ShipResponse(
    string ShipCode,
    string ShipName,
    string FiscalYearCode,
    string FiscalYearDescription,
    string Status,
    DateTime CreatedAt);

public sealed record AssignedShipResponse(
    string ShipCode,
    string ShipName,
    string FiscalYearCode,
    string FiscalYearDescription,
    string Status,
    DateTime AssignedAt);

public sealed record CreateShipRequest(string? ShipCode, string? ShipName, string? FiscalYearCode, string? Status);

public sealed record ListShipsQuery(
    string? Status = null,
    int PageNumber = Paging.DefaultPageNumber,
    int PageSize = Paging.DefaultPageSize);
