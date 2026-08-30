using Flexis.Application.Common;
using Flexis.Application.Users;

namespace Flexis.Application.Auth;

public sealed class AuthService
{
    private readonly IUserRepository _users;
    private readonly IUserPasswordHasher _passwordHasher;
    private readonly IAccessTokenIssuer _accessTokenIssuer;

    public AuthService(
        IUserRepository users,
        IUserPasswordHasher passwordHasher,
        IAccessTokenIssuer accessTokenIssuer)
    {
        _users = users;
        _passwordHasher = passwordHasher;
        _accessTokenIssuer = accessTokenIssuer;
    }

    public async Task<SignInResultDto> SignInAsync(
        SignInRequest request,
        CancellationToken cancellationToken)
    {
        var email = UserRules.NormalizeEmail(request.Email);
        var user = await _users.GetByEmailAsync(email, cancellationToken);
        if (user is null
            || !user.IsActive
            || !_passwordHasher.Verify(user.PasswordHash, request.Password))
        {
            throw new AuthenticationFailedException("Email or password is incorrect.");
        }

        return new SignInResultDto(_accessTokenIssuer.Issue(user), UserMapper.ToDto(user));
    }

    public async Task<UserDto> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await _users.GetByIdAsync(userId, cancellationToken);
        if (user is null || !user.IsActive)
        {
            throw new AuthenticationFailedException("Session is no longer valid.");
        }

        return UserMapper.ToDto(user);
    }
}
