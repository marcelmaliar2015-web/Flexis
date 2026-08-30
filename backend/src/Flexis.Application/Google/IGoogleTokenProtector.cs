namespace Flexis.Application.Google;

public interface IGoogleTokenProtector
{
    string Protect(string plaintext);

    string Unprotect(string protectedText);
}
