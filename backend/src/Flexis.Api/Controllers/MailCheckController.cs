using System.Security.Claims;
using Flexis.Application.Common;
using Flexis.Application.MailCheck;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Flexis.Api.Controllers;

[ApiController]
[Route("api/mail-check")]
public sealed class MailCheckController : ControllerBase
{
    [HttpGet("mailbox")]
    public Task<MailMailboxStatusDto> GetMailbox(
        [FromServices] MailConnectionService mailbox,
        CancellationToken cancellationToken)
    {
        return mailbox.GetStatusAsync(CurrentUserId(), cancellationToken);
    }

    [HttpPost("mailbox/gmail/start")]
    public Task<MailConnectStartDto> StartGmailMailbox(
        [FromBody] MailConnectStartRequest request,
        [FromServices] MailConnectionService mailbox,
        CancellationToken cancellationToken)
    {
        return mailbox.StartGmailConnectAsync(CurrentUserId(), request, cancellationToken);
    }

    [HttpPost("mailbox/outlook/start")]
    public Task<MailConnectStartDto> StartOutlookMailbox(
        [FromBody] MailConnectStartRequest request,
        [FromServices] MailConnectionService mailbox,
        CancellationToken cancellationToken)
    {
        return mailbox.StartOutlookConnectAsync(CurrentUserId(), request, cancellationToken);
    }

    [AllowAnonymous]
    [HttpGet("mailbox/outlook/callback")]
    public async Task<IActionResult> CompleteOutlookMailbox(
        [FromQuery] string? code,
        [FromQuery] string? state,
        [FromQuery] string? error,
        [FromServices] MailConnectionService mailbox,
        CancellationToken cancellationToken)
    {
        var redirectUrl = await mailbox.CompleteOutlookCallbackAsync(code, state, error, cancellationToken);
        return Redirect(redirectUrl);
    }

    [HttpDelete("mailbox/{id:guid}")]
    public async Task<IActionResult> DisconnectMailbox(
        Guid id,
        [FromServices] MailConnectionService mailbox,
        CancellationToken cancellationToken)
    {
        await mailbox.DisconnectAsync(CurrentUserId(), id, cancellationToken);
        return NoContent();
    }

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
        return mailCheck.RunAsync(
            CurrentUserId(),
            request ?? new MailCheckRunRequest(false, null, false),
            cancellationToken);
    }

    [HttpGet("run/progress")]
    public MailCheckRunProgressDto GetRunProgress([FromServices] MailCheckService mailCheck)
    {
        return mailCheck.GetRunProgress(CurrentUserId());
    }

    [HttpGet("inbox")]
    public Task<MailCheckInboxDto> GetInbox(
        [FromQuery] string? label,
        [FromServices] MailCheckService mailCheck,
        CancellationToken cancellationToken)
    {
        return mailCheck.GetInboxAsync(CurrentUserId(), label, cancellationToken);
    }

    [HttpGet("need-action")]
    public Task<MailCheckInboxDto> GetNeedActionInbox(
        [FromServices] MailCheckService mailCheck,
        CancellationToken cancellationToken)
    {
        return mailCheck.GetNeedActionInboxAsync(CurrentUserId(), cancellationToken);
    }

    private Guid CurrentUserId()
    {
        var subject = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new AuthenticationFailedException("Session is no longer valid.");
        return Guid.Parse(subject);
    }
}
