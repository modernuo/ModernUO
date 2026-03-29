using System.Text.Json.Serialization;

namespace BuildTool.Json;

public sealed class GlobalJson
{
    [JsonPropertyName("sdk")]
    public SdkConfig? Sdk { get; set; }
}

public sealed class SdkConfig
{
    [JsonPropertyName("version")]
    public string? Version { get; set; }

    [JsonPropertyName("rollForward")]
    public string? RollForward { get; set; }

    [JsonPropertyName("allowPrerelease")]
    public bool AllowPrerelease { get; set; }
}

[JsonSerializable(typeof(GlobalJson))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
internal partial class GlobalJsonContext : JsonSerializerContext;
