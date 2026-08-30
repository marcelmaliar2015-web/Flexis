using System.Security.Claims;
using Flexis.Application.Common;
using Flexis.Application.JobApplication;
using Flexis.Domain.JobApplication;
using Microsoft.AspNetCore.Mvc;

namespace Flexis.Api.Controllers;

[ApiController]
[Route("api/job-application")]
public sealed class JobCatalogController : ControllerBase
{
    [HttpGet("profiles")]
    public Task<IReadOnlyList<JobCatalogItemDto>> ListProfiles(
        [FromServices] JobCatalogService catalog,
        CancellationToken cancellationToken)
    {
        return catalog.ListAsync(CurrentUserId(), JobCatalogKind.Profile, cancellationToken);
    }

    [HttpPost("profiles")]
    public async Task<ActionResult<JobCatalogItemDto>> CreateProfile(
        [FromBody] JobCatalogWriteRequest request,
        [FromServices] JobCatalogService catalog,
        CancellationToken cancellationToken)
    {
        var created = await catalog.CreateAsync(
            CurrentUserId(),
            JobCatalogKind.Profile,
            request,
            cancellationToken);
        return Created($"/api/job-application/profiles/{created.Id}", created);
    }

    [HttpPut("profiles/{id:guid}")]
    public Task<JobCatalogItemDto> UpdateProfile(
        Guid id,
        [FromBody] JobCatalogWriteRequest request,
        [FromServices] JobCatalogService catalog,
        CancellationToken cancellationToken)
    {
        return catalog.UpdateAsync(CurrentUserId(), id, request, cancellationToken);
    }

    [HttpDelete("profiles/{id:guid}")]
    public async Task<IActionResult> DeleteProfile(
        Guid id,
        [FromServices] JobCatalogService catalog,
        CancellationToken cancellationToken)
    {
        await catalog.DeleteAsync(CurrentUserId(), id, cancellationToken);
        return NoContent();
    }

    [HttpGet("sources")]
    public Task<IReadOnlyList<JobCatalogItemDto>> ListSources(
        [FromServices] JobCatalogService catalog,
        CancellationToken cancellationToken)
    {
        return catalog.ListAsync(CurrentUserId(), JobCatalogKind.Source, cancellationToken);
    }

    [HttpPost("sources")]
    public async Task<ActionResult<JobCatalogItemDto>> CreateSource(
        [FromBody] JobCatalogWriteRequest request,
        [FromServices] JobCatalogService catalog,
        CancellationToken cancellationToken)
    {
        var created = await catalog.CreateAsync(
            CurrentUserId(),
            JobCatalogKind.Source,
            request,
            cancellationToken);
        return Created($"/api/job-application/sources/{created.Id}", created);
    }

    [HttpPut("sources/{id:guid}")]
    public Task<JobCatalogItemDto> UpdateSource(
        Guid id,
        [FromBody] JobCatalogWriteRequest request,
        [FromServices] JobCatalogService catalog,
        CancellationToken cancellationToken)
    {
        return catalog.UpdateAsync(CurrentUserId(), id, request, cancellationToken);
    }

    [HttpDelete("sources/{id:guid}")]
    public async Task<IActionResult> DeleteSource(
        Guid id,
        [FromServices] JobCatalogService catalog,
        CancellationToken cancellationToken)
    {
        await catalog.DeleteAsync(CurrentUserId(), id, cancellationToken);
        return NoContent();
    }

    private Guid CurrentUserId()
    {
        var subject = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new AuthenticationFailedException("Session is no longer valid.");
        return Guid.Parse(subject);
    }
}
