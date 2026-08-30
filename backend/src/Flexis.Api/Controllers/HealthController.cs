using Flexis.Application.Health;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Flexis.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/health")]
public sealed class HealthController : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<HealthStatusDto>> Get(
        [FromServices] HealthCheckService healthChecks,
        CancellationToken cancellationToken)
    {
        var report = await healthChecks.CheckHealthAsync(cancellationToken);
        var body = new HealthStatusDto(
            report.Status.ToString(),
            report.Entries
                .Select(entry => new HealthCheckDto(
                    entry.Key,
                    entry.Value.Status.ToString(),
                    entry.Value.Description))
                .ToArray());

        if (report.Status == HealthStatus.Healthy)
        {
            return Ok(body);
        }

        return StatusCode(StatusCodes.Status503ServiceUnavailable, body);
    }
}
