using Flexis.Application.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Flexis.Api.Controllers;

[ApiController]
[Route("api/diagnostics/events")]
public sealed class DiagnosticsController : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Write(
        [FromBody] DiagnosticsEventRequest request,
        [FromServices] IIssueLog log,
        CancellationToken cancellationToken)
    {
        var severity = request.Severity.Trim().ToLowerInvariant() == "warning" ? "warning" : "error";
        var source = string.IsNullOrWhiteSpace(request.Source) ? "client" : request.Source.Trim();
        var message = (request.Message ?? string.Empty).Trim();
        if (message.Length == 0)
        {
            return NoContent();
        }

        var loggedMessage = message.Length <= 1000 ? message : message[..1000];

        await log.WriteAsync(
            new IssueLogEntry(
                DateTimeOffset.UtcNow,
                severity,
                source,
                loggedMessage,
                Truncate(request.Method, 16),
                Truncate(request.Path, 300),
                request.Status,
                Truncate(request.Detail, 4000),
                null),
            cancellationToken);
        return NoContent();
    }

    private static string? Truncate(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= max ? trimmed : trimmed[..max];
    }
}
