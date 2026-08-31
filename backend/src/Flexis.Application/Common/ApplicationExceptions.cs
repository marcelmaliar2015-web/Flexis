namespace Flexis.Application.Common;

public sealed class AuthenticationFailedException : Exception
{
    public AuthenticationFailedException(string message)
        : base(message)
    {
    }
}

public sealed class NotFoundException : Exception
{
    public NotFoundException(string message)
        : base(message)
    {
    }
}

public sealed class ConflictException : Exception
{
    public ConflictException(string message)
        : base(message)
    {
    }
}

public sealed class ValidationFailedException : Exception
{
    public ValidationFailedException(string message)
        : base(message)
    {
    }
}

public sealed class DomainRuleException : Exception
{
    public DomainRuleException(string message)
        : base(message)
    {
    }
}

public sealed class GoogleOAuthException : Exception
{
    public GoogleOAuthException(string message)
        : base(message)
    {
    }
}

public sealed class MicrosoftOAuthException : Exception
{
    public MicrosoftOAuthException(string message)
        : base(message)
    {
    }
}
