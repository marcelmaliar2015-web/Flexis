using System.Security.Claims;
using Flexis.Application.Auth;
using Flexis.Application.Common;
using Flexis.Application.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Flexis.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("sign-in")]
    public Task<SignInResultDto> SignIn(
        [FromBody] SignInRequest request,
        [FromServices] AuthService auth,
        CancellationToken cancellationToken)
    {
        return auth.SignInAsync(request, cancellationToken);
    }

    [HttpGet("me")]
    public Task<UserDto> Me(
        [FromServices] AuthService auth,
        CancellationToken cancellationToken)
    {
        var subject = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new AuthenticationFailedException("Session is no longer valid.");
        return auth.GetCurrentUserAsync(Guid.Parse(subject), cancellationToken);
    }

    [HttpPut("me")]
    public Task<UserDto> UpdateMe(
        [FromBody] UpdateCurrentUserRequest request,
        [FromServices] UserManagementService users,
        CancellationToken cancellationToken)
    {
        var subject = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new AuthenticationFailedException("Session is no longer valid.");
        return users.UpdateCurrentAsync(Guid.Parse(subject), request, cancellationToken);
    }
}
