using System.Security.Claims;
using Flexis.Application.Common;
using Flexis.Application.JobApplication;
using Microsoft.AspNetCore.Mvc;

namespace Flexis.Api.Controllers;

[ApiController]
[Route("api/job-application/search")]
public sealed class JobSearchController : ControllerBase
{
    [HttpGet]
    public Task<JobSearchMetaDto> GetMeta(
        [FromServices] JobListingSearchService search,
        CancellationToken cancellationToken)
    {
        return search.GetMetaAsync(CurrentUserId(), cancellationToken);
    }

    [HttpPost]
    public Task<JobSearchResultDto> Search(
        [FromBody] JobSearchRequest request,
        [FromServices] JobListingSearchService search,
        CancellationToken cancellationToken)
    {
        return search.SearchAsync(CurrentUserId(), request, cancellationToken);
    }

    private Guid CurrentUserId()
    {
        var subject = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new AuthenticationFailedException("Session is no longer valid.");
        return Guid.Parse(subject);
    }
}
