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

    private Guid CurrentUserId()
    {
        var subject = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new AuthenticationFailedException("Session is no longer valid.");
        return Guid.Parse(subject);
    }
}
