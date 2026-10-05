using System.Text.Json.Serialization;

namespace CustomerCase.Functions.Serialization;

public interface IAppJsonSerializer
{
    string Serialize<T>(T value);

    ValueTask<T?> DeserializeAsync<T>(Stream stream, CancellationToken cancellationToken = default);
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

    private readonly JsonSerializerOptions _options = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        Converters = { new JsonStringEnumConverter() }
    };
}