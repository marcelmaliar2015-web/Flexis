using Flexis.Domain.MailCheck;

namespace Flexis.Application.MailCheck;

public sealed record OpenAiTokenUsage(int PromptTokens, int CompletionTokens, int TotalTokens)
{
    public static OpenAiTokenUsage Empty { get; } = new(0, 0, 0);

    public bool HasTokens => TotalTokens > 0 || PromptTokens > 0 || CompletionTokens > 0;

    public OpenAiTokenUsage Add(OpenAiTokenUsage other)
    {
        return new OpenAiTokenUsage(
            PromptTokens + other.PromptTokens,
            CompletionTokens + other.CompletionTokens,
            TotalTokens + other.TotalTokens);
    }
}

public sealed record MailCheckClassification(MailCheckLabel Label, OpenAiTokenUsage Usage);

public sealed record OpenAiModelInfo(string Id);

public interface IOpenAiGateway
{
    Task<IReadOnlyList<OpenAiModelInfo>> ListModelsAsync(string apiKey, CancellationToken cancellationToken);

    Task<MailCheckClassification> ClassifyAsync(
        string apiKey,
        string model,
        string classifierPrompt,
        string mailText,
        CancellationToken cancellationToken);
}
