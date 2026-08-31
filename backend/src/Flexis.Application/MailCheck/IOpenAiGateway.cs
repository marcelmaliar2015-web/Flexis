using Flexis.Domain.MailCheck;

namespace Flexis.Application.MailCheck;

public sealed record MailCheckClassification(MailCheckDecision Decision, string Reason);

public sealed record OpenAiModelInfo(string Id);

public interface IOpenAiGateway
{
    Task<IReadOnlyList<OpenAiModelInfo>> ListModelsAsync(string apiKey, CancellationToken cancellationToken);

    Task<MailCheckClassification> ClassifyAsync(
        string apiKey,
        string model,
        string mailText,
        CancellationToken cancellationToken);
}
