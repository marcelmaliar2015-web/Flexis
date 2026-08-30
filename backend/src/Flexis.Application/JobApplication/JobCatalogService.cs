using Flexis.Application.Common;
using Flexis.Application.Google;
using Flexis.Domain.JobApplication;

namespace Flexis.Application.JobApplication;

public sealed class JobCatalogService
{
    private readonly IJobCatalogRepository _items;
    private readonly GoogleAccessTokenService _tokens;
    private readonly IGoogleSheetsWorkspace _sheets;

    public JobCatalogService(
        IJobCatalogRepository items,
        GoogleAccessTokenService tokens,
        IGoogleSheetsWorkspace sheets)
    {
        _items = items;
        _tokens = tokens;
        _sheets = sheets;
    }

    public async Task<IReadOnlyList<JobCatalogItemDto>> ListAsync(
        Guid userId,
        JobCatalogKind kind,
        CancellationToken cancellationToken)
    {
        var items = await _items.ListAsync(userId, kind, cancellationToken);
        return items.Select(ToDto).ToArray();
    }

    public async Task<JobCatalogItemDto> CreateAsync(
        Guid userId,
        JobCatalogKind kind,
        JobCatalogWriteRequest request,
        CancellationToken cancellationToken)
    {
        var title = JobCatalogRules.NormalizeTitle(request.Title);
        if (await _items.TitleExistsAsync(userId, kind, title, null, cancellationToken))
        {
            throw new ConflictException($"A {KindLabel(kind)} with that title already exists.");
        }

        var accessToken = await _tokens.GetAccessTokenAsync(userId, cancellationToken);
        var firstSheet = kind == JobCatalogKind.Profile
            ? JobCatalogRules.SheetTabName(title)
            : JobCatalogRules.DefaultSourceLocation;
        var workbookKind = kind == JobCatalogKind.Profile ? JobWorkbookKind.Profile : JobWorkbookKind.Source;
        var spreadsheet = await _sheets.CreateWorkbookAsync(
            accessToken,
            title,
            firstSheet,
            workbookKind,
            cancellationToken);

        try
        {
            var item = JobCatalogItem.Create(userId, kind, title, spreadsheet.SpreadsheetUrl, spreadsheet.SpreadsheetId);
            await _items.AddAsync(item, cancellationToken);
            await _items.SaveChangesAsync(cancellationToken);
            return ToDto(item);
        }
        catch
        {
            await _sheets.DeleteFileAsync(accessToken, spreadsheet.SpreadsheetId, cancellationToken);
            throw;
        }
    }

    public async Task<JobCatalogItemDto> UpdateAsync(
        Guid userId,
        Guid id,
        JobCatalogWriteRequest request,
        CancellationToken cancellationToken)
    {
        var title = JobCatalogRules.NormalizeTitle(request.Title);
        var item = await RequireItem(userId, id, cancellationToken);
        if (await _items.TitleExistsAsync(userId, item.Kind, title, item.Id, cancellationToken))
        {
            throw new ConflictException($"A {KindLabel(item.Kind)} with that title already exists.");
        }

        if (!string.Equals(item.Title, title, StringComparison.Ordinal) && HasSpreadsheet(item))
        {
            var accessToken = await _tokens.GetAccessTokenAsync(userId, cancellationToken);
            await _sheets.RenameFileAsync(accessToken, item.SpreadsheetId, title, cancellationToken);
            if (item.Kind == JobCatalogKind.Profile)
            {
                var sheets = await _sheets.ListSheetsAsync(accessToken, item.SpreadsheetId, cancellationToken);
                if (sheets.Count == 1)
                {
                    await _sheets.RenameSheetAsync(
                        accessToken,
                        item.SpreadsheetId,
                        sheets[0].SheetId,
                        JobCatalogRules.SheetTabName(title),
                        cancellationToken);
                }
            }
        }

        item.SetTitle(title);
        await _items.SaveChangesAsync(cancellationToken);
        return ToDto(item);
    }

    public async Task DeleteAsync(Guid userId, Guid id, CancellationToken cancellationToken)
    {
        var item = await RequireItem(userId, id, cancellationToken);
        if (HasSpreadsheet(item))
        {
            var accessToken = await _tokens.GetAccessTokenAsync(userId, cancellationToken);
            await _sheets.DeleteFileAsync(accessToken, item.SpreadsheetId, cancellationToken);
        }

        _items.Remove(item);
        await _items.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SourceLocationDto>> ListLocationsAsync(
        Guid userId,
        Guid sourceId,
        CancellationToken cancellationToken)
    {
        var item = await RequireSource(userId, sourceId, cancellationToken);
        var accessToken = await _tokens.GetAccessTokenAsync(userId, cancellationToken);
        var sheets = await _sheets.ListSheetsAsync(accessToken, item.SpreadsheetId, cancellationToken);
        return sheets.Select(sheet => new SourceLocationDto(sheet.SheetId, sheet.Name)).ToArray();
    }

    public async Task<SourceLocationDto> AddLocationAsync(
        Guid userId,
        Guid sourceId,
        SourceLocationWriteRequest request,
        CancellationToken cancellationToken)
    {
        var name = JobCatalogRules.NormalizeLocationName(request.Name);
        var item = await RequireSource(userId, sourceId, cancellationToken);
        var accessToken = await _tokens.GetAccessTokenAsync(userId, cancellationToken);
        var sheets = await _sheets.ListSheetsAsync(accessToken, item.SpreadsheetId, cancellationToken);
        if (sheets.Any(sheet => string.Equals(sheet.Name, name, StringComparison.OrdinalIgnoreCase)))
        {
            throw new ConflictException("A location with that name already exists.");
        }

        var created = await _sheets.AddSourceLocationSheetAsync(accessToken, item.SpreadsheetId, name, cancellationToken);
        return new SourceLocationDto(created.SheetId, created.Name);
    }

    public async Task<SourceLocationDto> RenameLocationAsync(
        Guid userId,
        Guid sourceId,
        int sheetId,
        SourceLocationWriteRequest request,
        CancellationToken cancellationToken)
    {
        var name = JobCatalogRules.NormalizeLocationName(request.Name);
        var item = await RequireSource(userId, sourceId, cancellationToken);
        var accessToken = await _tokens.GetAccessTokenAsync(userId, cancellationToken);
        var sheets = await _sheets.ListSheetsAsync(accessToken, item.SpreadsheetId, cancellationToken);
        if (sheets.All(sheet => sheet.SheetId != sheetId))
        {
            throw new NotFoundException("Location was not found.");
        }

        if (sheets.Any(sheet => sheet.SheetId != sheetId
            && string.Equals(sheet.Name, name, StringComparison.OrdinalIgnoreCase)))
        {
            throw new ConflictException("A location with that name already exists.");
        }

        await _sheets.RenameSheetAsync(accessToken, item.SpreadsheetId, sheetId, name, cancellationToken);
        return new SourceLocationDto(sheetId, name);
    }

    public async Task DeleteLocationAsync(
        Guid userId,
        Guid sourceId,
        int sheetId,
        CancellationToken cancellationToken)
    {
        var item = await RequireSource(userId, sourceId, cancellationToken);
        var accessToken = await _tokens.GetAccessTokenAsync(userId, cancellationToken);
        var sheets = await _sheets.ListSheetsAsync(accessToken, item.SpreadsheetId, cancellationToken);
        if (sheets.All(sheet => sheet.SheetId != sheetId))
        {
            throw new NotFoundException("Location was not found.");
        }

        if (sheets.Count <= 1)
        {
            throw new DomainRuleException("A source must keep at least one location.");
        }

        await _sheets.DeleteSheetAsync(accessToken, item.SpreadsheetId, sheetId, cancellationToken);
    }

    private async Task<JobCatalogItem> RequireItem(Guid userId, Guid id, CancellationToken cancellationToken)
    {
        return await _items.GetByIdAsync(userId, id, cancellationToken)
            ?? throw new NotFoundException("Item was not found.");
    }

    private async Task<JobCatalogItem> RequireSource(Guid userId, Guid id, CancellationToken cancellationToken)
    {
        var item = await RequireItem(userId, id, cancellationToken);
        if (item.Kind != JobCatalogKind.Source)
        {
            throw new NotFoundException("Source was not found.");
        }

        if (!HasSpreadsheet(item))
        {
            throw new ValidationFailedException("This source has no Google Sheet.");
        }

        return item;
    }

    private static bool HasSpreadsheet(JobCatalogItem item)
    {
        return !string.IsNullOrWhiteSpace(item.SpreadsheetId);
    }

    private static JobCatalogItemDto ToDto(JobCatalogItem item)
    {
        return new JobCatalogItemDto(item.Id, item.Title, item.CreatedAt, item.Url, item.SpreadsheetId);
    }

    private static string KindLabel(JobCatalogKind itemKind)
    {
        return itemKind == JobCatalogKind.Profile ? "profile" : "source";
    }
}
