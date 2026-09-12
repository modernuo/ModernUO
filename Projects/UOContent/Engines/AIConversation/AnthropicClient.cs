using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Server.Text;

namespace Server.Engines.AIConversation;

public enum ChatRole
{
    User,
    Assistant
}

public record struct ChatTurn(ChatRole Role, string Text);

public class AnthropicResult
{
    public bool Success { get; init; }
    public string Text { get; init; }
    public string Error { get; init; }
    public string StopReason { get; init; }
    public long InputTokens { get; init; }
    public long OutputTokens { get; init; }
}

public class AnthropicRequest
{
    public string Model { get; init; }
    public int MaxTokens { get; init; }
    public string SystemPrompt { get; init; }
    public IReadOnlyList<ChatTurn> Messages { get; init; }
}

/// <summary>
/// Async client for the Anthropic Messages API (POST /v1/messages).
/// CompleteAsync never throws and never touches game state, so it is safe
/// to await from the game thread — the continuation is marshaled back
/// through the EventLoopContext.
/// </summary>
public class AnthropicClient
{
    // 429, 5xx and timeouts are worth a single retry after a short pause.
    private static readonly TimeSpan _retryDelay = TimeSpan.FromMilliseconds(1500);

    private readonly HttpClient _httpClient;
    private readonly string _apiUrl;
    private readonly string _apiKey;
    private readonly TimeSpan _timeout;

    public AnthropicClient(string apiUrl, string apiKey, TimeSpan timeout)
    {
        _apiUrl = apiUrl;
        _apiKey = apiKey;
        _timeout = timeout;
        _httpClient = new HttpClient();
    }

    public async Task<AnthropicResult> CompleteAsync(AnthropicRequest request)
    {
        var payload = BuildPayload(request);
        var (result, retryable) = await SendAsync(payload).ConfigureAwait(false);

        if (!result.Success && retryable)
        {
            await Task.Delay(_retryDelay).ConfigureAwait(false);
            (result, _) = await SendAsync(payload).ConfigureAwait(false);
        }

        return result;
    }

    private async Task<(AnthropicResult Result, bool Retryable)> SendAsync(string payload)
    {
        try
        {
            using var message = new HttpRequestMessage(HttpMethod.Post, _apiUrl);
            message.Headers.Add("x-api-key", _apiKey);
            message.Headers.Add("anthropic-version", "2023-06-01");
            message.Content = new StringContent(payload, Encoding.UTF8, "application/json");

            using var cts = new CancellationTokenSource(_timeout);
            using var response = await _httpClient.SendAsync(message, cts.Token).ConfigureAwait(false);

            var body = await response.Content.ReadAsStringAsync(cts.Token).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var status = (int)response.StatusCode;
                var error = $"HTTP {status} - {ExtractErrorMessage(body)}";
                return (new AnthropicResult { Success = false, Error = error }, status == 429 || status >= 500);
            }

            return (ParseResponse(body), false);
        }
        catch (OperationCanceledException)
        {
            return (new AnthropicResult { Success = false, Error = "Request timed out" }, true);
        }
        catch (Exception ex)
        {
            return (new AnthropicResult { Success = false, Error = ex.Message }, false);
        }
    }

    public static string BuildPayload(AnthropicRequest request)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteString("model", request.Model);
            writer.WriteNumber("max_tokens", request.MaxTokens);

            if (!string.IsNullOrEmpty(request.SystemPrompt))
            {
                writer.WriteString("system", request.SystemPrompt);
            }

            writer.WriteStartArray("messages");

            foreach (var turn in request.Messages)
            {
                writer.WriteStartObject();
                writer.WriteString("role", turn.Role == ChatRole.User ? "user" : "assistant");
                writer.WriteString("content", turn.Text);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    public static AnthropicResult ParseResponse(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("type", out var type) && type.ValueEquals("error"))
            {
                return new AnthropicResult { Success = false, Error = ExtractErrorMessage(root) };
            }

            // May run on a thread-pool continuation, so use the multi-threaded pool.
            using var text = ValueStringBuilder.CreateMT();

            if (root.TryGetProperty("content", out var content) && content.ValueKind == JsonValueKind.Array)
            {
                foreach (var block in content.EnumerateArray())
                {
                    if (block.TryGetProperty("type", out var blockType) && blockType.ValueEquals("text") &&
                        block.TryGetProperty("text", out var blockText))
                    {
                        text.Append(blockText.GetString());
                    }
                }
            }

            string stopReason = null;

            if (root.TryGetProperty("stop_reason", out var stop) && stop.ValueKind == JsonValueKind.String)
            {
                stopReason = stop.GetString();
            }

            long inputTokens = 0, outputTokens = 0;

            if (root.TryGetProperty("usage", out var usage))
            {
                if (usage.TryGetProperty("input_tokens", out var input) && input.TryGetInt64(out var inputValue))
                {
                    inputTokens = inputValue;
                }

                if (usage.TryGetProperty("output_tokens", out var output) && output.TryGetInt64(out var outputValue))
                {
                    outputTokens = outputValue;
                }
            }

            if (text.Length == 0)
            {
                return new AnthropicResult
                {
                    Success = false,
                    Error = $"Empty response (stop_reason: {stopReason ?? "unknown"})",
                    StopReason = stopReason
                };
            }

            return new AnthropicResult
            {
                Success = true,
                Text = text.ToString(),
                StopReason = stopReason,
                InputTokens = inputTokens,
                OutputTokens = outputTokens
            };
        }
        catch (JsonException)
        {
            return new AnthropicResult { Success = false, Error = "Unexpected response shape" };
        }
    }

    private static string ExtractErrorMessage(string body)
    {
        if (string.IsNullOrEmpty(body))
        {
            return "Unknown API error";
        }

        try
        {
            using var doc = JsonDocument.Parse(body);
            return ExtractErrorMessage(doc.RootElement);
        }
        catch (JsonException)
        {
            return "Unknown API error";
        }
    }

    private static string ExtractErrorMessage(JsonElement root)
    {
        if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("error", out var error) &&
            error.ValueKind == JsonValueKind.Object && error.TryGetProperty("message", out var message))
        {
            var errorType = error.TryGetProperty("type", out var t) && t.ValueKind == JsonValueKind.String
                ? t.GetString()
                : "error";

            return $"{errorType}: {message.GetString()}";
        }

        return "Unknown API error";
    }
}
