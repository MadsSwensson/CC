using Microsoft.Extensions.Options;

namespace CustomerCase.Functions.Serialization;

public interface IAppJsonSerializer
{
    string Serialize<T>(T value);

    ValueTask<T?> DeserializeAsync<T>(Stream stream, CancellationToken cancellationToken = default);
}

public sealed class AppJsonSerializer(IOptions<JsonSerializerOptions> options)
    : IAppJsonSerializer
{
    public string Serialize<T>(T value) =>
        JsonSerializer.Serialize(value, options.Value);

    public ValueTask<T?> DeserializeAsync<T>(
        Stream stream,
        CancellationToken cancellationToken = default) =>
        JsonSerializer.DeserializeAsync<T>(
            stream, options.Value, cancellationToken);
}