namespace Flexis.Domain.JobApplication;

public sealed class JobApplicationLog
{
    private JobApplicationLog()
    {
        Category = string.Empty;
        Action = string.Empty;
        Summary = string.Empty;
        Detail = string.Empty;
    }

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public DateTimeOffset OccurredAt { get; private set; }

    public string Category { get; private set; }

    public string Action { get; private set; }

    public string Summary { get; private set; }

    public string Detail { get; private set; }

    public static JobApplicationLog Create(
        Guid userId,
        string category,
        string action,
        string summary,
        string detail)
    {
        return new JobApplicationLog
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            OccurredAt = DateTimeOffset.UtcNow,
            Category = category,
            Action = action,
            Summary = summary,
            Detail = detail
        };
    }
}
