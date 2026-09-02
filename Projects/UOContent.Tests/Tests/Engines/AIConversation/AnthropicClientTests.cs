using System.Text.Json;
using Server.Engines.AIConversation;
using Xunit;

namespace Server.Tests.Engines.AIConversation;

public class AnthropicClientTests
{
    [Fact]
    public void ParseResponse_ExtractsTextStopReasonAndUsage()
    {
        const string json =
            """
            {
              "id": "msg_01",
              "type": "message",
              "role": "assistant",
              "content": [{ "type": "text", "text": "Well met, traveler." }],
              "stop_reason": "end_turn",
              "usage": { "input_tokens": 512, "output_tokens": 42 }
            }
            """;

        var result = AnthropicClient.ParseResponse(json);

        Assert.True(result.Success);
        Assert.Equal("Well met, traveler.", result.Text);
        Assert.Equal("end_turn", result.StopReason);
        Assert.Equal(512, result.InputTokens);
        Assert.Equal(42, result.OutputTokens);
    }

    [Fact]
    public void ParseResponse_ConcatenatesMultipleTextBlocks()
    {
        const string json =
            """
            {
              "content": [
                { "type": "text", "text": "First. " },
                { "type": "thinking", "thinking": "ignored" },
                { "type": "text", "text": "Second." }
              ],
              "stop_reason": "end_turn",
              "usage": { "input_tokens": 1, "output_tokens": 2 }
            }
            """;

        var result = AnthropicClient.ParseResponse(json);

        Assert.True(result.Success);
        Assert.Equal("First. Second.", result.Text);
    }

    [Fact]
    public void ParseResponse_ReturnsApiErrorDetails()
    {
        const string json =
            """
            {
              "type": "error",
              "error": { "type": "overloaded_error", "message": "Overloaded" }
            }
            """;

        var result = AnthropicClient.ParseResponse(json);

        Assert.False(result.Success);
        Assert.Equal("overloaded_error: Overloaded", result.Error);
    }

    [Fact]
    public void ParseResponse_EmptyContentIsFailure()
    {
        const string json =
            """
            { "content": [], "stop_reason": "max_tokens", "usage": { "input_tokens": 5, "output_tokens": 0 } }
            """;

        var result = AnthropicClient.ParseResponse(json);

        Assert.False(result.Success);
        Assert.Contains("max_tokens", result.Error);
    }

    [Fact]
    public void ParseResponse_MalformedJsonIsFailure()
    {
        var result = AnthropicClient.ParseResponse("not json at all");

        Assert.False(result.Success);
        Assert.Equal("Unexpected response shape", result.Error);
    }

    [Fact]
    public void BuildPayload_ProducesMessagesApiShape()
    {
        var request = new AnthropicRequest
        {
            Model = "claude-haiku-4-5",
            MaxTokens = 200,
            SystemPrompt = "You are a banker.",
            Messages = new[]
            {
                new ChatTurn(ChatRole.User, "hello"),
                new ChatTurn(ChatRole.Assistant, "well met"),
                new ChatTurn(ChatRole.User, "who are you?")
            }
        };

        using var doc = JsonDocument.Parse(AnthropicClient.BuildPayload(request));
        var root = doc.RootElement;

        Assert.Equal("claude-haiku-4-5", root.GetProperty("model").GetString());
        Assert.Equal(200, root.GetProperty("max_tokens").GetInt32());
        Assert.Equal("You are a banker.", root.GetProperty("system").GetString());

        var messages = root.GetProperty("messages");
        Assert.Equal(3, messages.GetArrayLength());
        Assert.Equal("user", messages[0].GetProperty("role").GetString());
        Assert.Equal("hello", messages[0].GetProperty("content").GetString());
        Assert.Equal("assistant", messages[1].GetProperty("role").GetString());
        Assert.Equal("user", messages[2].GetProperty("role").GetString());
    }

    [Fact]
    public void BuildPayload_OmitsEmptySystemPrompt()
    {
        var request = new AnthropicRequest
        {
            Model = "claude-haiku-4-5",
            MaxTokens = 200,
            Messages = new[] { new ChatTurn(ChatRole.User, "hello") }
        };

        using var doc = JsonDocument.Parse(AnthropicClient.BuildPayload(request));

        Assert.False(doc.RootElement.TryGetProperty("system", out _));
    }

    [Fact]
    public void BuildPayload_EscapesSpecialCharacters()
    {
        var request = new AnthropicRequest
        {
            Model = "m",
            MaxTokens = 1,
            Messages = new[] { new ChatTurn(ChatRole.User, "he said \"hi\"\nand left") }
        };

        using var doc = JsonDocument.Parse(AnthropicClient.BuildPayload(request));

        Assert.Equal(
            "he said \"hi\"\nand left",
            doc.RootElement.GetProperty("messages")[0].GetProperty("content").GetString()
        );
    }
}
