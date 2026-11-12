using Dapper;

namespace AeShipManagement.Infrastructure.Data;

internal static class SqlParameters
{
    public static DbString Varchar(string? value, int length) =>
        new() { Value = value, IsAnsi = true, IsFixedLength = false, Length = length };

    public static DbString Char(string? value, int length) =>
        new() { Value = value, IsAnsi = true, IsFixedLength = true, Length = length };

    public static DbString NVarchar(string? value, int length) =>
        new() { Value = value, IsAnsi = false, IsFixedLength = false, Length = length };

    public static DateTime ToDateTime(this DateOnly value) => value.ToDateTime(TimeOnly.MinValue);

    public static DateTime AsUtc(this DateTime value) => DateTime.SpecifyKind(value, DateTimeKind.Utc);
}
