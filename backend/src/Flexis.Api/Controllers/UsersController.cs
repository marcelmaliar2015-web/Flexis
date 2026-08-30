using Flexis.Application.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Flexis.Api.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/users")]
public sealed class UsersController : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<UserDto>> List(
        [FromServices] UserManagementService users,
        CancellationToken cancellationToken)
    {
        return users.ListAsync(cancellationToken);
    }

    [HttpPost]
    public async Task<ActionResult<UserDto>> Create(
        [FromBody] CreateUserRequest request,
        [FromServices] UserManagementService users,
        CancellationToken cancellationToken)
    {
        var created = await users.CreateAsync(request, cancellationToken);
        return Created($"/api/users/{created.Id}", created);
    }

    [HttpPut("{id:guid}")]
    public Task<UserDto> Update(
        Guid id,
        [FromBody] UpdateUserRequest request,
        [FromServices] UserManagementService users,
        CancellationToken cancellationToken)
    {
        return users.UpdateAsync(id, request, cancellationToken);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(
        Guid id,
        [FromServices] UserManagementService users,
        CancellationToken cancellationToken)
    {
        await users.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
