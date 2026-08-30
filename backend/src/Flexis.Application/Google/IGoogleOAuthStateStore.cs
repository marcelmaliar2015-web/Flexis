namespace Flexis.Application.Google;

public interface IGoogleOAuthStateStore
{
    void Save(string state, GoogleOAuthPending pending, TimeSpan lifetime);

    bool TryTake(string state, out GoogleOAuthPending? pending);
}
