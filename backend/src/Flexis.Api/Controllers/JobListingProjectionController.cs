using System.Security.Claims;
using Flexis.Application.Common;
using Flexis.Application.JobApplication;
using Microsoft.AspNetCore.Mvc;

namespace Flexis.Api.Controllers;

[ApiController]
[Route("api/job-application/listings")]
public sealed class JobListingProjectionController : ControllerBase
{
    [HttpPost("sync")]
    public Task<JobListingSyncResultDto> SyncDirty(
        [FromServices] JobListingProjectionService projections,
        CancellationToken cancellationToken)
    {
        return projections.SyncDirtyAsync(CurrentUserId(), cancellationToken);
    }

    [HttpPost("reindex")]
    public Task<JobListingSyncResultDto> Reindex(
        [FromServices] JobListingProjectionService projections,
        CancellationToken cancellationToken)
    {
        return projections.ReindexAllAsync(CurrentUserId(), cancellationToken);
    }

    private Guid CurrentUserId()
    {
        var subject = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new AuthenticationFailedException("Session is no longer valid.");
        return Guid.Parse(subject);
    }
}
