namespace Infrastructure.Models;

public class Payload<T>
{
    public T EntityData { get; set; } = default!;
    public List<string> ChangedFields { get; set; } = [];
}
