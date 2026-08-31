namespace Flexis.Application.Microsoft;

public interface IMicrosoftOAuthStateStore
{
    void Save(string state, MicrosoftOAuthPending pending, TimeSpan lifetime);

    bool TryTake(string state, out MicrosoftOAuthPending? pending);
}
