using System.Security.Claims;
using Flexis.Application.Common;
using Flexis.Application.JobApplication;
using Microsoft.AspNetCore.Mvc;

namespace Flexis.Api.Controllers;

[ApiController]
[Route("api/job-application/financial")]
public sealed class JobFinancialController : ControllerBase
{
    [HttpGet]
    public Task<JobFinancialBoardDto> Get(
        [FromServices] JobFinancialService financial,
        CancellationToken cancellationToken)
    {
        return financial.GetBoardAsync(CurrentUserId(), cancellationToken);
    }

    [HttpPut("defaults")]
    public Task<JobFinancialDefaultsDto> UpdateDefaults(
        [FromBody] JobFinancialRatesRequest request,
        [FromServices] JobFinancialService financial,
        CancellationToken cancellationToken)
    {
        return financial.UpdateDefaultsAsync(CurrentUserId(), request, cancellationToken);
    }

    [HttpPut("rows/{entryId:guid}/rates")]
    public Task<JobFinancialRowDto> UpdateRates(
        Guid entryId,
        [FromBody] JobFinancialRatesRequest request,
        [FromServices] JobFinancialService financial,
        CancellationToken cancellationToken)
    {
        return financial.UpdateRatesAsync(CurrentUserId(), entryId, request, cancellationToken);
    }

    [HttpGet("history")]
    public Task<IReadOnlyList<JobFinancialSnapshotDto>> GetHistory(
        [FromServices] JobFinancialService financial,
        CancellationToken cancellationToken)
    {
        return financial.GetHistoryAsync(CurrentUserId(), cancellationToken);
    }

    [HttpGet("statistics")]
    public Task<JobStatisticsBoardDto> GetStatistics(
        [FromServices] JobFinancialService financial,
        CancellationToken cancellationToken)
    {
        return financial.GetStatisticsAsync(CurrentUserId(), cancellationToken);
    }

    private Guid CurrentUserId()
    {
        var subject = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new AuthenticationFailedException("Session is no longer valid.");
        return Guid.Parse(subject);
    }
}
