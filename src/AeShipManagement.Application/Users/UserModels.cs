using AeShipManagement.Application.Common;

namespace AeShipManagement.Application.Users;

public sealed record UserResponse(int UserId, string FullName, string Email, string RoleName, DateTime CreatedAt);

public sealed record UserRoleResponse(int RoleId, string RoleName);

public sealed record CreateUserRequest(string? FullName, string? Email, string? RoleName);

public sealed record ListUsersQuery(int PageNumber = Paging.DefaultPageNumber, int PageSize = Paging.DefaultPageSize);
