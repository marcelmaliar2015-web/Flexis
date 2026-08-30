using Flexis.Domain.Users;

namespace Flexis.Application.Users;

internal static class UserMapper
{
    public static UserDto ToDto(User user)
    {
        return new UserDto(
            user.Id,
            user.Email,
            user.DisplayName,
            user.Role,
            user.IsActive,
            user.CreatedAt);
    }
}
