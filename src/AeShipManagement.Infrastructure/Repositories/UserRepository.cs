using System.Data;
using AeShipManagement.Application.Common;
using AeShipManagement.Application.Users;
using AeShipManagement.Infrastructure.Data;
using Dapper;

namespace AeShipManagement.Infrastructure.Repositories;

internal sealed class UserRepository(IStoredProcedureExecutor executor) : IUserRepository
{
    public async Task<UserResponse> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken)
    {
        var row = await executor.QuerySingleAsync<UserRow>(StoredProcedures.UserCreate, new
        {
            FullName = SqlParameters.NVarchar(request.FullName, 100),
            Email = SqlParameters.Varchar(request.Email, 254),
            RoleName = SqlParameters.Varchar(request.RoleName, 30)
        }, cancellationToken);
        return row.ToResponse();
    }

    public async Task<UserResponse> GetByIdAsync(int userId, CancellationToken cancellationToken)
    {
        var row = await executor.QuerySingleAsync<UserRow>(StoredProcedures.UserGetById, new { UserId = userId }, cancellationToken);
        return row.ToResponse();
    }

    public async Task<PagedResult<UserResponse>> ListAsync(ListUsersQuery query, CancellationToken cancellationToken)
    {
        var parameters = new DynamicParameters();
        parameters.Add("PageNumber", query.PageNumber);
        parameters.Add("PageSize", query.PageSize);
        parameters.Add("TotalCount", dbType: DbType.Int32, direction: ParameterDirection.Output);

        var rows = await executor.QueryAsync<UserRow>(StoredProcedures.UserList, parameters, cancellationToken);
        var items = rows.Select(r => r.ToResponse()).ToList();
        return new PagedResult<UserResponse>(items, query.PageNumber, query.PageSize, parameters.Get<int>("TotalCount"));
    }

    public async Task<IReadOnlyList<UserRoleResponse>> ListRolesAsync(CancellationToken cancellationToken)
    {
        var rows = await executor.QueryAsync<UserRoleRow>(StoredProcedures.UserRoleList, null, cancellationToken);
        return rows.Select(r => new UserRoleResponse(r.RoleId, r.RoleName)).ToList();
    }

    private sealed class UserRow
    {
        public int UserId { get; init; }
        public string FullName { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public string RoleName { get; init; } = string.Empty;
        public DateTime CreatedAt { get; init; }

        public UserResponse ToResponse() => new(UserId, FullName, Email, RoleName, CreatedAt.AsUtc());
    }

    private sealed class UserRoleRow
    {
        public byte RoleId { get; init; }
        public string RoleName { get; init; } = string.Empty;
    }
}
