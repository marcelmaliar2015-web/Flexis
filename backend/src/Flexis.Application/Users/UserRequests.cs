using Flexis.Domain.Users;

namespace Flexis.Application.Users;

public sealed record CreateUserRequest(
    string Email,
    string DisplayName,
    string Password,
    UserRole Role);

public sealed record UpdateUserRequest(
    string DisplayName,
    UserRole Role,
    bool IsActive,
    string? Password);

public sealed record UpdateCurrentUserRequest(
    string DisplayName,
    string? Password);
