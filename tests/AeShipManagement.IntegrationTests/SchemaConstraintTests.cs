using AeShipManagement.IntegrationTests.Infrastructure;
using Microsoft.Data.SqlClient;

namespace AeShipManagement.IntegrationTests;

[Collection(DatabaseCollection.Name)]
[Trait("Category", "Integration")]
public sealed class SchemaConstraintTests(DatabaseFixture db)
{
    [Theory]
    [InlineData("INSERT INTO dbo.BudgetEntry (ShipId, AccountNumber, AccountPeriod, BudgetValue) SELECT ShipId, '7135000', '2025-07-01', -1 FROM dbo.Ship WHERE ShipCode = 'SHIP01'")]
    [InlineData("INSERT INTO dbo.AccountTransaction (ShipId, AccountNumber, AccountPeriod, ActualValue) SELECT ShipId, '7135000', '2025-07-01', -0.01 FROM dbo.Ship WHERE ShipCode = 'SHIP01'")]
    public async Task Negative_amounts_are_rejected(string sql)
    {
        var error = await Assert.ThrowsAsync<SqlException>(() => db.ExecuteSqlAsync(sql));

        Assert.Equal(547, error.Number);
        Assert.Contains("CK_", error.Message);
    }

    [Theory]
    [InlineData("INSERT INTO dbo.BudgetEntry (ShipId, AccountNumber, AccountPeriod, BudgetValue) SELECT ShipId, '7100000', '2025-07-01', 10 FROM dbo.Ship WHERE ShipCode = 'SHIP01'")]
    [InlineData("INSERT INTO dbo.AccountTransaction (ShipId, AccountNumber, AccountPeriod, ActualValue) SELECT ShipId, '7000000', '2025-07-01', 10 FROM dbo.Ship WHERE ShipCode = 'SHIP01'")]
    public async Task Values_cannot_be_recorded_on_parent_accounts(string sql)
    {
        var error = await Assert.ThrowsAsync<SqlException>(() => db.ExecuteSqlAsync(sql));

        Assert.Equal(547, error.Number);
        Assert.Contains("ChildAccount", error.Message);
    }

    [Fact]
    public async Task A_child_account_cannot_become_a_parent()
    {
        var error = await Assert.ThrowsAsync<SqlException>(() => db.ExecuteSqlAsync(
            "INSERT INTO dbo.Account (AccountNumber, AccountName, AccountType, ParentAccountNumber) VALUES ('7135100', N'X', 'C', '7135000')"));

        Assert.Contains("FK_Account_Parent", error.Message);
    }

    [Theory]
    [InlineData("INSERT INTO dbo.BudgetEntry (ShipId, AccountNumber, AccountPeriod, BudgetValue) SELECT ShipId, '7135000', '2025-07-15', 10 FROM dbo.Ship WHERE ShipCode = 'SHIP01'", "CK_BudgetEntry_Period")]
    [InlineData("INSERT INTO dbo.FiscalYearType (FiscalYearCode, Description) VALUES ('0412', N'Invalid')", "CK_FiscalYearType_Code")]
    [InlineData("INSERT INTO dbo.Ship (ShipCode, ShipName, FiscalYearCode, Status) VALUES ('BAD01', N'X', '0112', 'Docked')", "CK_Ship_Status")]
    public async Task Check_constraints_guard_domain_rules(string sql, string constraint)
    {
        var error = await Assert.ThrowsAsync<SqlException>(() => db.ExecuteSqlAsync(sql));

        Assert.Contains(constraint, error.Message);
    }

    [Fact]
    public async Task Sign_off_before_sign_on_is_rejected()
    {
        var error = await Assert.ThrowsAsync<SqlException>(() => db.ExecuteSqlAsync("""
            INSERT INTO dbo.CrewServiceHistory (CrewMemberId, ShipId, RankId, SignOnDate, SignOffDate, EndOfContractDate)
            SELECT 'CREW001', s.ShipId, r.RankId, '2025-05-01', '2025-04-01', '2025-08-01'
            FROM dbo.Ship s CROSS JOIN dbo.[Rank] r WHERE s.ShipCode = 'SHIP01' AND r.RankCode = 'MSTR'
            """));

        Assert.Contains("CK_CrewServiceHistory_SignOff", error.Message);
    }

    [Fact]
    public async Task Sample_data_meets_the_minimums_in_the_brief()
    {
        Assert.True(await db.ScalarAsync<int>("SELECT COUNT(*) FROM dbo.Ship WHERE ShipCode LIKE 'SHIP%'") >= 5);
        Assert.True(await db.ScalarAsync<int>("SELECT COUNT(DISTINCT Status) FROM dbo.Ship") == 2);
        Assert.True(await db.ScalarAsync<int>("SELECT COUNT(*) FROM dbo.[Rank]") >= 5);
        Assert.Equal(0, await db.ScalarAsync<int>("""
            SELECT COUNT(*) FROM dbo.Ship s
            WHERE s.ShipCode LIKE 'SHIP%'
              AND (SELECT COUNT(DISTINCT c.CrewMemberId) FROM dbo.CrewServiceHistory c WHERE c.ShipId = s.ShipId) < 20
            """));
        Assert.Equal(0, await db.ScalarAsync<int>("""
            SELECT COUNT(*) FROM dbo.Account p
            WHERE p.AccountType = 'P' AND (SELECT COUNT(*) FROM dbo.Account c WHERE c.ParentAccountNumber = p.AccountNumber) < 5
            """));
        Assert.True(await db.ScalarAsync<int>("SELECT COUNT(*) FROM dbo.Account WHERE AccountType = 'P'") >= 5);
        Assert.Equal(24, await db.ScalarAsync<int>("""
            SELECT MIN(n) FROM (SELECT COUNT(DISTINCT AccountPeriod) n FROM dbo.BudgetEntry GROUP BY ShipId) x
            """));
        Assert.Equal(3, await db.ScalarAsync<int>("SELECT COUNT(DISTINCT ShipId) FROM dbo.AccountTransaction"));
        Assert.True(await db.ScalarAsync<int>("""
            SELECT MIN(n) FROM (SELECT COUNT(DISTINCT AccountPeriod) n FROM dbo.AccountTransaction
                                WHERE YEAR(AccountPeriod) = 2025 GROUP BY ShipId) x
            """) >= 6);
    }
}
