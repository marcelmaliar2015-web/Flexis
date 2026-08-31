using Flexis.Domain.MailCheck;

namespace Flexis.Application.MailCheck;

public sealed record MailMessageRef(string Id);

public sealed record MailCandidatePage(IReadOnlyList<MailMessageRef> Messages, string? NextPageToken);

public sealed record MailMessageContent(
    string Id,
    string ThreadId,
    string Subject,
    string From,
    string Date,
    string Snippet,
    string Body,
    IReadOnlyList<string> LabelIds);

public sealed record MailLabeledMessage(
    string Id,
    string ThreadId,
    string Subject,
    string From,
    string Date,
    string Snippet,
    MailCheckDecision Decision,
    bool Starred);

public interface IMailMailbox
{
    Task<IReadOnlyDictionary<MailCheckDecision, string>> EnsureLabelsAsync(
        string accessToken,
        CancellationToken cancellationToken);

    Task<MailCandidatePage> ListCandidatesAsync(
        string accessToken,
        string? pageToken,
        CancellationToken cancellationToken);

    Task<MailMessageContent> GetMessageAsync(
        string accessToken,
        string messageId,
        CancellationToken cancellationToken);

    Task ApplyLabelAndPinAsync(
        string accessToken,
        string messageId,
        string labelId,
        IReadOnlyList<string> currentLabelIds,
        CancellationToken cancellationToken);

    Task TrashAsync(string accessToken, string messageId, CancellationToken cancellationToken);

    Task<IReadOnlyList<MailLabeledMessage>> ListLabeledAsync(
        string accessToken,
        IReadOnlyDictionary<MailCheckDecision, string> labels,
        MailCheckDecision? filter,
        CancellationToken cancellationToken);
}
