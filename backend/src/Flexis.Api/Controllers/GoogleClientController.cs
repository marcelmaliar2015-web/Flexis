using Flexis.Application.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Flexis.Api.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/google/client")]
public sealed class GoogleClientController : ControllerBase
{
    [HttpGet]
    public Task<GoogleClientSettingsDto> Get(
        [FromServices] IGoogleClientCredentialStore clients,
        CancellationToken cancellationToken)
    {
        return clients.GetPublicAsync(cancellationToken);
    }

    [HttpPut]
    public async Task<ActionResult<GoogleClientSettingsDto>> Save(
        [FromBody] SaveGoogleClientSettingsRequest request,
        [FromServices] IGoogleClientCredentialStore clients,
        CancellationToken cancellationToken)
    {
        await clients.SaveAsync(request, cancellationToken);
        return await clients.GetPublicAsync(cancellationToken);
    }
}
