namespace Flexis.Application.Health;

public sealed record HealthStatusDto(
    string Status,
    IReadOnlyList<HealthCheckDto> Checks);

public sealed record HealthCheckDto(
    string Name,
    string Status,
    string? Description);
