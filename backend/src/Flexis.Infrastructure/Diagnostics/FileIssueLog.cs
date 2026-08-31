using System.Text.Json;
using Flexis.Application.Diagnostics;
using Microsoft.Extensions.Hosting;

namespace Flexis.Infrastructure.Diagnostics;

internal sealed class FileIssueLog : IIssueLog
{
    private const int KeepLines = 200;
    private const long RotateBytes = 512_000;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly string _path;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public FileIssueLog(IHostEnvironment environment)
    {
        _path = ResolvePath(environment.ContentRootPath);
    }

    public async Task WriteAsync(IssueLogEntry entry, CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(_path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var line = JsonSerializer.Serialize(entry, JsonOptions);
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (File.Exists(_path))
            {
                var info = new FileInfo(_path);
                if (info.Length > RotateBytes)
                {
                    var lines = await File.ReadAllLinesAsync(_path, cancellationToken);
                    var kept = lines.Length > KeepLines ? lines[^KeepLines..] : lines;
                    await File.WriteAllLinesAsync(_path, kept, cancellationToken);
                }
            }

            await File.AppendAllTextAsync(_path, line + Environment.NewLine, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    private static string ResolvePath(string contentRoot)
    {
        var current = new DirectoryInfo(contentRoot);
        while (current is not null)
        {
            var architecture = Path.Combine(current.FullName, "architecture", "README.md");
            if (File.Exists(architecture))
            {
                return Path.Combine(current.FullName, ".flexis", "issue-log.jsonl");
            }

            current = current.Parent;
        }

        return Path.Combine(contentRoot, "issue-log.jsonl");
    }
}
