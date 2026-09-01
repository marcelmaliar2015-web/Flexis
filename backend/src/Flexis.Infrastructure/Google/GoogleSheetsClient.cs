using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Flexis.Application.Common;
using Flexis.Application.Google;

namespace Flexis.Infrastructure.Google;

internal sealed class GoogleSheetsClient : IGoogleSheetsWorkspace
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private static readonly string[] StatusValues = ["Applied", "Interview", "Banned", "Invalid", "Expired", "Other"];

    private const string FlexisLockDescription = "Flexis owner lock";

    private static readonly (string Value, Color Bg, Color Fg)[] StatusColors =
    [
        ("Applied", new Color(0.910, 0.961, 0.914), new Color(0.180, 0.490, 0.196)),
        ("Interview", new Color(0.953, 0.898, 0.961), new Color(0.416, 0.106, 0.604)),
        ("Banned", new Color(0.988, 0.894, 0.925), new Color(0.678, 0.079, 0.341)),
        ("Invalid", new Color(1.0, 0.922, 0.933), new Color(0.776, 0.157, 0.157)),
        ("Expired", new Color(1.0, 0.973, 0.882), new Color(0.961, 0.498, 0.090)),
        ("Other", new Color(0.890, 0.949, 0.992), new Color(0.082, 0.396, 0.753))
    ];

    private static readonly Color ListingsHeaderBackground = new(0.055, 0.153, 0.267);

    private static readonly Color ListingsHeaderForeground = new(0.969, 0.957, 0.937);

    private static readonly Color ListingsFirstBandBackground = new(1.0, 0.988, 0.969);

    private static readonly Color ListingsSecondBandBackground = new(0.957, 0.945, 0.922);

    private readonly HttpClient _http;

    public GoogleSheetsClient(HttpClient http)
    {
        _http = http;
        _http.Timeout = TimeSpan.FromSeconds(120);
    }

    public async Task<CreatedSpreadsheet> CreateWorkbookAsync(
        string accessToken,
        string fileName,
        string firstSheetName,
        JobWorkbookKind kind,
        string parentFolderId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(parentFolderId))
        {
            throw new GoogleOAuthException("Google Drive folder is missing.");
        }

        var columns = ColumnsFor(kind);
        var createdFile = await SendJson<DriveFileCreated>(
            accessToken,
            HttpMethod.Post,
            "https://www.googleapis.com/drive/v3/files?fields=id&supportsAllDrives=true",
            new
            {
                name = fileName,
                mimeType = "application/vnd.google-apps.spreadsheet",
                parents = new[] { parentFolderId }
            },
            cancellationToken);

        if (string.IsNullOrWhiteSpace(createdFile.Id))
        {
            throw new GoogleOAuthException("Google Drive did not return a spreadsheet.");
        }

        var spreadsheet = await SendJson<SpreadsheetCreated>(
            accessToken,
            HttpMethod.Get,
            $"https://sheets.googleapis.com/v4/spreadsheets/{createdFile.Id}?fields=spreadsheetId,spreadsheetUrl,sheets.properties(sheetId,title)",
            null,
            cancellationToken);

        var sheetId = spreadsheet.Sheets?.FirstOrDefault()?.Properties?.SheetId;
        if (string.IsNullOrWhiteSpace(spreadsheet.SpreadsheetId)
            || string.IsNullOrWhiteSpace(spreadsheet.SpreadsheetUrl)
            || sheetId is null)
        {
            throw new GoogleOAuthException("Google Sheets did not return a spreadsheet.");
        }

        var tabId = sheetId.Value;

        var requests = new List<object>
        {
            new
            {
                updateSheetProperties = new
                {
                    properties = new
                    {
                        sheetId = tabId,
                        title = firstSheetName,
                        gridProperties = new
                        {
                            frozenRowCount = 1,
                            rowCount = 200,
                            columnCount = columns.Length
                        }
                    },
                    fields = "title,gridProperties.frozenRowCount,gridProperties.rowCount,gridProperties.columnCount"
                }
            },
            new
            {
                updateCells = new
                {
                    start = new { sheetId = tabId, rowIndex = 0, columnIndex = 0 },
                    rows = new[] { new { values = columns.Select(HeaderCell).ToArray() } },
                    fields = "userEnteredValue"
                }
            }
        };
        requests.AddRange(FormatRequests(tabId, columns));

        await SendJson<object>(
            accessToken,
            HttpMethod.Post,
            $"https://sheets.googleapis.com/v4/spreadsheets/{createdFile.Id}:batchUpdate",
            new { requests },
            cancellationToken);

        return new CreatedSpreadsheet(spreadsheet.SpreadsheetId, spreadsheet.SpreadsheetUrl);
    }

    public Task RenameFileAsync(
        string accessToken,
        string spreadsheetId,
        string fileName,
        CancellationToken cancellationToken)
    {
        return SendJson<object>(
            accessToken,
            HttpMethod.Patch,
            $"https://www.googleapis.com/drive/v3/files/{spreadsheetId}",
            new { name = fileName },
            cancellationToken);
    }

    public Task RenameSheetAsync(
        string accessToken,
        string spreadsheetId,
        int sheetId,
        string name,
        CancellationToken cancellationToken)
    {
        return SendJson<object>(
            accessToken,
            HttpMethod.Post,
            $"https://sheets.googleapis.com/v4/spreadsheets/{spreadsheetId}:batchUpdate",
            new
            {
                requests = new[]
                {
                    new
                    {
                        updateSheetProperties = new
                        {
                            properties = new { sheetId, title = name },
                            fields = "title"
                        }
                    }
                }
            },
            cancellationToken);
    }

    public Task<SpreadsheetSheet> AddSourceLocationSheetAsync(
        string accessToken,
        string spreadsheetId,
        string name,
        CancellationToken cancellationToken)
    {
        return AddFormattedSheetAsync(accessToken, spreadsheetId, name, JobWorkbookKind.Source, null, cancellationToken);
    }

    public async Task<SpreadsheetSheet> ReplaceProfileMainSheetAsync(
        string accessToken,
        string spreadsheetId,
        int currentMainSheetId,
        string archiveTabName,
        string newMainTabName,
        CancellationToken cancellationToken)
    {
        var columns = ColumnsFor(JobWorkbookKind.Profile);
        var newSheetId = Random.Shared.Next(1, int.MaxValue);
        var requests = new List<object>
        {
            new
            {
                updateSheetProperties = new
                {
                    properties = new { sheetId = currentMainSheetId, title = archiveTabName },
                    fields = "title"
                }
            }
        };
        requests.AddRange(AddSheetRequests(newSheetId, newMainTabName, columns, 0));

        await SendJson<object>(
            accessToken,
            HttpMethod.Post,
            $"https://sheets.googleapis.com/v4/spreadsheets/{spreadsheetId}:batchUpdate",
            new { requests },
            cancellationToken);

        return new SpreadsheetSheet(newSheetId, newMainTabName);
    }

    public Task DeleteSheetAsync(
        string accessToken,
        string spreadsheetId,
        int sheetId,
        CancellationToken cancellationToken)
    {
        return SendJson<object>(
            accessToken,
            HttpMethod.Post,
            $"https://sheets.googleapis.com/v4/spreadsheets/{spreadsheetId}:batchUpdate",
            new { requests = new[] { new { deleteSheet = new { sheetId } } } },
            cancellationToken);
    }

    public async Task<IReadOnlyList<SpreadsheetSheet>> ListSheetsAsync(
        string accessToken,
        string spreadsheetId,
        CancellationToken cancellationToken)
    {
        var payload = await SendJson<SpreadsheetList>(
            accessToken,
            HttpMethod.Get,
            $"https://sheets.googleapis.com/v4/spreadsheets/{spreadsheetId}?fields=sheets.properties(sheetId,title)",
            null,
            cancellationToken);

        return (payload.Sheets ?? [])
            .Select(sheet => sheet.Properties)
            .Where(properties => properties is not null)
            .Select(properties => new SpreadsheetSheet(properties!.SheetId, properties.Title ?? string.Empty))
            .ToArray();
    }

    public async Task DeleteFileAsync(string accessToken, string spreadsheetId, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Delete,
            $"https://www.googleapis.com/drive/v3/files/{spreadsheetId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        using var response = await _http.SendAsync(request, cancellationToken);
        if (response.StatusCode is System.Net.HttpStatusCode.NotFound or System.Net.HttpStatusCode.NoContent or System.Net.HttpStatusCode.OK)
        {
            return;
        }

        if (!response.IsSuccessStatusCode)
        {
            var payload = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new GoogleOAuthException(ReadGoogleError(payload, "Google Drive delete failed."));
        }
    }

    public async Task SetFixedRowHeightAsync(
        string accessToken,
        string spreadsheetId,
        CancellationToken cancellationToken)
    {
        var payload = await SendJson<SpreadsheetList>(
            accessToken,
            HttpMethod.Get,
            $"https://sheets.googleapis.com/v4/spreadsheets/{spreadsheetId}?fields=sheets.properties(sheetId,title,gridProperties.rowCount)",
            null,
            cancellationToken);

        var requests = new List<object>();
        foreach (var sheet in payload.Sheets ?? [])
        {
            if (sheet.Properties is null || JobSheetNames.IsProfileInfoTab(sheet.Properties.Title))
            {
                continue;
            }

            var sheetId = sheet.Properties.SheetId;
            var rowCount = sheet.Properties.GridProperties?.RowCount ?? 200;
            if (rowCount < 1)
            {
                rowCount = 1;
            }

            requests.Add(new
            {
                updateDimensionProperties = new
                {
                    range = new { sheetId, dimension = "ROWS", startIndex = 0, endIndex = rowCount },
                    properties = new { pixelSize = 21 },
                    fields = "pixelSize"
                }
            });
            requests.Add(new
            {
                repeatCell = new
                {
                    range = new { sheetId, startRowIndex = 0, endRowIndex = 1 },
                    cell = new { userEnteredFormat = new { wrapStrategy = "WRAP" } },
                    fields = "userEnteredFormat.wrapStrategy"
                }
            });
            if (rowCount > 1)
            {
                requests.Add(BodyCellFormatRequest(sheetId, 1, rowCount));
            }
        }

        if (requests.Count == 0)
        {
            return;
        }

        await SendJson<object>(
            accessToken,
            HttpMethod.Post,
            $"https://sheets.googleapis.com/v4/spreadsheets/{spreadsheetId}:batchUpdate",
            new { requests },
            cancellationToken);
    }

    public async Task RemoveStatusColumnAsync(
        string accessToken,
        string spreadsheetId,
        CancellationToken cancellationToken)
    {
        var sheets = await ListSheetsAsync(accessToken, spreadsheetId, cancellationToken);
        var requests = new List<object>();
        foreach (var sheet in sheets)
        {
            var payload = await SendJson<SheetValues>(
                accessToken,
                HttpMethod.Get,
                $"https://sheets.googleapis.com/v4/spreadsheets/{spreadsheetId}/values/{ValuesRange(sheet.Name, "1:1")}",
                null,
                cancellationToken);
            var headers = payload.Values?.FirstOrDefault() ?? [];
            for (var index = headers.Count - 1; index >= 0; index--)
            {
                if (!string.Equals(Cell(headers, index), "Status", StringComparison.Ordinal))
                {
                    continue;
                }

                requests.Add(new
                {
                    deleteDimension = new
                    {
                        range = new
                        {
                            sheetId = sheet.SheetId,
                            dimension = "COLUMNS",
                            startIndex = index,
                            endIndex = index + 1
                        }
                    }
                });
            }
        }

        if (requests.Count == 0)
        {
            return;
        }

        await SendJson<object>(
            accessToken,
            HttpMethod.Post,
            $"https://sheets.googleapis.com/v4/spreadsheets/{spreadsheetId}:batchUpdate",
            new { requests },
            cancellationToken);
    }

    public async Task ProtectWorkbookAsync(
        string accessToken,
        string spreadsheetId,
        string ownerEmail,
        JobWorkbookKind kind,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(ownerEmail))
        {
            return;
        }

        for (var attempt = 0; attempt < 3; attempt++)
        {
            try
            {
                await ApplyWorkbookProtectionAsync(
                    accessToken,
                    spreadsheetId,
                    ownerEmail,
                    kind,
                    cancellationToken);
                return;
            }
            catch (GoogleOAuthException exception)
                when (attempt < 2 && IsStaleProtectedRangeError(exception.Message))
            {
            }
        }
    }

    private async Task ApplyWorkbookProtectionAsync(
        string accessToken,
        string spreadsheetId,
        string ownerEmail,
        JobWorkbookKind kind,
        CancellationToken cancellationToken)
    {
        var payload = await SendJson<SpreadsheetList>(
            accessToken,
            HttpMethod.Get,
            $"https://sheets.googleapis.com/v4/spreadsheets/{spreadsheetId}?fields=sheets(properties(sheetId,title),protectedRanges(protectedRangeId,description))",
            null,
            cancellationToken);

        var deleteRequests = new List<object>();
        var addRequests = new List<object>();
        foreach (var sheet in payload.Sheets ?? [])
        {
            if (sheet.Properties is null)
            {
                continue;
            }

            var sheetId = sheet.Properties.SheetId;
            var invitedColumns = InvitedEditColumnIndexes(kind, sheet.Properties.Title);
            foreach (var range in sheet.ProtectedRanges ?? [])
            {
                if (!string.Equals(range.Description, FlexisLockDescription, StringComparison.Ordinal))
                {
                    continue;
                }

                deleteRequests.Add(new { deleteProtectedRange = new { protectedRangeId = range.ProtectedRangeId } });
            }

            object? unprotectedRanges = invitedColumns.Length == 0
                ? null
                : invitedColumns
                    .Select(index => new
                    {
                        sheetId,
                        startColumnIndex = index,
                        endColumnIndex = index + 1
                    })
                    .ToArray();

            addRequests.Add(new
            {
                addProtectedRange = new
                {
                    protectedRange = new
                    {
                        range = new { sheetId },
                        description = FlexisLockDescription,
                        warningOnly = false,
                        unprotectedRanges,
                        editors = new
                        {
                            users = new[] { ownerEmail },
                            domainUsersCanEdit = false
                        }
                    }
                }
            });
        }

        if (deleteRequests.Count == 0 && addRequests.Count == 0)
        {
            return;
        }

        if (deleteRequests.Count > 0)
        {
            await SendJson<object>(
                accessToken,
                HttpMethod.Post,
                $"https://sheets.googleapis.com/v4/spreadsheets/{spreadsheetId}:batchUpdate",
                new { requests = deleteRequests },
                cancellationToken);
        }

        if (addRequests.Count > 0)
        {
            await SendJson<object>(
                accessToken,
                HttpMethod.Post,
                $"https://sheets.googleapis.com/v4/spreadsheets/{spreadsheetId}:batchUpdate",
                new { requests = addRequests },
                cancellationToken);
        }
    }

    private static bool IsStaleProtectedRangeError(string message)
    {
        return message.Contains("No protected range with id", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<IReadOnlyList<JobListingRow>> ReadListingsAsync(
        string accessToken,
        string spreadsheetId,
        string sheetName,
        CancellationToken cancellationToken)
    {
        var payload = await SendJson<SheetValues>(
            accessToken,
            HttpMethod.Get,
            $"https://sheets.googleapis.com/v4/spreadsheets/{spreadsheetId}/values/{ValuesRange(sheetName, "A2:D")}",
            null,
            cancellationToken);

        return (payload.Values ?? [])
            .Select(ToListing)
            .ToArray();
    }

    public async Task SetProfileListingStatusesAsync(
        string accessToken,
        string spreadsheetId,
        string sheetName,
        IReadOnlyList<ProfileListingStatusUpdate> updates,
        CancellationToken cancellationToken)
    {
        if (updates.Count == 0)
        {
            return;
        }

        var columns = ColumnsFor(JobWorkbookKind.Profile);
        var statusIndex = Array.FindIndex(columns, column => column.Name == "Status");
        if (statusIndex < 0)
        {
            return;
        }

        var statusColumn = ColumnLetter(statusIndex);
        await SendJson<object>(
            accessToken,
            HttpMethod.Post,
            $"https://sheets.googleapis.com/v4/spreadsheets/{spreadsheetId}/values:batchUpdate",
            new
            {
                valueInputOption = "USER_ENTERED",
                data = updates.Select(update => new
                {
                    range = SheetRange(sheetName, $"{statusColumn}{update.RowNumber}"),
                    values = new[] { new[] { update.Status } }
                }).ToArray()
            },
            cancellationToken);
    }

    public async Task<IReadOnlyList<JobListingRow>> ReadProfileListingsAsync(
        string accessToken,
        string spreadsheetId,
        string sheetName,
        CancellationToken cancellationToken)
    {
        var payload = await SendJson<SheetValues>(
            accessToken,
            HttpMethod.Get,
            $"https://sheets.googleapis.com/v4/spreadsheets/{spreadsheetId}/values/{ValuesRange(sheetName, "A2:F")}",
            null,
            cancellationToken);

        return (payload.Values ?? [])
            .Select(ToListing)
            .ToArray();
    }

    public async Task EnsureProfileStatusDropdownAsync(
        string accessToken,
        string spreadsheetId,
        CancellationToken cancellationToken)
    {
        var payload = await SendJson<SpreadsheetList>(
            accessToken,
            HttpMethod.Get,
            $"https://sheets.googleapis.com/v4/spreadsheets/{spreadsheetId}?fields=sheets.properties(sheetId,title),sheets.bandedRanges(bandedRangeId,range),sheets.tables(tableId,name,range,columnProperties),sheets.conditionalFormats",
            null,
            cancellationToken);

        var columns = ColumnsFor(JobWorkbookKind.Profile);
        var statusIndex = Array.FindIndex(columns, column => column.Name == "Status");
        if (statusIndex < 0)
        {
            return;
        }

        var prepRequests = new List<object>();
        var tableRequests = new List<object>();
        var styleRequests = new List<object>();
        foreach (var sheet in payload.Sheets ?? [])
        {
            if (sheet.Properties is null || JobSheetNames.IsProfileInfoTab(sheet.Properties.Title))
            {
                continue;
            }

            var sheetId = sheet.Properties.SheetId;
            var existingTable = (sheet.Tables ?? [])
                .FirstOrDefault(table => string.Equals(table.TableId, ListingsTableId(sheetId), StringComparison.Ordinal))
                ?? (sheet.Tables ?? []).FirstOrDefault(table => table.Range?.SheetId == sheetId);
            if (existingTable?.TableId is { } tableId)
            {
                tableRequests.Add(UpdateListingsTableStatusColumnRequest(tableId, statusIndex, columns[statusIndex].Name));
                styleRequests.Add(UpdateListingsTableRowsPropertiesRequest(tableId));
                styleRequests.AddRange(ListingsTableSurfaceFormatRequests(sheetId, columns.Length));
                styleRequests.AddRange(DeleteConditionalFormatRequests(sheetId, sheet.ConditionalFormats?.Count ?? 0));
                styleRequests.AddRange(StatusConditionalFormatRequests(sheetId, statusIndex));
                continue;
            }

            foreach (var banding in sheet.BandedRanges ?? [])
            {
                prepRequests.Add(new { deleteBanding = new { bandedRangeId = banding.BandedRangeId } });
            }

            prepRequests.Add(new { clearBasicFilter = new { sheetId } });
            prepRequests.Add(ClearStatusDataValidationRequest(sheetId, statusIndex));
            tableRequests.Add(AddListingsTableRequest(sheetId, columns));
            styleRequests.AddRange(ListingsTableSurfaceFormatRequests(sheetId, columns.Length));
            styleRequests.AddRange(DeleteConditionalFormatRequests(sheetId, sheet.ConditionalFormats?.Count ?? 0));
            styleRequests.AddRange(StatusConditionalFormatRequests(sheetId, statusIndex));
        }

        if (prepRequests.Count == 0 && tableRequests.Count == 0 && styleRequests.Count == 0)
        {
            return;
        }

        if (prepRequests.Count > 0)
        {
            await SendJson<object>(
                accessToken,
                HttpMethod.Post,
                $"https://sheets.googleapis.com/v4/spreadsheets/{spreadsheetId}:batchUpdate",
                new { requests = prepRequests },
                cancellationToken);
        }

        if (tableRequests.Count > 0)
        {
            await SendJson<object>(
                accessToken,
                HttpMethod.Post,
                $"https://sheets.googleapis.com/v4/spreadsheets/{spreadsheetId}:batchUpdate",
                new { requests = tableRequests },
                cancellationToken);
        }

        if (styleRequests.Count > 0)
        {
            await SendJson<object>(
                accessToken,
                HttpMethod.Post,
                $"https://sheets.googleapis.com/v4/spreadsheets/{spreadsheetId}:batchUpdate",
                new { requests = styleRequests },
                cancellationToken);
        }
    }

    public async Task AppendListingsAsync(
        string accessToken,
        string spreadsheetId,
        string sheetName,
        IReadOnlyList<JobListingRow> rows,
        CancellationToken cancellationToken)
    {
        if (rows.Count == 0)
        {
            return;
        }

        var appendResult = await SendJson<ValuesAppendResponse>(
            accessToken,
            HttpMethod.Post,
            $"https://sheets.googleapis.com/v4/spreadsheets/{spreadsheetId}/values/{ValuesRange(sheetName, "A2:D")}:append?valueInputOption=USER_ENTERED&insertDataOption=INSERT_ROWS",
            new
            {
                values = rows.Select(row => new[] { row.CompanyName, row.Position, row.Link, row.Jd }).ToArray()
            },
            cancellationToken);

        if (!TryParseAppendedRowRange(appendResult.Updates?.UpdatedRange, out var startRow, out var endRow))
        {
            return;
        }

        var sheets = await ListSheetsAsync(accessToken, spreadsheetId, cancellationToken);
        var sheet = sheets.FirstOrDefault(item => string.Equals(item.Name, sheetName, StringComparison.Ordinal));
        if (sheet is null)
        {
            return;
        }

        var columnCount = ColumnsFor(JobWorkbookKind.Profile).Length;
        var requests = ListingsDataRowFormatRequests(
            sheet.SheetId,
            startRow - 1,
            endRow,
            columnCount);
        await SendJson<object>(
            accessToken,
            HttpMethod.Post,
            $"https://sheets.googleapis.com/v4/spreadsheets/{spreadsheetId}:batchUpdate",
            new { requests },
            cancellationToken);
    }

    public async Task EnsureProfileInfoSheetAsync(
        string accessToken,
        string spreadsheetId,
        CancellationToken cancellationToken)
    {
        var sheets = await ListSheetsAsync(accessToken, spreadsheetId, cancellationToken);
        if (sheets.Any(sheet => JobSheetNames.IsProfileInfoTab(sheet.Name)))
        {
            return;
        }

        var fields = ProfileInfoFields;
        var sheetId = Random.Shared.Next(1, int.MaxValue);
        var rows = new List<object>
        {
            new
            {
                values = new[]
                {
                    HeaderCell(new Column("Field", 220)),
                    HeaderCell(new Column("Value", 360))
                }
            }
        };
        foreach (var field in fields)
        {
            rows.Add(new
            {
                values = new object[]
                {
                    new { userEnteredValue = new { stringValue = field } },
                    new { userEnteredValue = new { stringValue = string.Empty } }
                }
            });
        }

        var requests = new List<object>
        {
            new
            {
                addSheet = new
                {
                    properties = new
                    {
                        sheetId,
                        title = JobSheetNames.ProfileInfoTab,
                        index = 1,
                        gridProperties = new
                        {
                            frozenRowCount = 1,
                            rowCount = Math.Max(fields.Length + 5, 20),
                            columnCount = 2
                        }
                    }
                }
            },
            new
            {
                updateCells = new
                {
                    start = new { sheetId, rowIndex = 0, columnIndex = 0 },
                    rows,
                    fields = "userEnteredValue"
                }
            }
        };
        requests.AddRange(FormatRequests(sheetId, [new Column("Field", 220), new Column("Value", 360)]));
        requests.Add(new
        {
            updateDimensionProperties = new
            {
                range = new
                {
                    sheetId,
                    dimension = "ROWS",
                    startIndex = 0,
                    endIndex = fields.Length + 5
                },
                properties = new { pixelSize = 28 },
                fields = "pixelSize"
            }
        });

        await SendJson<object>(
            accessToken,
            HttpMethod.Post,
            $"https://sheets.googleapis.com/v4/spreadsheets/{spreadsheetId}:batchUpdate",
            new { requests },
            cancellationToken);
    }

    public async Task<IReadOnlyDictionary<string, string>> ReadProfileInfoAsync(
        string accessToken,
        string spreadsheetId,
        CancellationToken cancellationToken)
    {
        await EnsureProfileInfoSheetAsync(accessToken, spreadsheetId, cancellationToken);
        var payload = await SendJson<SheetValues>(
            accessToken,
            HttpMethod.Get,
            $"https://sheets.googleapis.com/v4/spreadsheets/{spreadsheetId}/values/{ValuesRange(JobSheetNames.ProfileInfoTab, "A2:B20")}",
            null,
            cancellationToken);

        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var field in ProfileInfoFields)
        {
            map[field] = string.Empty;
        }

        foreach (var row in payload.Values ?? [])
        {
            var label = Cell(row, 0).Trim();
            if (label.Length == 0 || !map.ContainsKey(label))
            {
                continue;
            }

            map[label] = Cell(row, 1);
        }

        return map;
    }

    public async Task WriteProfileInfoAsync(
        string accessToken,
        string spreadsheetId,
        IReadOnlyDictionary<string, string> values,
        CancellationToken cancellationToken)
    {
        await EnsureProfileInfoSheetAsync(accessToken, spreadsheetId, cancellationToken);
        var rows = ProfileInfoFields
            .Select(field => new[]
            {
                field,
                values.TryGetValue(field, out var value) ? value : string.Empty
            })
            .ToArray();

        await SendJson<object>(
            accessToken,
            HttpMethod.Put,
            $"https://sheets.googleapis.com/v4/spreadsheets/{spreadsheetId}/values/{ValuesRange(JobSheetNames.ProfileInfoTab, "A2:B11")}?valueInputOption=USER_ENTERED",
            new { values = rows },
            cancellationToken);
    }

    private const string JobMasterProfileManagementSheet = "Profile Management";

    private static readonly string[] JobMasterProfileManagementHeaders =
    [
        "Name",
        "Tab",
        "Sheet",
        "Prompt",
        "Resume Style",
        "Owner"
    ];

    public async Task<CreatedSpreadsheet> EnsureJobMasterWorkbookAsync(
        string accessToken,
        string rootFolderId,
        string? existingSpreadsheetId,
        CancellationToken cancellationToken)
    {
        string spreadsheetId;
        if (!string.IsNullOrWhiteSpace(existingSpreadsheetId))
        {
            var active = await SendJson<SpreadsheetCreated>(
                accessToken,
                HttpMethod.Get,
                $"https://sheets.googleapis.com/v4/spreadsheets/{existingSpreadsheetId}?fields=spreadsheetId,spreadsheetUrl",
                null,
                cancellationToken);
            if (!string.IsNullOrWhiteSpace(active.SpreadsheetId) && !string.IsNullOrWhiteSpace(active.SpreadsheetUrl))
            {
                spreadsheetId = active.SpreadsheetId;
                await EnsureJobMasterProfileManagementSheetAsync(accessToken, spreadsheetId, cancellationToken);
                return new CreatedSpreadsheet(spreadsheetId, active.SpreadsheetUrl);
            }
        }

        var createdFile = await SendJson<DriveFileCreated>(
            accessToken,
            HttpMethod.Post,
            "https://www.googleapis.com/drive/v3/files?fields=id&supportsAllDrives=true",
            new
            {
                name = FlexisDriveLayout.JobMasterFileName,
                mimeType = "application/vnd.google-apps.spreadsheet",
                parents = new[] { rootFolderId }
            },
            cancellationToken);

        if (string.IsNullOrWhiteSpace(createdFile.Id))
        {
            throw new GoogleOAuthException("Google Drive did not return a spreadsheet.");
        }

        spreadsheetId = createdFile.Id;
        var spreadsheet = await SendJson<SpreadsheetCreated>(
            accessToken,
            HttpMethod.Get,
            $"https://sheets.googleapis.com/v4/spreadsheets/{spreadsheetId}?fields=spreadsheetId,spreadsheetUrl,sheets.properties(sheetId,title)",
            null,
            cancellationToken);

        if (string.IsNullOrWhiteSpace(spreadsheet.SpreadsheetId) || string.IsNullOrWhiteSpace(spreadsheet.SpreadsheetUrl))
        {
            throw new GoogleOAuthException("Google Sheets did not return a spreadsheet.");
        }

        await EnsureJobMasterProfileManagementSheetAsync(accessToken, spreadsheet.SpreadsheetId, cancellationToken);
        return new CreatedSpreadsheet(spreadsheet.SpreadsheetId, spreadsheet.SpreadsheetUrl);
    }

    public async Task SyncJobMasterProfileManagementAsync(
        string accessToken,
        string spreadsheetId,
        IReadOnlyList<JobMasterProfileRow> rows,
        CancellationToken cancellationToken)
    {
        await EnsureJobMasterProfileManagementSheetAsync(accessToken, spreadsheetId, cancellationToken);
        var values = new List<object[]>(rows.Count + 1)
        {
            JobMasterProfileManagementHeaders
        };
        foreach (var row in rows)
        {
            values.Add(
            [
                row.Name,
                row.Tab,
                row.Sheet,
                row.Prompt,
                row.ResumeStyle?.ToString() ?? string.Empty,
                row.Owner
            ]);
        }

        var endRow = Math.Max(values.Count, 2);
        await SendJson<object>(
            accessToken,
            HttpMethod.Put,
            $"https://sheets.googleapis.com/v4/spreadsheets/{spreadsheetId}/values/{ValuesRange(JobMasterProfileManagementSheet, $"A1:F{endRow}")}?valueInputOption=USER_ENTERED",
            new { values },
            cancellationToken);

        var sheets = await ListSheetsAsync(accessToken, spreadsheetId, cancellationToken);
        var sheet = sheets.FirstOrDefault(item => string.Equals(item.Name, JobMasterProfileManagementSheet, StringComparison.Ordinal))
            ?? throw new GoogleOAuthException("Profile Management sheet was not found.");
        await SendJson<object>(
            accessToken,
            HttpMethod.Post,
            $"https://sheets.googleapis.com/v4/spreadsheets/{spreadsheetId}:batchUpdate",
            new
            {
                requests = JobMasterProfileManagementFormatRequests(sheet.SheetId, endRow)
            },
            cancellationToken);
    }

    private async Task EnsureJobMasterProfileManagementSheetAsync(
        string accessToken,
        string spreadsheetId,
        CancellationToken cancellationToken)
    {
        var sheets = await ListSheetsAsync(accessToken, spreadsheetId, cancellationToken);
        var profileManagement = sheets.FirstOrDefault(item =>
            string.Equals(item.Name, JobMasterProfileManagementSheet, StringComparison.Ordinal));
        if (profileManagement is null)
        {
            var first = sheets.FirstOrDefault()
                ?? throw new GoogleOAuthException("Google Sheets returned no tabs.");
            await SendJson<object>(
                accessToken,
                HttpMethod.Post,
                $"https://sheets.googleapis.com/v4/spreadsheets/{spreadsheetId}:batchUpdate",
                new
                {
                    requests = new object[]
                    {
                        new
                        {
                            updateSheetProperties = new
                            {
                                properties = new
                                {
                                    sheetId = first.SheetId,
                                    title = JobMasterProfileManagementSheet,
                                    gridProperties = new
                                    {
                                        frozenRowCount = 1,
                                        rowCount = 500,
                                        columnCount = JobMasterProfileManagementHeaders.Length
                                    }
                                },
                                fields = "title,gridProperties.frozenRowCount,gridProperties.rowCount,gridProperties.columnCount"
                            }
                        }
                    }
                },
                cancellationToken);
            profileManagement = new SpreadsheetSheet(first.SheetId, JobMasterProfileManagementSheet);
        }

        await SendJson<object>(
            accessToken,
            HttpMethod.Put,
            $"https://sheets.googleapis.com/v4/spreadsheets/{spreadsheetId}/values/{ValuesRange(JobMasterProfileManagementSheet, "A1:F1")}?valueInputOption=USER_ENTERED",
            new { values = new[] { JobMasterProfileManagementHeaders } },
            cancellationToken);
        await SendJson<object>(
            accessToken,
            HttpMethod.Post,
            $"https://sheets.googleapis.com/v4/spreadsheets/{spreadsheetId}:batchUpdate",
            new
            {
                requests = JobMasterProfileManagementFormatRequests(profileManagement.SheetId, 500)
            },
            cancellationToken);
    }

    private static object[] JobMasterProfileManagementFormatRequests(int sheetId, int rowCount)
    {
        return
        [
            new
            {
                repeatCell = new
                {
                    range = new
                    {
                        sheetId,
                        startRowIndex = 0,
                        endRowIndex = 1,
                        startColumnIndex = 0,
                        endColumnIndex = JobMasterProfileManagementHeaders.Length
                    },
                    cell = new
                    {
                        userEnteredFormat = new
                        {
                            backgroundColor = new Color(0.055, 0.153, 0.267),
                            horizontalAlignment = "LEFT",
                            verticalAlignment = "MIDDLE",
                            wrapStrategy = "WRAP",
                            textFormat = new
                            {
                                foregroundColor = new Color(0.969, 0.957, 0.937),
                                fontFamily = "Calibri",
                                fontSize = 11,
                                bold = true
                            }
                        }
                    },
                    fields = "userEnteredFormat(backgroundColor,textFormat,horizontalAlignment,verticalAlignment,wrapStrategy)"
                }
            },
            new
            {
                repeatCell = new
                {
                    range = new
                    {
                        sheetId,
                        startRowIndex = 1,
                        endRowIndex = rowCount,
                        startColumnIndex = 0,
                        endColumnIndex = JobMasterProfileManagementHeaders.Length
                    },
                    cell = new
                    {
                        userEnteredFormat = new
                        {
                            horizontalAlignment = "LEFT",
                            verticalAlignment = "TOP",
                            wrapStrategy = "WRAP",
                            textFormat = new
                            {
                                foregroundColor = new Color(0, 0, 0),
                                fontFamily = "Calibri",
                                fontSize = 11
                            }
                        }
                    },
                    fields = "userEnteredFormat(textFormat,horizontalAlignment,verticalAlignment,wrapStrategy)"
                }
            },
            new
            {
                updateDimensionProperties = new
                {
                    range = new { sheetId, dimension = "ROWS", startIndex = 0, endIndex = rowCount },
                    properties = new { pixelSize = 21 },
                    fields = "pixelSize"
                }
            },
            new
            {
                updateDimensionProperties = new
                {
                    range = new { sheetId, dimension = "COLUMNS", startIndex = 0, endIndex = 1 },
                    properties = new { pixelSize = 180 },
                    fields = "pixelSize"
                }
            },
            new
            {
                updateDimensionProperties = new
                {
                    range = new { sheetId, dimension = "COLUMNS", startIndex = 1, endIndex = 2 },
                    properties = new { pixelSize = 180 },
                    fields = "pixelSize"
                }
            },
            new
            {
                updateDimensionProperties = new
                {
                    range = new { sheetId, dimension = "COLUMNS", startIndex = 2, endIndex = 3 },
                    properties = new { pixelSize = 280 },
                    fields = "pixelSize"
                }
            },
            new
            {
                updateDimensionProperties = new
                {
                    range = new { sheetId, dimension = "COLUMNS", startIndex = 3, endIndex = 4 },
                    properties = new { pixelSize = 360 },
                    fields = "pixelSize"
                }
            },
            new
            {
                updateDimensionProperties = new
                {
                    range = new { sheetId, dimension = "COLUMNS", startIndex = 4, endIndex = 5 },
                    properties = new { pixelSize = 120 },
                    fields = "pixelSize"
                }
            },
            new
            {
                updateDimensionProperties = new
                {
                    range = new { sheetId, dimension = "COLUMNS", startIndex = 5, endIndex = 6 },
                    properties = new { pixelSize = 160 },
                    fields = "pixelSize"
                }
            }
        ];
    }

    private static readonly string[] ProfileInfoFields =
    [
        "Name",
        "Address",
        "Mail",
        "Password",
        "LinkedIn",
        "Phone",
        "Sex",
        "Target Rate (Monthly)",
        "Race",
        "Veteran Status"
    ];

    private static Column[] ColumnsFor(JobWorkbookKind kind)
    {
        return kind == JobWorkbookKind.Profile
            ?
            [
                new Column("Company Name", 180),
                new Column("Position", 180),
                new Column("Link", 220),
                new Column("JD", 280),
                new Column("Download", 140),
                new Column("Status", 130),
                new Column("Issue", 200)
            ]
            :
            [
                new Column("Company Name", 200),
                new Column("Position", 200),
                new Column("Link", 240),
                new Column("JD", 300)
            ];
    }

    private static int[] InvitedEditColumnIndexes(JobWorkbookKind kind, string? sheetTitle)
    {
        if (kind == JobWorkbookKind.Profile
            && (JobSheetNames.IsArchiveTab(sheetTitle) || JobSheetNames.IsProfileInfoTab(sheetTitle)))
        {
            return [];
        }

        var columns = ColumnsFor(kind);
        return columns
            .Select((column, index) => (column.Name, index))
            .Where(column => column.Name is "Status" or "Issue")
            .Select(column => column.index)
            .ToArray();
    }

    private async Task<SpreadsheetSheet> AddFormattedSheetAsync(
        string accessToken,
        string spreadsheetId,
        string name,
        JobWorkbookKind kind,
        int? index,
        CancellationToken cancellationToken)
    {
        var columns = ColumnsFor(kind);
        var sheetId = Random.Shared.Next(1, int.MaxValue);
        await SendJson<object>(
            accessToken,
            HttpMethod.Post,
            $"https://sheets.googleapis.com/v4/spreadsheets/{spreadsheetId}:batchUpdate",
            new { requests = AddSheetRequests(sheetId, name, columns, index) },
            cancellationToken);

        return new SpreadsheetSheet(sheetId, name);
    }

    private static object[] AddSheetRequests(int sheetId, string name, Column[] columns, int? index)
    {
        object properties = index is { } sheetIndex
            ? new
            {
                sheetId,
                title = name,
                index = sheetIndex,
                gridProperties = new
                {
                    frozenRowCount = 1,
                    rowCount = 200,
                    columnCount = columns.Length
                }
            }
            : new
            {
                sheetId,
                title = name,
                gridProperties = new
                {
                    frozenRowCount = 1,
                    rowCount = 200,
                    columnCount = columns.Length
                }
            };

        return new object[]
        {
            new { addSheet = new { properties } },
            new
            {
                updateCells = new
                {
                    start = new { sheetId, rowIndex = 0, columnIndex = 0 },
                    rows = new[] { new { values = columns.Select(HeaderCell).ToArray() } },
                    fields = "userEnteredValue"
                }
            }
        }.Concat(FormatRequests(sheetId, columns)).ToArray();
    }

    private static object HeaderCell(Column column)
    {
        return new { userEnteredValue = new { stringValue = column.Name } };
    }

    private static object BodyCellFormatRequest(
        int sheetId,
        int startRowIndex,
        int endRowIndex,
        int? endColumnIndex = null)
    {
        object range = endColumnIndex is int columnCount
            ? new
            {
                sheetId,
                startRowIndex,
                endRowIndex,
                startColumnIndex = 0,
                endColumnIndex = columnCount
            }
            : new
            {
                sheetId,
                startRowIndex,
                endRowIndex
            };

        return new
        {
            repeatCell = new
            {
                range,
                cell = new
                {
                    userEnteredFormat = new
                    {
                        wrapStrategy = "WRAP",
                        textFormat = new
                        {
                            foregroundColor = new Color(0, 0, 0),
                            fontFamily = "Calibri",
                            fontSize = 11
                        }
                    }
                },
                fields = "userEnteredFormat.wrapStrategy,userEnteredFormat.textFormat.foregroundColor,userEnteredFormat.textFormat.fontFamily,userEnteredFormat.textFormat.fontSize"
            }
        };
    }

    private static bool TryParseAppendedRowRange(string? updatedRange, out int startRow, out int endRow)
    {
        startRow = 0;
        endRow = 0;
        if (string.IsNullOrWhiteSpace(updatedRange))
        {
            return false;
        }

        var separator = updatedRange.LastIndexOf('!');
        if (separator < 0 || separator >= updatedRange.Length - 1)
        {
            return false;
        }

        var cells = updatedRange[(separator + 1)..].Split(':');
        if (cells.Length != 2)
        {
            return false;
        }

        return TryParseSheetRow(cells[0], out startRow) && TryParseSheetRow(cells[1], out endRow);
    }

    private static bool TryParseSheetRow(string cellReference, out int row)
    {
        row = 0;
        var digits = new string(cellReference.Where(char.IsDigit).ToArray());
        return int.TryParse(digits, out row) && row > 0;
    }

    private static string ListingsTableId(int sheetId) => $"flexis-listings-{sheetId}";

    private static string ListingsTableName(int sheetId) => $"Flexis Listings {sheetId}";

    private static object StatusDropdownValidationRule()
    {
        return new
        {
            condition = new
            {
                type = "ONE_OF_LIST",
                values = StatusValues.Select(value => new { userEnteredValue = value }).ToArray()
            }
        };
    }

    private static object ListingsTableRowsProperties()
    {
        return new
        {
            headerColorStyle = RgbColorStyle(ListingsHeaderBackground),
            firstBandColorStyle = RgbColorStyle(ListingsFirstBandBackground),
            secondBandColorStyle = RgbColorStyle(ListingsSecondBandBackground)
        };
    }

    private static object RgbColorStyle(Color color)
    {
        return new { rgbColor = new { red = color.Red, green = color.Green, blue = color.Blue } };
    }

    private static object UpdateListingsTableRowsPropertiesRequest(string tableId)
    {
        return new
        {
            updateTable = new
            {
                table = new
                {
                    tableId,
                    rowsProperties = ListingsTableRowsProperties()
                },
                fields = "rowsProperties"
            }
        };
    }

    private static object ListingsHeaderFormatRequest(int sheetId, int columnCount)
    {
        return new
        {
            repeatCell = new
            {
                range = new
                {
                    sheetId,
                    startRowIndex = 0,
                    endRowIndex = 1,
                    startColumnIndex = 0,
                    endColumnIndex = columnCount
                },
                cell = new
                {
                    userEnteredFormat = new
                    {
                        backgroundColor = ListingsHeaderBackground,
                        horizontalAlignment = "LEFT",
                        verticalAlignment = "MIDDLE",
                        wrapStrategy = "WRAP",
                        textFormat = new
                        {
                            foregroundColor = ListingsHeaderForeground,
                            fontFamily = "Calibri",
                            fontSize = 11,
                            bold = true
                        }
                    }
                },
                fields = "userEnteredFormat(backgroundColor,textFormat,horizontalAlignment,verticalAlignment,wrapStrategy)"
            }
        };
    }

    private static object ListingsDataRowFormatRequest(int sheetId, int rowIndex, int columnCount)
    {
        var background = rowIndex % 2 == 1
            ? ListingsFirstBandBackground
            : ListingsSecondBandBackground;
        return new
        {
            repeatCell = new
            {
                range = new
                {
                    sheetId,
                    startRowIndex = rowIndex,
                    endRowIndex = rowIndex + 1,
                    startColumnIndex = 0,
                    endColumnIndex = columnCount
                },
                cell = new
                {
                    userEnteredFormat = new
                    {
                        backgroundColor = background,
                        horizontalAlignment = "LEFT",
                        verticalAlignment = "TOP",
                        wrapStrategy = "WRAP",
                        textFormat = new
                        {
                            foregroundColor = new Color(0, 0, 0),
                            fontFamily = "Calibri",
                            fontSize = 11
                        }
                    }
                },
                fields = "userEnteredFormat(backgroundColor,textFormat,horizontalAlignment,verticalAlignment,wrapStrategy)"
            }
        };
    }

    private static object[] ListingsDataRowFormatRequests(
        int sheetId,
        int startRowIndex,
        int endRowIndex,
        int columnCount)
    {
        var requests = new List<object>(Math.Max(endRowIndex - startRowIndex, 0));
        for (var rowIndex = startRowIndex; rowIndex < endRowIndex; rowIndex++)
        {
            requests.Add(ListingsDataRowFormatRequest(sheetId, rowIndex, columnCount));
        }

        return requests.ToArray();
    }

    private static object[] ListingsTableSurfaceFormatRequests(int sheetId, int columnCount)
    {
        return [ListingsHeaderFormatRequest(sheetId, columnCount)];
    }

    private static object[] ListingsTableColumnProperties(Column[] columns)
    {
        return columns.Select((column, index) =>
        {
            if (column.Name == "Status")
            {
                return (object)new
                {
                    columnIndex = index,
                    columnName = column.Name,
                    columnType = "DROPDOWN",
                    dataValidationRule = StatusDropdownValidationRule()
                };
            }

            return (object)new { columnIndex = index, columnName = column.Name };
        }).ToArray();
    }

    private static object AddListingsTableRequest(int sheetId, Column[] columns)
    {
        return new
        {
            addTable = new
            {
                table = new
                {
                    name = ListingsTableName(sheetId),
                    tableId = ListingsTableId(sheetId),
                    range = new
                    {
                        sheetId,
                        startRowIndex = 0,
                        endRowIndex = 200,
                        startColumnIndex = 0,
                        endColumnIndex = columns.Length
                    },
                    columnProperties = ListingsTableColumnProperties(columns),
                    rowsProperties = ListingsTableRowsProperties()
                }
            }
        };
    }

    private static object UpdateListingsTableStatusColumnRequest(string tableId, int statusIndex, string statusColumnName)
    {
        return new
        {
            updateTable = new
            {
                table = new
                {
                    tableId,
                    columnProperties = new[]
                    {
                        new
                        {
                            columnIndex = statusIndex,
                            columnName = statusColumnName,
                            columnType = "DROPDOWN",
                            dataValidationRule = StatusDropdownValidationRule()
                        }
                    }
                },
                fields = "columnProperties"
            }
        };
    }

    private static object ClearStatusDataValidationRequest(int sheetId, int statusIndex)
    {
        return new
        {
            setDataValidation = new
            {
                range = new
                {
                    sheetId,
                    startRowIndex = 1,
                    endRowIndex = 200,
                    startColumnIndex = statusIndex,
                    endColumnIndex = statusIndex + 1
                }
            }
        };
    }

    private static object[] FormatRequests(int sheetId, Column[] columns)
    {
        var statusIndex = Array.FindIndex(columns, column => column.Name == "Status");
        var usesListingsTable = statusIndex >= 0;
        var requests = new List<object>
        {
            ListingsHeaderFormatRequest(sheetId, columns.Length),
            new
            {
                updateDimensionProperties = new
                {
                    range = new { sheetId, dimension = "ROWS", startIndex = 0, endIndex = 200 },
                    properties = new { pixelSize = 21 },
                    fields = "pixelSize"
                }
            },
            BodyCellFormatRequest(sheetId, 1, 200, columns.Length)
        };

        if (!usesListingsTable)
        {
            requests.Add(new
            {
                addBanding = new
                {
                    bandedRange = new
                    {
                        range = new
                        {
                            sheetId,
                            startRowIndex = 0,
                            endRowIndex = 200,
                            startColumnIndex = 0,
                            endColumnIndex = columns.Length
                        },
                        rowProperties = new
                        {
                            headerColor = ListingsHeaderBackground,
                            firstBandColor = ListingsFirstBandBackground,
                            secondBandColor = ListingsSecondBandBackground
                        }
                    }
                }
            });
            requests.Add(new
            {
                setBasicFilter = new
                {
                    filter = new
                    {
                        range = new
                        {
                            sheetId,
                            startRowIndex = 0,
                            endRowIndex = 200,
                            startColumnIndex = 0,
                            endColumnIndex = columns.Length
                        }
                    }
                }
            });
        }

        for (var index = 0; index < columns.Length; index++)
        {
            var column = columns[index];
            requests.Add(new
            {
                updateDimensionProperties = new
                {
                    range = new { sheetId, dimension = "COLUMNS", startIndex = index, endIndex = index + 1 },
                    properties = new { pixelSize = column.Width },
                    fields = "pixelSize"
                }
            });
        }

        if (statusIndex >= 0)
        {
            requests.Add(AddListingsTableRequest(sheetId, columns));
            requests.AddRange(ListingsTableSurfaceFormatRequests(sheetId, columns.Length));
            requests.AddRange(StatusConditionalFormatRequests(sheetId, statusIndex));
        }

        return requests.ToArray();
    }

    private static object[] StatusConditionalFormatRequests(int sheetId, int statusIndex)
    {
        var requests = new List<object>();
        foreach (var status in StatusColors)
        {
            requests.Add(new
            {
                addConditionalFormatRule = new
                {
                    rule = new
                    {
                        ranges = new[]
                        {
                            new
                            {
                                sheetId,
                                startRowIndex = 1,
                                endRowIndex = 200,
                                startColumnIndex = statusIndex,
                                endColumnIndex = statusIndex + 1
                            }
                        },
                        booleanRule = new
                        {
                            condition = new
                            {
                                type = "TEXT_EQ",
                                values = new[] { new { userEnteredValue = status.Value } }
                            },
                            format = new
                            {
                                backgroundColor = status.Bg,
                                textFormat = new { foregroundColor = status.Fg, bold = true }
                            }
                        }
                    },
                    index = 0
                }
            });
        }

        return requests.ToArray();
    }

    private static object[] DeleteConditionalFormatRequests(int sheetId, int ruleCount)
    {
        var requests = new List<object>();
        for (var index = ruleCount - 1; index >= 0; index--)
        {
            requests.Add(new { deleteConditionalFormatRule = new { sheetId, index } });
        }

        return requests.ToArray();
    }

    private static string ColumnLetter(int zeroBasedIndex)
    {
        var quotient = zeroBasedIndex;
        var letter = "";
        do
        {
            letter = (char)('A' + quotient % 26) + letter;
            quotient = quotient / 26 - 1;
        }
        while (quotient >= 0);

        return letter;
    }

    private async Task<T> SendJson<T>(
        string accessToken,
        HttpMethod method,
        string url,
        object? body,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        if (body is not null)
        {
            request.Content = new StringContent(JsonSerializer.Serialize(body, JsonOptions), Encoding.UTF8, "application/json");
        }

        using var response = await _http.SendAsync(request, cancellationToken);
        var payload = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new GoogleOAuthException(ReadGoogleError(payload, "Google Sheets request failed."));
        }

        if (typeof(T) == typeof(object) || string.IsNullOrWhiteSpace(payload))
        {
            return default!;
        }

        return JsonSerializer.Deserialize<T>(payload, JsonOptions)
            ?? throw new GoogleOAuthException("Google Sheets returned an empty payload.");
    }

    private static string ReadGoogleError(string payload, string fallback)
    {
        try
        {
            var error = JsonSerializer.Deserialize<GoogleErrorEnvelope>(payload, JsonOptions);
            if (!string.IsNullOrWhiteSpace(error?.Error?.Message))
            {
                return error.Error.Message;
            }
        }
        catch (JsonException)
        {
        }

        return fallback;
    }

    private static string ValuesRange(string sheetName, string cells)
    {
        return Uri.EscapeDataString(SheetRange(sheetName, cells));
    }

    private static string SheetRange(string sheetName, string cells)
    {
        return $"'{sheetName.Replace("'", "''", StringComparison.Ordinal)}'!{cells}";
    }

    private static JobListingRow ToListing(List<JsonElement> row)
    {
        return new JobListingRow(Cell(row, 0), Cell(row, 1), Cell(row, 2), Cell(row, 3), Cell(row, 5));
    }

    private static string Cell(List<JsonElement> row, int index)
    {
        if (index >= row.Count)
        {
            return string.Empty;
        }

        var cell = row[index];
        return cell.ValueKind switch
        {
            JsonValueKind.String => cell.GetString() ?? string.Empty,
            JsonValueKind.Number => cell.ToString(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            _ => string.Empty
        };
    }

    private sealed class ValuesAppendResponse
    {
        public ValuesAppendUpdates? Updates { get; set; }
    }

    private sealed class ValuesAppendUpdates
    {
        public string? UpdatedRange { get; set; }
    }

    private sealed record Column(string Name, int Width);

    private sealed record Color(double Red, double Green, double Blue);

    private sealed class SpreadsheetCreated
    {
        public string? SpreadsheetId { get; set; }

        public string? SpreadsheetUrl { get; set; }

        public List<SheetEnvelope>? Sheets { get; set; }
    }

    private sealed class DriveFileCreated
    {
        public string? Id { get; set; }
    }

    private sealed class SpreadsheetList
    {
        public List<SheetEnvelope>? Sheets { get; set; }
    }

    private sealed class SheetEnvelope
    {
        public SheetProps? Properties { get; set; }

        public List<ProtectedRangeInfo>? ProtectedRanges { get; set; }

        public List<BandedRangeInfo>? BandedRanges { get; set; }

        public List<TableInfo>? Tables { get; set; }

        public List<ConditionalFormatInfo>? ConditionalFormats { get; set; }
    }

    private sealed class ConditionalFormatInfo
    {
    }

    private sealed class BandedRangeInfo
    {
        public int BandedRangeId { get; set; }

        public GridRangeInfo? Range { get; set; }
    }

    private sealed class TableInfo
    {
        public string? TableId { get; set; }

        public string? Name { get; set; }

        public GridRangeInfo? Range { get; set; }

        public List<TableColumnProperty>? ColumnProperties { get; set; }
    }

    private sealed class TableColumnProperty
    {
        public int? ColumnIndex { get; set; }

        public string? ColumnName { get; set; }

        public string? ColumnType { get; set; }
    }

    private sealed class GridRangeInfo
    {
        public int? SheetId { get; set; }
    }

    private sealed class ProtectedRangeInfo
    {
        public int ProtectedRangeId { get; set; }

        public string? Description { get; set; }
    }

    private sealed class SheetProps
    {
        public int SheetId { get; set; }

        public string? Title { get; set; }

        public GridProps? GridProperties { get; set; }
    }

    private sealed class GridProps
    {
        public int RowCount { get; set; }
    }

    private sealed class SheetValues
    {
        public List<List<JsonElement>>? Values { get; set; }
    }

    private sealed class GoogleErrorEnvelope
    {
        public GoogleErrorBody? Error { get; set; }
    }

    private sealed class GoogleErrorBody
    {
        public string? Message { get; set; }
    }
}
