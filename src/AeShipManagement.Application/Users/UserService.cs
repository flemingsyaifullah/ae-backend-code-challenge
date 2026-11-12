using AeShipManagement.Application.Common;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace AeShipManagement.Application.Users;

public interface IUserService
{
    Task<UserResponse> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken);
    Task<UserResponse> GetByIdAsync(int userId, CancellationToken cancellationToken);
    Task<PagedResult<UserResponse>> ListAsync(ListUsersQuery query, CancellationToken cancellationToken);
    Task<IReadOnlyList<UserRoleResponse>> ListRolesAsync(CancellationToken cancellationToken);
}

public sealed class UserService(
    IUserRepository repository,
    IValidator<CreateUserRequest> createValidator,
    IValidator<ListUsersQuery> listValidator,
    ILogger<UserService> logger) : IUserService
{
    public async Task<UserResponse> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken)
    {
        await createValidator.ValidateAndThrowAsync(request, cancellationToken);
        var user = await repository.CreateAsync(request, cancellationToken);
        logger.LogInformation("User {UserId} created with role {RoleName}", user.UserId, user.RoleName);
        return user;
    }

    public Task<UserResponse> GetByIdAsync(int userId, CancellationToken cancellationToken)
    {
        UserIdRules.EnsureValid(userId);
        return repository.GetByIdAsync(userId, cancellationToken);
    }

    public async Task<PagedResult<UserResponse>> ListAsync(ListUsersQuery query, CancellationToken cancellationToken)
    {
        await listValidator.ValidateAndThrowAsync(query, cancellationToken);
        return await repository.ListAsync(query, cancellationToken);
    }

    public Task<IReadOnlyList<UserRoleResponse>> ListRolesAsync(CancellationToken cancellationToken) =>
        repository.ListRolesAsync(cancellationToken);
}
