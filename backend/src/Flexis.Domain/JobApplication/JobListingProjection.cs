namespace Flexis.Domain.JobApplication;

public static class JobListingProjectionSources
{
    public const string ProfileMain = "profile_main";

    public const string ProfileArchive = "profile_archive";

    public const string SearchBase = "search_base";
}

public sealed class JobListingProjection
{
    private JobListingProjection()
    {
        Source = JobListingProjectionSources.ProfileMain;
        ArchiveTab = string.Empty;
        ListingKey = string.Empty;
        CompanyName = string.Empty;
        Position = string.Empty;
        Link = string.Empty;
        Jd = string.Empty;
        Download = string.Empty;
        Status = string.Empty;
        Issue = string.Empty;
        ProfileName = string.Empty;
        ContentHash = string.Empty;
    }

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public Guid? ProfileId { get; private set; }

    public string Source { get; private set; }

    public string ArchiveTab { get; private set; }

    public string ListingKey { get; private set; }

    public string CompanyName { get; private set; }

    public string Position { get; private set; }

    public string Link { get; private set; }

    public string Jd { get; private set; }

    public string Download { get; private set; }

    public string Status { get; private set; }

    public string Issue { get; private set; }

    public string ProfileName { get; private set; }

    public int RowNumber { get; private set; }

    public string ContentHash { get; private set; }

    public DateTimeOffset SyncedAt { get; private set; }

    public static JobListingProjection Create(
        Guid userId,
        Guid? profileId,
        string source,
        string archiveTab,
        string listingKey,
        string companyName,
        string position,
        string link,
        string jd,
        string download,
        string status,
        string issue,
        string profileName,
        int rowNumber,
        string contentHash)
    {
        return new JobListingProjection
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ProfileId = profileId,
            Source = source,
            ArchiveTab = archiveTab ?? string.Empty,
            ListingKey = listingKey,
            CompanyName = companyName,
            Position = position,
            Link = link,
            Jd = jd,
            Download = download,
            Status = status,
            Issue = issue,
            ProfileName = profileName,
            RowNumber = rowNumber,
            ContentHash = contentHash,
            SyncedAt = DateTimeOffset.UtcNow
        };
    }

    public void SetStatus(string status)
    {
        Status = status;
        SyncedAt = DateTimeOffset.UtcNow;
    }
}

public sealed class JobSheetSyncState
{
    private JobSheetSyncState()
    {
        SpreadsheetId = string.Empty;
        DriveModifiedTime = string.Empty;
    }

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public string SpreadsheetId { get; private set; }

    public string DriveModifiedTime { get; private set; }

    public DateTimeOffset? LastSyncedAt { get; private set; }

    public static JobSheetSyncState Create(Guid userId, string spreadsheetId)
    {
        return new JobSheetSyncState
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            SpreadsheetId = spreadsheetId,
            DriveModifiedTime = string.Empty
        };
    }

    public void MarkSynced(string driveModifiedTime)
    {
        DriveModifiedTime = driveModifiedTime ?? string.Empty;
        LastSyncedAt = DateTimeOffset.UtcNow;
    }
}
