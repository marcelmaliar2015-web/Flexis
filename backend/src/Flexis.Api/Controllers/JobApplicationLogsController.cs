using System.Security.Claims;
using Flexis.Application.Common;
using Flexis.Application.JobApplication;
using Microsoft.AspNetCore.Mvc;

namespace Flexis.Api.Controllers;

[ApiController]
[Route("api/job-application/logs")]
public sealed class JobApplicationLogsController : ControllerBase
{
    [HttpGet]
    public Task<JobApplicationLogPageDto> List(
        [FromServices] JobApplicationLogService logs,
        [FromQuery] int page,
        [FromQuery] int pageSize,
        [FromQuery] string? category,
        [FromQuery] string? q,
        CancellationToken cancellationToken)
    {
        return logs.ListAsync(
            CurrentUserId(),
            new JobApplicationLogQuery(page, pageSize, category, q),
            cancellationToken);
    }

    private Guid CurrentUserId()
    {
        var subject = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new AuthenticationFailedException("Session is no longer valid.");
        return Guid.Parse(subject);
    }
}
