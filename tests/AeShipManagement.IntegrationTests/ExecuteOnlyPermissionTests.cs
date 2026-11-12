using AeShipManagement.IntegrationTests.Infrastructure;
using Dapper;
using Microsoft.Data.SqlClient;

namespace AeShipManagement.IntegrationTests;

[Collection(DatabaseCollection.Name)]
[Trait("Category", "Integration")]
public sealed class ExecuteOnlyPermissionTests(DatabaseFixture db)
{
    private const string Setup = """
        IF USER_ID('ae_api_probe') IS NULL CREATE USER ae_api_probe WITHOUT LOGIN;
        GRANT EXECUTE ON SCHEMA::dbo TO ae_api_probe;
        """;

    [Fact]
    public async Task Execute_only_user_can_run_procedures()
    {
        await db.ExecuteSqlAsync(Setup);
        await using var connection = await db.OpenAsync();

        await using var grid = await connection.QueryMultipleAsync("""
            EXECUTE AS USER = 'ae_api_probe';
            DECLARE @Total INT;
            EXEC dbo.usp_Ship_List @PageSize = 1, @TotalCount = @Total OUTPUT;
            REVERT;
            SELECT @Total;
            """);

        var ships = (await grid.ReadAsync()).AsList();
        var total = await grid.ReadSingleAsync<int>();

        Assert.Single(ships);
        Assert.True(total >= 6);
    }

    [Theory]
    [InlineData("SELECT TOP 1 * FROM dbo.Ship")]
    [InlineData("UPDATE dbo.Ship SET ShipName = ShipName WHERE 1 = 0")]
    [InlineData("SELECT TOP 1 * FROM dbo.vw_AccountTree")]
    public async Task Execute_only_user_cannot_touch_tables_or_views_directly(string statement)
    {
        await db.ExecuteSqlAsync(Setup);
        await using var connection = await db.OpenAsync();

        var error = await Assert.ThrowsAsync<SqlException>(() =>
            connection.ExecuteAsync($"EXECUTE AS USER = 'ae_api_probe'; {statement}; REVERT;"));

        Assert.Equal(229, error.Number);
    }
}
