using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;

namespace AeShipManagement.IntegrationTests.Infrastructure;

public sealed partial class DatabaseFixture : IAsyncLifetime
{
    public const string MasterConnectionVariable = "AE_TEST_SQL_CONNECTION";
    private const string DefaultMasterConnection =
        @"Server=localhost\SQLEXPRESS;Database=master;Integrated Security=true;TrustServerCertificate=true";

    private static readonly string[] Scripts =
        ["01_schema.sql", "02_functions_views.sql", "03_procedures.sql", "04_seed_reference.sql", "05_seed_sample.sql"];

    private readonly string _masterConnectionString =
        Environment.GetEnvironmentVariable(MasterConnectionVariable) is { Length: > 0 } configured
            ? configured
            : DefaultMasterConnection;

    public string DatabaseName { get; } = $"AeShipManagement_IT_{Guid.NewGuid():N}"[..36];

    public string ConnectionString => new SqlConnectionStringBuilder(_masterConnectionString)
    {
        InitialCatalog = DatabaseName
    }.ConnectionString;

    public async Task InitializeAsync()
    {
        await using (var master = new SqlConnection(_masterConnectionString))
        {
            await master.OpenAsync();
            await ExecuteAsync(master, $"CREATE DATABASE [{DatabaseName}]");
        }

        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();

        foreach (var script in Scripts)
        {
            var path = Path.Combine(AppContext.BaseDirectory, "Database", script);
            foreach (var batch in BatchSeparator().Split(await File.ReadAllTextAsync(path)))
            {
                if (!string.IsNullOrWhiteSpace(batch))
                {
                    await ExecuteAsync(connection, batch);
                }
            }
        }
    }

    public async Task DisposeAsync()
    {
        SqlConnection.ClearAllPools();
        await using var master = new SqlConnection(_masterConnectionString);
        await master.OpenAsync();
        await ExecuteAsync(master,
            $"ALTER DATABASE [{DatabaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{DatabaseName}];");
    }

    public async Task<SqlConnection> OpenAsync()
    {
        var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();
        return connection;
    }

    private static async Task ExecuteAsync(SqlConnection connection, string sql)
    {
        await using var command = new SqlCommand(sql, connection) { CommandTimeout = 300 };
        await command.ExecuteNonQueryAsync();
    }

    [GeneratedRegex(@"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase)]
    private static partial Regex BatchSeparator();
}

[CollectionDefinition(Name)]
public sealed class DatabaseCollection : ICollectionFixture<DatabaseFixture>
{
    public const string Name = "Database";
}
