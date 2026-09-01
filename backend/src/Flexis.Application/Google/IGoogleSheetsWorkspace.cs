namespace Flexis.Application.Google;

public enum JobWorkbookKind
{
    Profile,
    Source
}

public sealed record CreatedSpreadsheet(string SpreadsheetId, string SpreadsheetUrl);

public sealed record SpreadsheetSheet(int SheetId, string Name);

public sealed record JobListingRow(
    string CompanyName,
    string Position,
    string Link,
    string Jd,
    string Status = "")
{
    public bool IsEmpty =>
        string.IsNullOrWhiteSpace(CompanyName)
        && string.IsNullOrWhiteSpace(Position)
        && string.IsNullOrWhiteSpace(Link)
        && string.IsNullOrWhiteSpace(Jd);
}

public interface IGoogleSheetsWorkspace
{
    Task<CreatedSpreadsheet> CreateWorkbookAsync(
        string accessToken,
        string fileName,
        string firstSheetName,
        JobWorkbookKind kind,
        string parentFolderId,
        CancellationToken cancellationToken);

    Task RenameFileAsync(
        string accessToken,
        string spreadsheetId,
        string fileName,
        CancellationToken cancellationToken);

    Task RenameSheetAsync(
        string accessToken,
        string spreadsheetId,
        int sheetId,
        string name,
        CancellationToken cancellationToken);

    Task<SpreadsheetSheet> AddSourceLocationSheetAsync(
        string accessToken,
        string spreadsheetId,
        string name,
        CancellationToken cancellationToken);

    Task<SpreadsheetSheet> ReplaceProfileMainSheetAsync(
        string accessToken,
        string spreadsheetId,
        int currentMainSheetId,
        string archiveTabName,
        string newMainTabName,
        CancellationToken cancellationToken);

    Task DeleteSheetAsync(
        string accessToken,
        string spreadsheetId,
        int sheetId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<SpreadsheetSheet>> ListSheetsAsync(
        string accessToken,
        string spreadsheetId,
        CancellationToken cancellationToken);

    Task DeleteFileAsync(string accessToken, string spreadsheetId, CancellationToken cancellationToken);

    Task SetFixedRowHeightAsync(
        string accessToken,
        string spreadsheetId,
        CancellationToken cancellationToken);

    Task RemoveStatusColumnAsync(
        string accessToken,
        string spreadsheetId,
        CancellationToken cancellationToken);

    Task ProtectWorkbookAsync(
        string accessToken,
        string spreadsheetId,
        string ownerEmail,
        JobWorkbookKind kind,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<JobListingRow>> ReadListingsAsync(
        string accessToken,
        string spreadsheetId,
        string sheetName,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<JobListingRow>> ReadProfileListingsAsync(
        string accessToken,
        string spreadsheetId,
        string sheetName,
        CancellationToken cancellationToken);

    Task EnsureProfileStatusDropdownAsync(
        string accessToken,
        string spreadsheetId,
        CancellationToken cancellationToken);

    Task AppendListingsAsync(
        string accessToken,
        string spreadsheetId,
        string sheetName,
        IReadOnlyList<JobListingRow> rows,
        CancellationToken cancellationToken);

    Task EnsureProfileInfoSheetAsync(
        string accessToken,
        string spreadsheetId,
        CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<string, string>> ReadProfileInfoAsync(
        string accessToken,
        string spreadsheetId,
        CancellationToken cancellationToken);

    Task WriteProfileInfoAsync(
        string accessToken,
        string spreadsheetId,
        IReadOnlyDictionary<string, string> values,
        CancellationToken cancellationToken);

    Task<CreatedSpreadsheet> EnsureJobMasterWorkbookAsync(
        string accessToken,
        string rootFolderId,
        string? existingSpreadsheetId,
        CancellationToken cancellationToken);

    Task SyncJobMasterProfileManagementAsync(
        string accessToken,
        string spreadsheetId,
        IReadOnlyList<JobMasterProfileRow> rows,
        CancellationToken cancellationToken);
}

public sealed record JobMasterProfileRow(
    string Name,
    string Tab,
    string Sheet,
    string Prompt,
    int? ResumeStyle,
    string Owner);

