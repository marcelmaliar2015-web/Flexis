namespace Flexis.Domain.MailCheck;

public sealed class MailCheckSettings
{
    public const string DefaultModel = "gpt-4o-mini";

    private MailCheckSettings()
    {
        Model = DefaultModel;
        LastError = string.Empty;
        ClassifierPrompt = string.Empty;
    }

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public string? ApiKeyProtected { get; private set; }

    public string Model { get; private set; }

    public string ClassifierPrompt { get; private set; }

    public DateTimeOffset? LastRunAt { get; private set; }

    public string LastError { get; private set; }

    public int LastLabeled { get; private set; }

    public int LastTrashed { get; private set; }

    public int LastSkipped { get; private set; }

    public int LastProcessed { get; private set; }

    public int LastErrors { get; private set; }

    public bool LastHasMore { get; private set; }

    public int TotalLabeled { get; private set; }

    public int TotalTrashed { get; private set; }

    public static MailCheckSettings Create(Guid userId)
    {
        return new MailCheckSettings
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Model = DefaultModel,
            LastError = string.Empty
        };
    }

    public bool HasApiKey => !string.IsNullOrWhiteSpace(ApiKeyProtected);

    public void SetApiKeyProtected(string protectedKey)
    {
        ApiKeyProtected = protectedKey;
    }

    public void ClearApiKey()
    {
        ApiKeyProtected = null;
    }

    public void SetModel(string model)
    {
        Model = model;
    }

    public void SetClassifierPrompt(string prompt)
    {
        ClassifierPrompt = prompt;
    }

    public void RecordRun(
        int labeled,
        int trashed,
        int skipped,
        int processed,
        int errors,
        bool hasMore,
        string error)
    {
        LastRunAt = DateTimeOffset.UtcNow;
        LastLabeled = labeled;
        LastTrashed = trashed;
        LastSkipped = skipped;
        LastProcessed = processed;
        LastErrors = errors;
        LastHasMore = hasMore;
        LastError = error.Length > 500 ? error[..500] : error;
        TotalLabeled += labeled;
        TotalTrashed += trashed;
    }
}
