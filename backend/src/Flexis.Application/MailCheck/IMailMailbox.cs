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
    MailCheckLabel Label,
    bool Starred);

public interface IMailMailbox
{
    Task<IReadOnlyDictionary<MailCheckLabel, string>> EnsureLabelsAsync(
        string accessToken,
        IReadOnlyCollection<MailCheckLabel> labels,
        CancellationToken cancellationToken);

    Task<MailCandidatePage> ListCandidatesAsync(
        string accessToken,
        string? pageToken,
        CancellationToken cancellationToken);

    Task<MailMessageContent> GetMessageAsync(
        string accessToken,
        string messageId,
        CancellationToken cancellationToken);

    Task ApplyLabelAsync(
        string accessToken,
        string messageId,
        string labelId,
        IReadOnlyList<string> currentLabelIds,
        bool star,
        CancellationToken cancellationToken);

    Task TrashAsync(string accessToken, string messageId, CancellationToken cancellationToken);

    Task CreateDraftReplyAsync(
        string accessToken,
        MailMessageContent message,
        string replyBody,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<MailLabeledMessage>> ListLabeledAsync(
        string accessToken,
        IReadOnlyDictionary<MailCheckLabel, string> labels,
        MailCheckLabel? filter,
        CancellationToken cancellationToken);
}
