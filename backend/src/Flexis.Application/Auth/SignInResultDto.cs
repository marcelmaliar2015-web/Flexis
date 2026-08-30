using Flexis.Application.Users;

namespace Flexis.Application.Auth;

public sealed record SignInResultDto(string AccessToken, UserDto User);
