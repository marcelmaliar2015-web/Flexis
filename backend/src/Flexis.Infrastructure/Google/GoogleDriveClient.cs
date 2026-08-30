using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Flexis.Application.Common;
using Flexis.Application.Google;

namespace Flexis.Infrastructure.Google;

internal sealed class GoogleDriveClient : IGoogleDriveGateway
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly HttpClient _http;

    public GoogleDriveClient(HttpClient http)
    {
        _http = http;
        _http.Timeout = TimeSpan.FromSeconds(15);
    }

    public async Task<bool> FolderIsActiveAsync(
        string accessToken,
        string folderId,
        CancellationToken cancellationToken)
    {
        var file = await GetFileAsync(accessToken, folderId, "id,mimeType,trashed", cancellationToken);
        return file is not null
            && file.Trashed != true
            && string.Equals(file.MimeType, "application/vnd.google-apps.folder", StringComparison.Ordinal);
    }

    public async Task<string> CreateFolderAsync(
        string accessToken,
        string name,
        string? parentFolderId,
        string description,
        CancellationToken cancellationToken)
    {
        var body = new
        {
            name,
            description,
            mimeType = "application/vnd.google-apps.folder",
            parents = string.IsNullOrWhiteSpace(parentFolderId) ? null : new[] { parentFolderId }
        };
        var created = await SendJson<DriveFile>(
            accessToken,
            HttpMethod.Post,
            "https://www.googleapis.com/drive/v3/files?fields=id",
            body,
            cancellationToken);
        if (string.IsNullOrWhiteSpace(created.Id))
        {
            throw new GoogleOAuthException("Google Drive did not return a folder.");
        }

        return created.Id;
    }

    public async Task<string?> FindFolderAsync(
        string accessToken,
        string name,
        string? parentFolderId,
        CancellationToken cancellationToken)
    {
        var parent = string.IsNullOrWhiteSpace(parentFolderId) ? "root" : parentFolderId;
        var query =
            $"name = '{EscapeDriveQuery(name)}' and mimeType = 'application/vnd.google-apps.folder' and trashed = false and '{EscapeDriveQuery(parent)}' in parents";
        var listed = await SendJson<DriveFileList>(
            accessToken,
            HttpMethod.Get,
            $"https://www.googleapis.com/drive/v3/files?q={Uri.EscapeDataString(query)}&fields=files(id)&pageSize=1&orderBy=createdTime",
            null,
            cancellationToken);
        var id = listed?.Files?.FirstOrDefault()?.Id;
        return string.IsNullOrWhiteSpace(id) ? null : id;
    }

    public async Task MoveFileToFolderAsync(
        string accessToken,
        string fileId,
        string folderId,
        CancellationToken cancellationToken)
    {
        var file = await GetFileAsync(accessToken, fileId, "id,parents,trashed", cancellationToken);
        if (file is null || file.Trashed == true)
        {
            return;
        }

        var parents = file.Parents ?? [];
        if (parents.Count == 1 && string.Equals(parents[0], folderId, StringComparison.Ordinal))
        {
            return;
        }

        var remove = string.Join(',', parents.Where(parent => !string.Equals(parent, folderId, StringComparison.Ordinal)));
        var query = $"addParents={Uri.EscapeDataString(folderId)}&fields=id";
        if (remove.Length > 0)
        {
            query += $"&removeParents={Uri.EscapeDataString(remove)}";
        }

        await SendJson<DriveFile>(
            accessToken,
            HttpMethod.Patch,
            $"https://www.googleapis.com/drive/v3/files/{fileId}?{query}",
            new Dictionary<string, string>(),
            cancellationToken);
    }

    private async Task<DriveFile?> GetFileAsync(
        string accessToken,
        string fileId,
        string fields,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"https://www.googleapis.com/drive/v3/files/{fileId}?fields={Uri.EscapeDataString(fields)}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        using var response = await _http.SendAsync(request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        var payload = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new GoogleOAuthException(ReadGoogleError(payload, "Google Drive request failed."));
        }

        return JsonSerializer.Deserialize<DriveFile>(payload, JsonOptions);
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
            throw new GoogleOAuthException(ReadGoogleError(payload, "Google Drive request failed."));
        }

        if (typeof(T) == typeof(object) || string.IsNullOrWhiteSpace(payload))
        {
            return default!;
        }

        return JsonSerializer.Deserialize<T>(payload, JsonOptions)
            ?? throw new GoogleOAuthException("Google Drive returned an empty payload.");
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

    private static string EscapeDriveQuery(string value)
    {
        return value.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("'", "\\'", StringComparison.Ordinal);
    }

    private sealed class DriveFile
    {
        public string? Id { get; set; }

        public string? MimeType { get; set; }

        public bool? Trashed { get; set; }

        public List<string>? Parents { get; set; }
    }

    private sealed class DriveFileList
    {
        public List<DriveFile>? Files { get; set; }
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
