using System.Text.Json.Serialization;

namespace CustomerCase.Functions.Serialization;

public interface IAppJsonSerializer
{
    string Serialize<T>(T value);
    ValueTask<T?> DeserializeAsync<T>(Stream stream, CancellationToken cancellationToken = default);
    public T? Deserialize<T>(BinaryData data);
}

public sealed class AppJsonSerializer : IAppJsonSerializer
{
    public string Serialize<T>(T value) =>
        JsonSerializer.Serialize(value, _options);

    public ValueTask<T?> DeserializeAsync<T>(
        Stream stream,
        CancellationToken cancellationToken = default) =>
        JsonSerializer.DeserializeAsync<T>(
            stream, _options, cancellationToken);
    
    public T? Deserialize<T>(BinaryData data) =>
        JsonSerializer.Deserialize<T>(data,_options);

    private readonly JsonSerializerOptions _options = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        Converters = { new JsonStringEnumConverter() }
    };
}