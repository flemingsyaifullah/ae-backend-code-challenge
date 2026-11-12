using AeShipManagement.Application.Common;

namespace AeShipManagement.Application.Users;

public interface IUserRepository
{
    Task<UserResponse> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken);
    Task<UserResponse> GetByIdAsync(int userId, CancellationToken cancellationToken);
    Task<PagedResult<UserResponse>> ListAsync(ListUsersQuery query, CancellationToken cancellationToken);
    Task<IReadOnlyList<UserRoleResponse>> ListRolesAsync(CancellationToken cancellationToken);
}
