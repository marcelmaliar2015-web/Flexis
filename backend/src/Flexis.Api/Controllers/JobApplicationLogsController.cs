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
    public Task<IReadOnlyList<JobApplicationLogDto>> List(
        [FromServices] JobApplicationLogService logs,
        CancellationToken cancellationToken)
    {
        return logs.ListAsync(CurrentUserId(), cancellationToken);
    }

    private Guid CurrentUserId()
    {
        var subject = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new AuthenticationFailedException("Session is no longer valid.");
        return Guid.Parse(subject);
    }
}
