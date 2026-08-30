namespace Flexis.Domain.JobApplication;

public sealed class JobCatalogItem
{
    private JobCatalogItem()
    {
        Title = string.Empty;
        Url = string.Empty;
    }

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public JobCatalogKind Kind { get; private set; }

    public string Title { get; private set; }

    public string Url { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static JobCatalogItem Create(
        Guid userId,
        JobCatalogKind kind,
        string title,
        string url)
    {
        return new JobCatalogItem
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Kind = kind,
            Title = title,
            Url = url,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public void SetDetails(string title, string url)
    {
        Title = title;
        Url = url;
    }
}
