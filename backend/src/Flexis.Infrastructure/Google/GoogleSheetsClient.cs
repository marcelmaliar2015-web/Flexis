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

    private static readonly string[] StatusValues = ["Applied", "Invalid", "Expired", "Other"];

    private static readonly (string Value, Color Bg, Color Fg)[] StatusColors =
    [
        ("Applied", new Color(0.902, 0.957, 0.925), new Color(0.106, 0.498, 0.306)),
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

    public async Task<SpreadsheetSheet> AddSourceLocationSheetAsync(
        string accessToken,
        string spreadsheetId,
        string name,
        CancellationToken cancellationToken)
    {
        var columns = ColumnsFor(JobWorkbookKind.Source);
        var sheetId = Random.Shared.Next(1, int.MaxValue);
        var addBody = new
        {
            requests = new object[]
            {
                new
                {
                    addSheet = new
                    {
                        properties = new
                        {
                            sheetId,
                            title = name,
                            gridProperties = new
                            {
                                frozenRowCount = 1,
                                rowCount = 200,
                                columnCount = columns.Length
                            }
                        }
                    }
                },
                new
                {
                    updateCells = new
                    {
                        start = new { sheetId, rowIndex = 0, columnIndex = 0 },
                        rows = new[] { new { values = columns.Select(HeaderCell).ToArray() } },
                        fields = "userEnteredValue"
                    }
                }
            }.Concat(FormatRequests(sheetId, columns)).ToArray()
        };

        await SendJson<object>(
            accessToken,
            HttpMethod.Post,
            $"https://sheets.googleapis.com/v4/spreadsheets/{spreadsheetId}:batchUpdate",
            addBody,
            cancellationToken);

        return new SpreadsheetSheet(sheetId, name);
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
                new Column("JD", 300),
                new Column("Status", 130)
            ];
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
                            textFormat = new
                            {
                                foregroundColor = new Color(0.969, 0.957, 0.937),
                                fontFamily = "Calibri",
                                fontSize = 11,
                                bold = true
                            }
                        }
                    },
                    fields = "userEnteredFormat(backgroundColor,textFormat,horizontalAlignment,verticalAlignment)"
                }
            },
            new
            {
                updateDimensionProperties = new
                {
                    range = new { sheetId, dimension = "ROWS", startIndex = 0, endIndex = 1 },
                    properties = new { pixelSize = 36 },
                    fields = "pixelSize"
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
        return new JobListingRow(Cell(row, 0), Cell(row, 1), Cell(row, 2), Cell(row, 3));
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
    }

    private sealed class SheetProps
    {
        public int SheetId { get; set; }

        public string? Title { get; set; }
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
