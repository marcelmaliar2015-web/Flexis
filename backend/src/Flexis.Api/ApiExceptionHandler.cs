using Flexis.Application.Common;
using Flexis.Application.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;

namespace Flexis.Api;

internal sealed class ApiExceptionHandler : IExceptionHandler
{
    private readonly IIssueLog _issues;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<ApiExceptionHandler> _logger;

    public ApiExceptionHandler(
        IIssueLog issues,
        IHostEnvironment environment,
        ILogger<ApiExceptionHandler> logger)
    {
        _issues = issues;
        _environment = environment;
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (status, title, unexpected) = exception switch
        {
            AuthenticationFailedException => (StatusCodes.Status401Unauthorized, exception.Message, false),
            ValidationFailedException => (StatusCodes.Status400BadRequest, exception.Message, false),
            NotFoundException => (StatusCodes.Status404NotFound, exception.Message, false),
            ConflictException => (StatusCodes.Status409Conflict, exception.Message, false),
            DomainRuleException => (StatusCodes.Status409Conflict, exception.Message, false),
            GoogleOAuthException => (StatusCodes.Status400BadRequest, exception.Message, false),
            MicrosoftOAuthException => (StatusCodes.Status400BadRequest, exception.Message, false),
            _ => (
                StatusCodes.Status500InternalServerError,
                _environment.IsDevelopment() ? exception.Message : "An unexpected error occurred.",
                true)
        };

        try
        {
            await _issues.WriteAsync(
                new IssueLogEntry(
                    DateTimeOffset.UtcNow,
                    "error",
                    "api",
                    title,
                    httpContext.Request.Method,
                    httpContext.Request.Path.Value,
                    status,
                    title,
                    unexpected ? exception.ToString() : null),
                cancellationToken);
        }
        catch (Exception logError)
        {
            _logger.LogWarning(logError, "Issue log write failed.");
        }

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
