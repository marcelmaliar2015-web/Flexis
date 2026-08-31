namespace Flexis.Application.Diagnostics;

public sealed record IssueLogEntry(
    DateTimeOffset OccurredAt,
    string Severity,
    string Source,
    string Message,
    string? Method,
    string? Path,
    int? Status,
    string? Detail,
    string? Exception);

public interface IIssueLog
{
    Task WriteAsync(IssueLogEntry entry, CancellationToken cancellationToken);
}
