namespace Flexis.Application.Diagnostics;

public sealed record DiagnosticsEventRequest(
    string Severity,
    string Source,
    string Message,
    string? Method,
    string? Path,
    int? Status,
    string? Detail);
