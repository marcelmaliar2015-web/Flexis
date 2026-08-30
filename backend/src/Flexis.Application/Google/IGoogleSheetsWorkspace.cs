namespace Flexis.Application.Google;

public enum JobWorkbookKind
{
    Profile,
    Source
}

public sealed record CreatedSpreadsheet(string SpreadsheetId, string SpreadsheetUrl);

public sealed record SpreadsheetSheet(int SheetId, string Name);

public interface IGoogleSheetsWorkspace
{
    Task<CreatedSpreadsheet> CreateWorkbookAsync(
        string accessToken,
        string fileName,
        string firstSheetName,
        JobWorkbookKind kind,
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
}
