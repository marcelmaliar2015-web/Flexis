namespace Flexis.Domain.MailCheck;

public sealed class MailCheckSettings
{
    public const string DefaultModel = "gpt-4.1-mini";

    public const string DefaultLabelActionsJson =
        "{\"rejected\":\"trash\",\"applied\":\"keep\",\"schedule\":\"pin\",\"scheduled\":\"pin\",\"assessment\":\"pin\",\"availability\":\"pin\",\"success\":\"pin\",\"other\":\"keep\",\"less_important\":\"trash\"}";

    public const string DefaultNeedActionLabelsJson = "[\"schedule\",\"assessment\",\"availability\"]";

    private MailCheckSettings()
    {
        Model = DefaultModel;
        LastError = string.Empty;
        ClassifierPrompt = string.Empty;
        LabelActionsJson = MailCheckSettings.DefaultLabelActionsJson;
        NeedActionLabelsJson = MailCheckSettings.DefaultNeedActionLabelsJson;
        AutoCheckEnabled = true;
    }

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public string? ApiKeyProtected { get; private set; }

    public string Model { get; private set; }

    public string ClassifierPrompt { get; private set; }

    public string LabelActionsJson { get; private set; }

    public string NeedActionLabelsJson { get; private set; }

    public bool AutoCheckEnabled { get; private set; }

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
            LastError = string.Empty,
            LabelActionsJson = DefaultLabelActionsJson,
            NeedActionLabelsJson = DefaultNeedActionLabelsJson,
            AutoCheckEnabled = true
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

    public void SetLabelActionsJson(string json)
    {
        LabelActionsJson = json;
    }

    public void SetNeedActionLabelsJson(string json)
    {
        NeedActionLabelsJson = json;
    }

    public void SetAutoCheckEnabled(bool enabled)
    {
        AutoCheckEnabled = enabled;
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
