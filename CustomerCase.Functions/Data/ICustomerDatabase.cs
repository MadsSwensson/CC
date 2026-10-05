namespace CustomerCase.Functions.Data;

/// <summary>
/// Abstraction for customer data persistence.
/// The default implementation uses a local JSON file, but candidates are free to provide
/// their own implementation (e.g., SQL Server, Redis, in-memory, or any other store).
/// </summary>
public interface ICustomerDatabase
{
    Task<List<CustomerModel>> GetAllAsync();
    Task<CustomerModel?> GetByIdAsync(string customerId);
    Task AddAsync(CustomerModel customer);
    Task UpdateAsync(CustomerModel customer);
}
