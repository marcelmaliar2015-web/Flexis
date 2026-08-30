using Flexis.Domain.Users;

namespace Flexis.Application.Users;

public sealed record UserDto(
    Guid Id,
    string Email,
    string DisplayName,
    UserRole Role,
    bool IsActive,
    DateTimeOffset CreatedAt);
