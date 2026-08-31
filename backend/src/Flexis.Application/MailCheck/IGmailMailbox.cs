using Flexis.Domain.MailCheck;

namespace Flexis.Application.MailCheck;

public sealed record GmailMessageRef(string Id);

public sealed record GmailCandidatePage(IReadOnlyList<GmailMessageRef> Messages, string? NextPageToken);

public sealed record GmailMessageContent(
    string Id,
    string ThreadId,
    string Subject,
    string From,
    string Date,
    string Snippet,
    string Body,
    IReadOnlyList<string> LabelIds);

public sealed record GmailLabeledMessage(
    string Id,
    string ThreadId,
    string Subject,
    string From,
    string Date,
    string Snippet,
    MailCheckDecision Decision,
    bool Starred);

public interface IGmailMailbox
{
    Task<IReadOnlyDictionary<MailCheckDecision, string>> EnsureLabelsAsync(
        string accessToken,
        CancellationToken cancellationToken);

    Task<GmailCandidatePage> ListCandidatesAsync(
        string accessToken,
        string? pageToken,
        CancellationToken cancellationToken);

    Task<GmailMessageContent> GetMessageAsync(
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

    Task<IReadOnlyList<GmailLabeledMessage>> ListLabeledAsync(
        string accessToken,
        IReadOnlyDictionary<MailCheckDecision, string> labels,
        MailCheckDecision? filter,
        CancellationToken cancellationToken);
}
