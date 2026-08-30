using System.Security.Claims;
using Flexis.Application.Common;
using Flexis.Application.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Flexis.Api.Controllers;

[ApiController]
[Route("api/google/connections")]
public sealed class GoogleConnectionsController : ControllerBase
{
    [HttpGet]
    public Task<GoogleConnectionStatusDto> Get(
        [FromServices] GoogleConnectionService connections,
        CancellationToken cancellationToken)
    {
        return connections.GetStatusAsync(CurrentUserId(), cancellationToken);
    }

    [HttpPost("start")]
    public GoogleConnectStartDto Start(
        [FromBody] GoogleConnectStartRequest request,
        [FromServices] GoogleConnectionService connections)
    {
        return connections.StartConnect(CurrentUserId(), request);
    }

    [AllowAnonymous]
    [HttpGet("callback")]
    public async Task<IActionResult> Callback(
        [FromQuery] string? code,
        [FromQuery] string? state,
        [FromQuery] string? error,
        [FromServices] GoogleConnectionService connections,
        CancellationToken cancellationToken)
    {
        var location = await connections.CompleteCallbackAsync(code, state, error, cancellationToken);
        return Redirect(location);
    }

    [HttpDelete]
    public async Task<IActionResult> Disconnect(
        [FromServices] GoogleConnectionService connections,
        CancellationToken cancellationToken)
    {
        await connections.DisconnectAsync(CurrentUserId(), cancellationToken);
        return NoContent();
    }

    private Guid CurrentUserId()
    {
        var subject = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new AuthenticationFailedException("Session is no longer valid.");
        return Guid.Parse(subject);
    }
}
