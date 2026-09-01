using System.Security.Claims;
using Flexis.Application.Common;
using Flexis.Application.JobApplication;
using Microsoft.AspNetCore.Mvc;

namespace Flexis.Api.Controllers;

[ApiController]
[Route("api/job-application/resume")]
public sealed class JobResumeController : ControllerBase
{
    [HttpGet]
    public Task<JobResumeBoardDto> Get(
        [FromServices] JobResumeService resume,
        CancellationToken cancellationToken)
    {
        return resume.GetBoardAsync(CurrentUserId(), cancellationToken);
    }

    [HttpPut("owner-options")]
    public Task<JobResumeBoardDto> UpdateOwnerOptions(
        [FromBody] JobResumeOwnerOptionsWriteRequest request,
        [FromServices] JobResumeService resume,
        CancellationToken cancellationToken)
    {
        return resume.UpdateOwnerOptionsAsync(CurrentUserId(), request, cancellationToken);
    }

    [HttpPut("profiles/{profileId:guid}")]
    public Task<JobResumeBoardDto> UpdateProfileResume(
        Guid profileId,
        [FromBody] JobResumeProfileWriteRequest request,
        [FromServices] JobResumeService resume,
        CancellationToken cancellationToken)
    {
        return resume.UpdateProfileResumeAsync(CurrentUserId(), profileId, request, cancellationToken);
    }

    private Guid CurrentUserId()
    {
        var subject = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new AuthenticationFailedException("Session is no longer valid.");
        return Guid.Parse(subject);
    }
}
