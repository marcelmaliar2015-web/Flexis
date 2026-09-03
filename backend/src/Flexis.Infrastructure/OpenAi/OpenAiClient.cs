using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Flexis.Application.Common;
using Flexis.Application.MailCheck;
using Flexis.Domain.MailCheck;

namespace Flexis.Infrastructure.OpenAi;

internal sealed class OpenAiClient : IOpenAiGateway
{
    private const int DefaultOutputTokens = 256;
    private const int ReasoningOutputTokens = 768;
    private const int MaxOutputTokens = 2048;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly HttpClient _http;

    public OpenAiClient(HttpClient http)
    {
        _http = http;
        _http.BaseAddress = new Uri("https://api.openai.com/");
        _http.Timeout = TimeSpan.FromSeconds(120);
    }

    public async Task<IReadOnlyList<OpenAiModelInfo>> ListModelsAsync(string apiKey, CancellationToken cancellationToken)
    {
        var payload = await SendAsync<ModelList>(apiKey, HttpMethod.Get, "v1/models", null, cancellationToken);
        return (payload.Data ?? [])
            .Select(item => item.Id)
            .Where(id => !string.IsNullOrWhiteSpace(id) && IsChatModel(id!))
            .Select(id => new OpenAiModelInfo(id!))
            .DistinctBy(item => item.Id, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public async Task<MailCheckClassification> ClassifyAsync(
        string apiKey,
        string model,
        string classifierPrompt,
        string mailText,
        CancellationToken cancellationToken)
    {
        var prompt = string.IsNullOrWhiteSpace(classifierPrompt)
            ? MailCheckClassifierPrompt.Default
            : classifierPrompt.Trim();
        var shape = ShapeFor(model);
        var userText = $"{prompt}\n\n---\n\n{mailText}";
        ValidationFailedException? last = null;
        for (var attempt = 0; attempt < 6; attempt++)
        {
            try
            {
                var content = shape.UseResponsesApi
                    ? await ClassifyWithResponsesAsync(apiKey, model, userText, shape, cancellationToken)
                    : await ClassifyWithChatAsync(apiKey, model, mailText, prompt, userText, shape, cancellationToken);
                return ParseClassification(content);
            }
            catch (ValidationFailedException exception)
            {
                last = exception;
                if (!Adapt(shape, exception.Message))
                {
                    throw;
                }
            }
        }

        throw last ?? new ValidationFailedException("OpenAI classification failed.");
    }

    private async Task<string> ClassifyWithChatAsync(
        string apiKey,
        string model,
        string mailText,
        string classifierPrompt,
        string combined,
        RequestShape shape,
        CancellationToken cancellationToken)
    {
        object messages = shape.SystemAsUser
            ? new object[] { new { role = "user", content = combined } }
            : new object[]
            {
                new { role = "system", content = classifierPrompt },
                new { role = "user", content = mailText }
            };
        var body = new Dictionary<string, object?>
        {
            ["model"] = model,
            ["messages"] = messages
        };
        if (shape.UseCompletionTokens)
        {
            body["max_completion_tokens"] = shape.OutputTokens;
        }
        else
        {
            body["max_tokens"] = shape.OutputTokens;
        }

        if (shape.IncludeTemperature)
        {
            body["temperature"] = 0;
        }

        if (shape.IncludeJsonFormat)
        {
            body["response_format"] = new { type = "json_object" };
        }

        var payload = await SendAsync<ChatResponse>(apiKey, HttpMethod.Post, "v1/chat/completions", body, cancellationToken);
        var content = payload.Choices?.FirstOrDefault()?.Message?.Content;
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new ValidationFailedException("OpenAI returned an empty classification.");
        }

        return content;
    }

    private async Task<string> ClassifyWithResponsesAsync(
        string apiKey,
        string model,
        string input,
        RequestShape shape,
        CancellationToken cancellationToken)
    {
        var body = new Dictionary<string, object?>
        {
            ["model"] = model,
            ["input"] = input
        };
        if (shape.UseCompletionTokens)
        {
            body["max_output_tokens"] = shape.OutputTokens;
        }

        if (shape.IncludeJsonFormat)
        {
            body["text"] = new { format = new { type = "json_object" } };
        }

        var payload = await SendAsync<ResponsesPayload>(apiKey, HttpMethod.Post, "v1/responses", body, cancellationToken);
        if (!string.IsNullOrWhiteSpace(payload.OutputText))
        {
            return payload.OutputText;
        }

        var fromOutput = ReadResponsesText(payload);
        if (string.IsNullOrWhiteSpace(fromOutput))
        {
            throw new ValidationFailedException("OpenAI returned an empty classification.");
        }

        return fromOutput;
    }

    private async Task<T> SendAsync<T>(
        string apiKey,
        HttpMethod method,
        string path,
        object? body,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        if (body is not null)
        {
            request.Content = new StringContent(JsonSerializer.Serialize(body, JsonOptions), Encoding.UTF8, "application/json");
        }

        using var response = await _http.SendAsync(request, cancellationToken);
        var payload = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new ValidationFailedException(ReadError(payload, "OpenAI request failed."));
        }

        if (string.IsNullOrWhiteSpace(payload))
        {
            throw new ValidationFailedException("OpenAI returned an empty payload.");
        }

        return JsonSerializer.Deserialize<T>(payload, JsonOptions)
            ?? throw new ValidationFailedException("OpenAI returned an empty payload.");
    }

    private static RequestShape ShapeFor(string model)
    {
        var name = model.Trim().ToLowerInvariant();
        var reasoning = name.StartsWith("o1", StringComparison.Ordinal)
            || name.StartsWith("o3", StringComparison.Ordinal)
            || name.StartsWith("o4", StringComparison.Ordinal)
            || name.StartsWith("gpt-5", StringComparison.Ordinal)
            || name.Contains("reason", StringComparison.Ordinal);
        return new RequestShape
        {
            UseResponsesApi = false,
            UseCompletionTokens = reasoning,
            IncludeTemperature = !reasoning,
            IncludeJsonFormat = !reasoning,
            SystemAsUser = reasoning,
            OutputTokens = reasoning ? ReasoningOutputTokens : DefaultOutputTokens
        };
    }

    private static bool Adapt(RequestShape shape, string message)
    {
        var text = message.ToLowerInvariant();
        if (text.Contains("empty classification", StringComparison.Ordinal)
            || text.Contains("empty payload", StringComparison.Ordinal))
        {
            var adapted = false;
            if (shape.OutputTokens < MaxOutputTokens)
            {
                shape.OutputTokens = Math.Min(MaxOutputTokens, Math.Max(shape.OutputTokens * 2, ReasoningOutputTokens));
                adapted = true;
            }

            if (shape.IncludeJsonFormat)
            {
                shape.IncludeJsonFormat = false;
                adapted = true;
            }

            if (!shape.UseResponsesApi)
            {
                shape.UseResponsesApi = true;
                shape.UseCompletionTokens = true;
                shape.IncludeTemperature = false;
                adapted = true;
            }

            return adapted;
        }

        if (!shape.UseResponsesApi && (text.Contains("v1/chat/completions", StringComparison.Ordinal) || text.Contains("/chat/completions", StringComparison.Ordinal))
            && (text.Contains("not found", StringComparison.Ordinal) || text.Contains("does not exist", StringComparison.Ordinal) || text.Contains("unsupported", StringComparison.Ordinal)))
        {
            shape.UseResponsesApi = true;
            return true;
        }

        if (text.Contains("max_tokens", StringComparison.Ordinal) && !shape.UseCompletionTokens)
        {
            shape.UseCompletionTokens = true;
            return true;
        }

        if (text.Contains("max_completion_tokens", StringComparison.Ordinal) && shape.UseCompletionTokens && !shape.UseResponsesApi)
        {
            shape.UseCompletionTokens = false;
            return true;
        }

        if (text.Contains("temperature", StringComparison.Ordinal) && shape.IncludeTemperature)
        {
            shape.IncludeTemperature = false;
            return true;
        }

        if ((text.Contains("response_format", StringComparison.Ordinal) || text.Contains("json_object", StringComparison.Ordinal))
            && shape.IncludeJsonFormat)
        {
            shape.IncludeJsonFormat = false;
            return true;
        }

        if (text.Contains("system", StringComparison.Ordinal) && !shape.SystemAsUser)
        {
            shape.SystemAsUser = true;
            return true;
        }

        if (!shape.UseResponsesApi && (text.Contains("invalid", StringComparison.Ordinal) || text.Contains("unsupported", StringComparison.Ordinal)))
        {
            shape.UseResponsesApi = true;
            shape.UseCompletionTokens = true;
            shape.IncludeTemperature = false;
            return true;
        }

        return false;
    }

    private static MailCheckClassification ParseClassification(string content)
    {
        var json = ExtractJson(content);
        ClassifierPayload? parsed = null;
        try
        {
            parsed = JsonSerializer.Deserialize<ClassifierPayload>(json, JsonOptions);
        }
        catch (JsonException)
        {
        }

        return new MailCheckClassification(MailCheckLabelCatalog.Parse(parsed?.Label));
    }

    private static string ExtractJson(string content)
    {
        var trimmed = content.Trim();
        if (trimmed.StartsWith('{'))
        {
            return trimmed;
        }

        var match = Regex.Match(trimmed, "\\{[\\s\\S]*\\}");
        return match.Success ? match.Value : trimmed;
    }

    private static string ReadResponsesText(ResponsesPayload payload)
    {
        var parts = new List<string>();
        foreach (var item in payload.Output ?? [])
        {
            foreach (var block in item.Content ?? [])
            {
                if (!string.IsNullOrWhiteSpace(block.Text))
                {
                    parts.Add(block.Text);
                }
            }
        }

        return string.Join(string.Empty, parts);
    }

    private static bool IsChatModel(string id)
    {
        var name = id.ToLowerInvariant();
        if (name.Contains("embed", StringComparison.Ordinal)
            || name.Contains("whisper", StringComparison.Ordinal)
            || name.Contains("tts", StringComparison.Ordinal)
            || name.Contains("dall-e", StringComparison.Ordinal)
            || name.Contains("dall_e", StringComparison.Ordinal)
            || name.Contains("moderation", StringComparison.Ordinal)
            || name.Contains("realtime", StringComparison.Ordinal)
            || name.Contains("transcribe", StringComparison.Ordinal)
            || name.Contains("audio", StringComparison.Ordinal)
            || name.Contains("image", StringComparison.Ordinal)
            || name.Contains("sora", StringComparison.Ordinal)
            || name.Contains("computer-use", StringComparison.Ordinal))
        {
            return false;
        }

        return name.Contains("gpt", StringComparison.Ordinal)
            || name.StartsWith("o1", StringComparison.Ordinal)
            || name.StartsWith("o3", StringComparison.Ordinal)
            || name.StartsWith("o4", StringComparison.Ordinal)
            || name.StartsWith("chatgpt", StringComparison.Ordinal);
    }

    private static string ReadError(string payload, string fallback)
    {
        try
        {
            var error = JsonSerializer.Deserialize<OpenAiErrorEnvelope>(payload, JsonOptions);
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

    private sealed class RequestShape
    {
        public bool UseResponsesApi { get; set; }

        public bool UseCompletionTokens { get; set; }

        public bool IncludeTemperature { get; set; }

        public bool IncludeJsonFormat { get; set; }

        public bool SystemAsUser { get; set; }

        public int OutputTokens { get; set; }
    }

    private sealed class ModelList
    {
        public List<ModelRow>? Data { get; set; }
    }

    private sealed class ModelRow
    {
        public string? Id { get; set; }
    }

    private sealed class ChatResponse
    {
        public List<ChatChoice>? Choices { get; set; }
    }

    private sealed class ChatChoice
    {
        public ChatMessage? Message { get; set; }
    }

    private sealed class ChatMessage
    {
        public string? Content { get; set; }
    }

    private sealed class ResponsesPayload
    {
        public string? OutputText { get; set; }

        public List<ResponseItem>? Output { get; set; }
    }

    private sealed class ResponseItem
    {
        public List<ResponseContent>? Content { get; set; }
    }

    private sealed class ResponseContent
    {
        public string? Text { get; set; }
    }

    private sealed class ClassifierPayload
    {
        public string? Label { get; set; }
    }

    private sealed class OpenAiErrorEnvelope
    {
        public OpenAiErrorBody? Error { get; set; }
    }

    private sealed class OpenAiErrorBody
    {
        public string? Message { get; set; }
    }
}
