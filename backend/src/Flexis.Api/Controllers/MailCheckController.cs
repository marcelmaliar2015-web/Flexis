using System.Security.Claims;
using Flexis.Application.Common;
using Flexis.Application.MailCheck;
using Microsoft.AspNetCore.Mvc;

namespace Flexis.Api.Controllers;

[ApiController]
[Route("api/mail-check")]
public sealed class MailCheckController : ControllerBase
{
    [HttpGet("settings")]
    public Task<MailCheckSettingsDto> GetSettings(
        [FromServices] MailCheckService mailCheck,
        CancellationToken cancellationToken)
    {
        return mailCheck.GetSettingsAsync(CurrentUserId(), cancellationToken);
    }

    [HttpPut("settings")]
    public Task<MailCheckSettingsDto> UpdateSettings(
        [FromBody] MailCheckSettingsWriteRequest request,
        [FromServices] MailCheckService mailCheck,
        CancellationToken cancellationToken)
    {
        return mailCheck.UpdateSettingsAsync(CurrentUserId(), request, cancellationToken);
    }

    [HttpGet("models")]
    public Task<MailCheckModelsDto> ListModels(
        [FromServices] MailCheckService mailCheck,
        CancellationToken cancellationToken)
    {
        return mailCheck.ListModelsAsync(CurrentUserId(), cancellationToken);
    }

    [HttpPost("run")]
    public Task<MailCheckRunDto> Run(
        [FromBody] MailCheckRunRequest? request,
        [FromServices] MailCheckService mailCheck,
        CancellationToken cancellationToken)
    {
        return mailCheck.RunAsync(CurrentUserId(), request ?? new MailCheckRunRequest(false), cancellationToken);
    }

    [HttpGet("inbox")]
    public Task<MailCheckInboxDto> GetInbox(
        [FromQuery] string? label,
        [FromServices] MailCheckService mailCheck,
        CancellationToken cancellationToken)
    {
        return mailCheck.GetInboxAsync(CurrentUserId(), label, cancellationToken);
    }

    private Guid CurrentUserId()
    {
        var subject = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new AuthenticationFailedException("Session is no longer valid.");
        return Guid.Parse(subject);
    }
}
