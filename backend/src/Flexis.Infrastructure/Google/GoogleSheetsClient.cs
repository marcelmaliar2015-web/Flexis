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

    private static readonly string[] StatusValues = ["Applied", "Interview", "Invalid", "Expired", "Other"];

    private const string FlexisLockDescription = "Flexis owner lock";

    private static readonly (string Value, Color Bg, Color Fg)[] StatusColors =
    [
        ("Applied", new Color(0.902, 0.957, 0.925), new Color(0.106, 0.498, 0.306)),
        ("Interview", new Color(0.910, 0.851, 0.980), new Color(0.404, 0.176, 0.620)),
        ("Invalid", new Color(0.988, 0.910, 0.902), new Color(0.706, 0.137, 0.094)),
        ("Expired", new Color(0.996, 0.941, 0.780), new Color(0.710, 0.278, 0.031)),
        ("Other", new Color(0.820, 0.914, 1.0), new Color(0.090, 0.361, 0.827))
    ];

    private readonly HttpClient _http;

    public GoogleSheetsClient(HttpClient http)
    {
        _http = http;
        _http.Timeout = TimeSpan.FromSeconds(30);
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
            $"https://sheets.googleapis.com/v4/spreadsheets/{spreadsheetId}?fields=sheets.properties(sheetId,gridProperties.rowCount)",
            null,
            cancellationToken);

        var requests = new List<object>();
        foreach (var sheet in payload.Sheets ?? [])
        {
            if (sheet.Properties is null)
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
                requests.Add(new
                {
                    repeatCell = new
                    {
                        range = new { sheetId, startRowIndex = 1, endRowIndex = rowCount },
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

        var payload = await SendJson<SpreadsheetList>(
            accessToken,
            HttpMethod.Get,
            $"https://sheets.googleapis.com/v4/spreadsheets/{spreadsheetId}?fields=sheets(properties(sheetId,title),protectedRanges(protectedRangeId,description))",
            null,
            cancellationToken);

        var requests = new List<object>();
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

                requests.Add(new { deleteProtectedRange = new { protectedRangeId = range.ProtectedRangeId } });
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

            requests.Add(new
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
            $"https://sheets.googleapis.com/v4/spreadsheets/{spreadsheetId}?fields=sheets.properties(sheetId,title)",
            null,
            cancellationToken);

        var requests = new List<object>();
        foreach (var sheet in payload.Sheets ?? [])
        {
            if (sheet.Properties is null)
            {
                continue;
            }

            var sheetId = sheet.Properties.SheetId;
            var statusIndex = Array.FindIndex(ColumnsFor(JobWorkbookKind.Profile), column => column.Name == "Status");
            requests.Add(new
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
                    },
                    rule = new
                    {
                        condition = new
                        {
                            type = "ONE_OF_LIST",
                            values = StatusValues.Select(value => new { userEnteredValue = value }).ToArray()
                        },
                        showCustomUi = true,
                        strict = false
                    }
                }
            });
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

    public Task AppendListingsAsync(
        string accessToken,
        string spreadsheetId,
        string sheetName,
        IReadOnlyList<JobListingRow> rows,
        CancellationToken cancellationToken)
    {
        if (rows.Count == 0)
        {
            return Task.CompletedTask;
        }

        return SendJson<object>(
            accessToken,
            HttpMethod.Post,
            $"https://sheets.googleapis.com/v4/spreadsheets/{spreadsheetId}/values/{ValuesRange(sheetName, "A2:D")}:append?valueInputOption=USER_ENTERED&insertDataOption=INSERT_ROWS",
            new
            {
                values = rows.Select(row => new[] { row.CompanyName, row.Position, row.Link, row.Jd }).ToArray()
            },
            cancellationToken);
    }

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
        if (kind == JobWorkbookKind.Profile && JobSheetNames.IsArchiveTab(sheetTitle))
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

    private static object[] FormatRequests(int sheetId, Column[] columns)
    {
        var statusIndex = Array.FindIndex(columns, column => column.Name == "Status");
        var requests = new List<object>
        {
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
                        endColumnIndex = columns.Length
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
                updateDimensionProperties = new
                {
                    range = new { sheetId, dimension = "ROWS", startIndex = 0, endIndex = 200 },
                    properties = new { pixelSize = 21 },
                    fields = "pixelSize"
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
                        endRowIndex = 200,
                        startColumnIndex = 0,
                        endColumnIndex = columns.Length
                    },
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
            },
            new
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
                            headerColor = new Color(0.055, 0.153, 0.267),
                            firstBandColor = new Color(1.0, 0.988, 0.969),
                            secondBandColor = new Color(0.957, 0.945, 0.922)
                        }
                    }
                }
            },
            new
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
            }
        };

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
            requests.Add(new
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
                    },
                    rule = new
                    {
                        condition = new
                        {
                            type = "ONE_OF_LIST",
                            values = StatusValues.Select(value => new { userEnteredValue = value }).ToArray()
                        },
                        showCustomUi = true,
                        strict = false
                    }
                }
            });

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
        }

        return requests.ToArray();
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
        return Uri.EscapeDataString($"'{sheetName.Replace("'", "''", StringComparison.Ordinal)}'!{cells}");
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
