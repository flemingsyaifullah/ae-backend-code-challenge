using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace AeShipManagement.Infrastructure.Data;

public interface IStoredProcedureExecutor
{
    Task<IReadOnlyList<T>> QueryAsync<T>(string procedure, object? parameters, CancellationToken cancellationToken);
    Task<T> QuerySingleAsync<T>(string procedure, object? parameters, CancellationToken cancellationToken);
    Task ExecuteAsync(string procedure, object? parameters, CancellationToken cancellationToken);
    Task<TResult> QueryMultipleAsync<TResult>(
        string procedure,
        object? parameters,
        Func<SqlMapper.GridReader, Task<TResult>> read,
        CancellationToken cancellationToken);
}

internal sealed class StoredProcedureExecutor(ISqlConnectionFactory connectionFactory, IOptions<DatabaseOptions> options)
    : IStoredProcedureExecutor
{
    public Task<IReadOnlyList<T>> QueryAsync<T>(string procedure, object? parameters, CancellationToken cancellationToken) =>
        RunAsync(async connection =>
            (IReadOnlyList<T>)(await connection.QueryAsync<T>(Command(procedure, parameters, cancellationToken))).AsList(),
            cancellationToken);

    public Task<T> QuerySingleAsync<T>(string procedure, object? parameters, CancellationToken cancellationToken) =>
        RunAsync(connection => connection.QuerySingleAsync<T>(Command(procedure, parameters, cancellationToken)), cancellationToken);

    public Task ExecuteAsync(string procedure, object? parameters, CancellationToken cancellationToken) =>
        RunAsync(connection => connection.ExecuteAsync(Command(procedure, parameters, cancellationToken)), cancellationToken);

    public Task<TResult> QueryMultipleAsync<TResult>(
        string procedure,
        object? parameters,
        Func<SqlMapper.GridReader, Task<TResult>> read,
        CancellationToken cancellationToken) =>
        RunAsync(async connection =>
        {
            await using var grid = await connection.QueryMultipleAsync(Command(procedure, parameters, cancellationToken));
            return await read(grid);
        }, cancellationToken);

    private CommandDefinition Command(string procedure, object? parameters, CancellationToken cancellationToken) =>
        new(procedure, parameters, commandType: CommandType.StoredProcedure,
            commandTimeout: options.Value.CommandTimeoutSeconds, cancellationToken: cancellationToken);

    private async Task<T> RunAsync<T>(Func<SqlConnection, Task<T>> action, CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = await connectionFactory.OpenAsync(cancellationToken);
            return await action(connection);
        }
        catch (SqlException ex) when (SqlErrorTranslator.Translate(ex.Number, ex.Message) is { } translated)
        {
            throw translated;
        }
    }
}
