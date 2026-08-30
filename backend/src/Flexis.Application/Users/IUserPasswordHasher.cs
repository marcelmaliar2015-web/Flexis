namespace Flexis.Application.Users;

public interface IUserPasswordHasher
{
    string Hash(string password);

    bool Verify(string passwordHash, string password);
}
