using System.Security.Claims;
using Flexis.Application.Common;
using Flexis.Application.JobApplication;
using Microsoft.AspNetCore.Mvc;

namespace Flexis.Api.Controllers;

[ApiController]
[Route("api/job-application/pipeline")]
public sealed class JobPipelineController : ControllerBase
{
    [HttpGet]
    public Task<JobPipelineBoardDto> Get(
        [FromServices] JobPipelineService pipeline,
        CancellationToken cancellationToken)
    {
        return pipeline.GetBoardAsync(CurrentUserId(), cancellationToken);
    }

    [HttpPost]
    public async Task<ActionResult<JobPipelineEntryDto>> Create(
        [FromBody] JobPipelineWriteRequest request,
        [FromServices] JobPipelineService pipeline,
        CancellationToken cancellationToken)
    {
        var created = await pipeline.CreateAsync(CurrentUserId(), request, cancellationToken);
        return Created($"/api/job-application/pipeline/{created.Id}", created);
    }

    [HttpPut("{id:guid}")]
    public Task<JobPipelineEntryDto> Update(
        Guid id,
        [FromBody] JobPipelineWriteRequest request,
        [FromServices] JobPipelineService pipeline,
        CancellationToken cancellationToken)
    {
        return pipeline.UpdateAsync(CurrentUserId(), id, request, cancellationToken);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(
        Guid id,
        [FromServices] JobPipelineService pipeline,
        CancellationToken cancellationToken)
    {
        await pipeline.DeleteAsync(CurrentUserId(), id, cancellationToken);
        return NoContent();
    }

    [HttpDelete]
    public async Task<IActionResult> DeleteAll(
        [FromServices] JobPipelineService pipeline,
        CancellationToken cancellationToken)
    {
        await pipeline.DeleteAllAsync(CurrentUserId(), cancellationToken);
        return NoContent();
    }

    [HttpPost("update-all")]
    public Task<JobPipelineUpdateResultDto> ApplyAll(
        [FromServices] JobPipelineService pipeline,
        CancellationToken cancellationToken)
    {
        return pipeline.ApplyAllAsync(CurrentUserId(), cancellationToken);
    }

    [HttpPost("forward-all")]
    public Task<JobPipelineBatchForwardResultDto> ForwardAll(
        [FromServices] JobPipelineService pipeline,
        CancellationToken cancellationToken)
    {
        return pipeline.ForwardAllAsync(CurrentUserId(), cancellationToken);
    }

    [HttpPost("{id:guid}/update")]
    public Task<JobPipelineUpdateResultDto> Apply(
        Guid id,
        [FromServices] JobPipelineService pipeline,
        CancellationToken cancellationToken)
    {
        return pipeline.ApplyAsync(CurrentUserId(), id, cancellationToken);
    }

    [HttpPost("{id:guid}/forward")]
    public Task<JobPipelineForwardResultDto> Forward(
        Guid id,
        [FromServices] JobPipelineService pipeline,
        CancellationToken cancellationToken)
    {
        return pipeline.ForwardAsync(CurrentUserId(), id, cancellationToken);
    }

    [HttpGet("{id:guid}/banned-companies")]
    public Task<IReadOnlyList<JobPipelineBannedCompanyDto>> ListBanned(
        Guid id,
        [FromServices] JobPipelineService pipeline,
        CancellationToken cancellationToken)
    {
        return pipeline.ListBannedAsync(CurrentUserId(), id, cancellationToken);
    }

    [HttpPost("{id:guid}/banned-companies")]
    public async Task<ActionResult<JobPipelineBannedCompanyDto>> CreateBanned(
        Guid id,
        [FromBody] JobPipelineBannedCompanyWriteRequest request,
        [FromServices] JobPipelineService pipeline,
        CancellationToken cancellationToken)
    {
        var created = await pipeline.CreateBannedAsync(CurrentUserId(), id, request, cancellationToken);
        return Created($"/api/job-application/pipeline/{id}/banned-companies/{created.Id}", created);
    }

    [HttpPut("{id:guid}/banned-companies/{companyId:guid}")]
    public Task<JobPipelineBannedCompanyDto> UpdateBanned(
        Guid id,
        Guid companyId,
        [FromBody] JobPipelineBannedCompanyWriteRequest request,
        [FromServices] JobPipelineService pipeline,
        CancellationToken cancellationToken)
    {
        return pipeline.UpdateBannedAsync(CurrentUserId(), id, companyId, request, cancellationToken);
    }

    [HttpDelete("{id:guid}/banned-companies/{companyId:guid}")]
    public async Task<IActionResult> DeleteBanned(
        Guid id,
        Guid companyId,
        [FromServices] JobPipelineService pipeline,
        CancellationToken cancellationToken)
    {
        await pipeline.DeleteBannedAsync(CurrentUserId(), id, companyId, cancellationToken);
        return NoContent();
    }

    [HttpGet("{id:guid}/banned-matches")]
    public Task<JobPipelineBannedMatchesDto> ListBannedMatches(
        Guid id,
        [FromServices] JobPipelineService pipeline,
        CancellationToken cancellationToken)
    {
        return pipeline.ListBannedMatchesAsync(CurrentUserId(), id, cancellationToken);
    }

    private Guid CurrentUserId()
    {
        var subject = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new AuthenticationFailedException("Session is no longer valid.");
        return Guid.Parse(subject);
    }
}
