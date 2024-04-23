using System.Text.Json;

namespace AssistantsDotNet;

static class JsonHelpers
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public static string Serialize<T>(T value) => JsonSerializer.Serialize(value, JsonSerializerOptions);

    public static T Deserialize<T>(string json) => JsonSerializer.Deserialize<T>(json, JsonSerializerOptions)!;

    public static BinaryData FromObjectAsJson(object value) => BinaryData.FromObjectAsJson(value, JsonSerializerOptions);
}
