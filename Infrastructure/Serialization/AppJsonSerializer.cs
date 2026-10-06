using System.Text.Json;
using System.Text.Json.Serialization;

namespace Infrastructure.Serialization;

public static class SharedJsonSerializer
{
    public static string Serialize<T>(T value) =>
        JsonSerializer.Serialize(value, Options);

    public static ValueTask<T?> DeserializeAsync<T>(
        Stream stream,
        CancellationToken cancellationToken = default) =>
        JsonSerializer.DeserializeAsync<T>(
            stream, Options, cancellationToken);
    
    public static T? Deserialize<T>(BinaryData data) =>
        JsonSerializer.Deserialize<T>(data,Options);

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        Converters = { new JsonStringEnumConverter() }
    };
}