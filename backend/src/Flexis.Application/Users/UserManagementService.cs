using Flexis.Application.Common;
using Flexis.Domain.Users;

namespace Flexis.Application.Users;

public sealed class UserManagementService
{
    private readonly IUserRepository _users;
    private readonly IUserPasswordHasher _passwordHasher;

    public UserManagementService(IUserRepository users, IUserPasswordHasher passwordHasher)
    {
        _users = users;
        _passwordHasher = passwordHasher;
    }

    public async Task<IReadOnlyList<UserDto>> ListAsync(CancellationToken cancellationToken)
    {
        var users = await _users.ListAsync(cancellationToken);
        return users.Select(UserMapper.ToDto).ToArray();
    }

    public async Task<UserDto> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken)
    {
        var email = UserRules.NormalizeEmail(request.Email);
        var displayName = UserRules.NormalizeDisplayName(request.DisplayName);
        UserRules.EnsurePassword(request.Password);
        EnsureDefinedRole(request.Role);

        if (await _users.GetByEmailAsync(email, cancellationToken) is not null)
        {
            throw new ConflictException("A user with that email already exists.");
        }

        var user = User.Create(email, displayName, request.Role, _passwordHasher.Hash(request.Password));
        await _users.AddAsync(user, cancellationToken);
        await _users.SaveChangesAsync(cancellationToken);
        return UserMapper.ToDto(user);
    }

    public async Task<UserDto> UpdateAsync(
        Guid id,
        UpdateUserRequest request,
        CancellationToken cancellationToken)
    {
        var displayName = UserRules.NormalizeDisplayName(request.DisplayName);
        EnsureDefinedRole(request.Role);

        var user = await _users.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("User was not found.");

        var removingLastAdmin = user.Role == UserRole.Admin
            && user.IsActive
            && (request.Role != UserRole.Admin || !request.IsActive)
            && await _users.CountActiveAdminsAsync(cancellationToken) <= 1;

        if (removingLastAdmin)
        {
            throw new DomainRuleException("The last active admin cannot be demoted or deactivated.");
        }

        user.SetDisplayName(displayName);
        user.ChangeRole(request.Role);
        user.SetActive(request.IsActive);

        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            UserRules.EnsurePassword(request.Password);
            user.SetPasswordHash(_passwordHasher.Hash(request.Password));
        }

        await _users.SaveChangesAsync(cancellationToken);
        return UserMapper.ToDto(user);
    }

    public async Task<UserDto> UpdateCurrentAsync(
        Guid id,
        UpdateCurrentUserRequest request,
        CancellationToken cancellationToken)
    {
        var displayName = UserRules.NormalizeDisplayName(request.DisplayName);
        var user = await _users.GetByIdAsync(id, cancellationToken);
        if (user is null || !user.IsActive)
        {
            throw new AuthenticationFailedException("Session is no longer valid.");
        }

        user.SetDisplayName(displayName);
        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            UserRules.EnsurePassword(request.Password);
            user.SetPasswordHash(_passwordHasher.Hash(request.Password));
        }

        await _users.SaveChangesAsync(cancellationToken);
        return UserMapper.ToDto(user);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var user = await _users.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("User was not found.");

        if (user.Role == UserRole.Admin
            && user.IsActive
            && await _users.CountActiveAdminsAsync(cancellationToken) <= 1)
        {
            throw new DomainRuleException("The last active admin cannot be deleted.");
        }

        _users.Remove(user);
        await _users.SaveChangesAsync(cancellationToken);
    }

    private static void EnsureDefinedRole(UserRole role)
    {
        if (!Enum.IsDefined(role))
        {
            throw new ValidationFailedException("Role is not valid.");
        }
    }
}
