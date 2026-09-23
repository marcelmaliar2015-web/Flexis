using Flexis.Domain.JobApplication;

namespace Flexis.Application.JobApplication;

public interface IJobListingProjectionRepository
{
    Task<IReadOnlyList<JobListingProjection>> ListByUserAsync(
        Guid userId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<JobListingProjection>> ListProfileMainAsync(
        Guid userId,
        Guid profileId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<JobListingProjection>> ListProfileArchivesAsync(
        Guid userId,
        Guid profileId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<JobListingProjection>> ListSearchBaseAsync(
        Guid userId,
        CancellationToken cancellationToken);

    Task<bool> HasProfileMainAsync(Guid userId, Guid profileId, CancellationToken cancellationToken);

    Task ReplaceScopeAsync(
        Guid userId,
        string source,
        Guid? profileId,
        string archiveTab,
        IReadOnlyList<JobListingProjection> rows,
        CancellationToken cancellationToken);

    Task MoveProfileMainToArchiveAsync(
        Guid userId,
        Guid profileId,
        string archiveTab,
        CancellationToken cancellationToken);

    Task ClearProfileMainAsync(Guid userId, Guid profileId, CancellationToken cancellationToken);

    Task UpdateStatusesAsync(
        Guid userId,
        Guid profileId,
        IReadOnlyDictionary<string, string> statusByListingKey,
        CancellationToken cancellationToken);

    Task DeleteByProfileAsync(Guid userId, Guid profileId, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}

public interface IJobSheetSyncStateRepository
{
    Task<JobSheetSyncState?> GetAsync(
        Guid userId,
        string spreadsheetId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<JobSheetSyncState>> ListByUserAsync(
        Guid userId,
        CancellationToken cancellationToken);

    Task AddAsync(JobSheetSyncState state, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
