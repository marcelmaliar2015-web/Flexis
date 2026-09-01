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

    [HttpGet("profiles/{id:guid}/info")]
    public Task<ProfileInfoDto> GetProfileInfo(
        Guid id,
        [FromServices] JobCatalogService catalog,
        CancellationToken cancellationToken)
    {
        return catalog.GetProfileInfoAsync(CurrentUserId(), id, cancellationToken);
    }

    [HttpPut("profiles/{id:guid}/info")]
    public Task<ProfileInfoDto> UpdateProfileInfo(
        Guid id,
        [FromBody] ProfileInfoWriteRequest request,
        [FromServices] JobCatalogService catalog,
        CancellationToken cancellationToken)
    {
        return catalog.UpdateProfileInfoAsync(CurrentUserId(), id, request, cancellationToken);
    }

    [HttpGet("profiles/{id:guid}/banned-companies")]
    public Task<IReadOnlyList<ProfileBannedCompanyDto>> ListBannedCompanies(
        Guid id,
        [FromServices] JobCatalogService catalog,
        CancellationToken cancellationToken)
    {
        return catalog.ListBannedCompaniesAsync(CurrentUserId(), id, cancellationToken);
    }

    [HttpPost("profiles/{id:guid}/banned-companies")]
    public async Task<ActionResult<ProfileBannedCompanyDto>> CreateBannedCompany(
        Guid id,
        [FromBody] ProfileBannedCompanyWriteRequest request,
        [FromServices] JobCatalogService catalog,
        CancellationToken cancellationToken)
    {
        var created = await catalog.CreateBannedCompanyAsync(CurrentUserId(), id, request, cancellationToken);
        return Created($"/api/job-application/profiles/{id}/banned-companies/{created.Id}", created);
    }

    [HttpPut("profiles/{id:guid}/banned-companies/{companyId:guid}")]
    public Task<ProfileBannedCompanyDto> UpdateBannedCompany(
        Guid id,
        Guid companyId,
        [FromBody] ProfileBannedCompanyWriteRequest request,
        [FromServices] JobCatalogService catalog,
        CancellationToken cancellationToken)
    {
        return catalog.UpdateBannedCompanyAsync(CurrentUserId(), id, companyId, request, cancellationToken);
    }

    [HttpDelete("profiles/{id:guid}/banned-companies/{companyId:guid}")]
    public async Task<IActionResult> DeleteBannedCompany(
        Guid id,
        Guid companyId,
        [FromServices] JobCatalogService catalog,
        CancellationToken cancellationToken)
    {
        await catalog.DeleteBannedCompanyAsync(CurrentUserId(), id, companyId, cancellationToken);
        return NoContent();
    }

    [HttpGet("profiles/{id:guid}/banned-matches")]
    public Task<ProfileBannedMatchesDto> ListBannedMatches(
        Guid id,
        [FromServices] JobCatalogService catalog,
        CancellationToken cancellationToken)
    {
        return catalog.ListBannedMatchesAsync(CurrentUserId(), id, cancellationToken);
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

    [HttpGet("sources/{id:guid}/locations")]
    public Task<IReadOnlyList<SourceLocationDto>> ListLocations(
        Guid id,
        [FromServices] JobCatalogService catalog,
        CancellationToken cancellationToken)
    {
        return catalog.ListLocationsAsync(CurrentUserId(), id, cancellationToken);
    }

    [HttpPost("sources/{id:guid}/locations")]
    public async Task<ActionResult<SourceLocationDto>> AddLocation(
        Guid id,
        [FromBody] SourceLocationWriteRequest request,
        [FromServices] JobCatalogService catalog,
        CancellationToken cancellationToken)
    {
        var created = await catalog.AddLocationAsync(CurrentUserId(), id, request, cancellationToken);
        return Created($"/api/job-application/sources/{id}/locations/{created.SheetId}", created);
    }

    [HttpPut("sources/{id:guid}/locations/{sheetId:int}")]
    public Task<SourceLocationDto> RenameLocation(
        Guid id,
        int sheetId,
        [FromBody] SourceLocationWriteRequest request,
        [FromServices] JobCatalogService catalog,
        CancellationToken cancellationToken)
    {
        return catalog.RenameLocationAsync(CurrentUserId(), id, sheetId, request, cancellationToken);
    }

    [HttpDelete("sources/{id:guid}/locations/{sheetId:int}")]
    public async Task<IActionResult> DeleteLocation(
        Guid id,
        int sheetId,
        [FromServices] JobCatalogService catalog,
        CancellationToken cancellationToken)
    {
        await catalog.DeleteLocationAsync(CurrentUserId(), id, sheetId, cancellationToken);
        return NoContent();
    }

    private Guid CurrentUserId()
    {
        var subject = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new AuthenticationFailedException("Session is no longer valid.");
        return Guid.Parse(subject);
    }
}
