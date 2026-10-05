namespace CustomerCase.Functions.Data;

/// <summary>
/// A simple file-backed customer database using a local JSON file.
/// This is the default ("dummy") implementation provided with the project.
/// Candidates may replace this with their own ICustomerDatabase implementation.
/// </summary>
public class JsonFileCustomerDatabase : ICustomerDatabase
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private readonly string _filePath;

    public JsonFileCustomerDatabase()
    {
        _filePath = Path.Combine(AppContext.BaseDirectory, "Data", "customers.json");
    }

    public JsonFileCustomerDatabase(string filePath)
    {
        _filePath = filePath;
    }

    public async Task<List<CustomerModel>> GetAllAsync()
    {
        if (!File.Exists(_filePath))
            return [];

        var json = await File.ReadAllTextAsync(_filePath);
        return JsonSerializer.Deserialize<List<CustomerModel>>(json, JsonOptions) ?? [];
    }

    public async Task<CustomerModel?> GetByIdAsync(string customerId)
    {
        var customers = await GetAllAsync();
        return customers.FirstOrDefault(c =>
            string.Equals(c.CustomerId, customerId, StringComparison.OrdinalIgnoreCase));
    }

    public async Task AddAsync(CustomerModel customer)
    {
        var customers = await GetAllAsync();
        customers.Add(customer);
        var json = JsonSerializer.Serialize(customers, JsonOptions);
        await File.WriteAllTextAsync(_filePath, json);
    }

    public async Task UpdateAsync(CustomerModel customer)
    {
        var customers = await GetAllAsync();
        var index = customers.FindIndex(c =>
            string.Equals(c.CustomerId, customer.CustomerId, StringComparison.OrdinalIgnoreCase));

        if (index >= 0)
            customers[index] = customer;
        else
            customers.Add(customer);

        var updatedJson = JsonSerializer.Serialize(customers, JsonOptions);
        await File.WriteAllTextAsync(_filePath, updatedJson);
    }
}
