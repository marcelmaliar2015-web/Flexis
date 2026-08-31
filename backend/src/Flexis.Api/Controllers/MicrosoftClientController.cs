using Flexis.Application.Microsoft;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Flexis.Api.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/microsoft/client")]
public sealed class MicrosoftClientController : ControllerBase
{
    [HttpGet]
    public Task<MicrosoftClientSettingsDto> Get(
        [FromServices] IMicrosoftClientCredentialStore clients,
        CancellationToken cancellationToken)
    {
        return clients.GetPublicAsync(cancellationToken);
    }

    [HttpPut]
    public async Task<ActionResult<MicrosoftClientSettingsDto>> Save(
        [FromBody] SaveMicrosoftClientSettingsRequest request,
        [FromServices] IMicrosoftClientCredentialStore clients,
        CancellationToken cancellationToken)
    {
        await clients.SaveAsync(request, cancellationToken);
        return await clients.GetPublicAsync(cancellationToken);
    }
}
