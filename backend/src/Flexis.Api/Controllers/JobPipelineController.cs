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

    [HttpPost("{id:guid}/update")]
    public Task<JobPipelineUpdateResultDto> Apply(
        Guid id,
        [FromServices] JobPipelineService pipeline,
        CancellationToken cancellationToken)
    {
        return pipeline.ApplyAsync(CurrentUserId(), id, cancellationToken);
    }

    private Guid CurrentUserId()
    {
        var subject = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new AuthenticationFailedException("Session is no longer valid.");
        return Guid.Parse(subject);
    }
}
