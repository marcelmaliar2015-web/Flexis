using Flexis.Application.Common;
using Microsoft.AspNetCore.Diagnostics;

namespace Flexis.Api;

internal sealed class ApiExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (status, title) = exception switch
        {
            AuthenticationFailedException => (StatusCodes.Status401Unauthorized, exception.Message),
            ValidationFailedException => (StatusCodes.Status400BadRequest, exception.Message),
            NotFoundException => (StatusCodes.Status404NotFound, exception.Message),
            ConflictException => (StatusCodes.Status409Conflict, exception.Message),
            DomainRuleException => (StatusCodes.Status409Conflict, exception.Message),
            GoogleOAuthException => (StatusCodes.Status400BadRequest, exception.Message),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred.")
        };

        httpContext.Response.StatusCode = status;
        await httpContext.Response.WriteAsJsonAsync(
            new HttpValidationProblemDetails
            {
                Status = status,
                Title = title,
                Detail = title
            },
            cancellationToken);

        return true;
    }
}
