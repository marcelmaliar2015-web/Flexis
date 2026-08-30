using Flexis.Domain.Users;

namespace Flexis.Application.Auth;

public interface IAccessTokenIssuer
{
    string Issue(User user);
}
