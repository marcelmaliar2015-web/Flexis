using Flexis.Application.Users;
using Flexis.Domain.Users;
using Microsoft.AspNetCore.Identity;

namespace Flexis.Infrastructure.Security;

internal sealed class AspNetUserPasswordHasher : IUserPasswordHasher
{
    private readonly PasswordHasher<User> _hasher = new();

    public string Hash(string password)
    {
        return _hasher.HashPassword(null!, password);
    }

    public bool Verify(string passwordHash, string password)
    {
        return _hasher.VerifyHashedPassword(null!, passwordHash, password)
            is not PasswordVerificationResult.Failed;
    }
}
