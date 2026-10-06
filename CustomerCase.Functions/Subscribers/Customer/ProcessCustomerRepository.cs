using CustomerCase.Functions.Data;
using Microsoft.Extensions.Logging;

namespace CustomerCase.Functions.Subscribers.Customer;

public interface IProcessCustomerRepository
{
    Task<FunctionResponseModel> CreateCustomerAsync(CustomerPublisherModel data);
    Task<FunctionResponseModel> UpdateCustomerAsync(CustomerPublisherModel data);
}

public class ProcessCustomerRepository : IProcessCustomerRepository
{
    private readonly ICustomerDatabase _customerDatabase;
    private readonly ILogger<ProcessCustomerRepository> _logger;

    public ProcessCustomerRepository(ICustomerDatabase customerDatabase, ILogger<ProcessCustomerRepository> logger)
    {
        _customerDatabase = customerDatabase;
        _logger = logger;
    }

    public async Task<FunctionResponseModel> CreateCustomerAsync(CustomerPublisherModel data)
    {
        if (await _customerDatabase.GetByIdAsync(data.CustomerId) is not null)
        {
            _logger.LogWarning("Customer {CustomerId} already exists", data.CustomerId);
            return new FunctionResponseModel { StatusCode = HttpStatusCode.Conflict, Subject = data.CustomerId };
        }

        var now = DateTime.UtcNow;
        await _customerDatabase.AddAsync(new CustomerModel
        {
            CustomerId = data.CustomerId,
            FirstName = data.FirstName ?? string.Empty,
            LastName = data.LastName ?? string.Empty,
            Email = data.Email ?? string.Empty,
            Phone = data.Phone ?? string.Empty,
            Street = data.Street ?? string.Empty,
            City = data.City ?? string.Empty,
            ZipCode = data.ZipCode ?? string.Empty,
            Country = data.Country ?? string.Empty,
            Status = data.Status ?? "Active",
            CreatedAt = now,
            UpdatedAt = now
        });
        
        _logger.LogInformation("Customer {CustomerId} was created", data.CustomerId);

        return new FunctionResponseModel { StatusCode = HttpStatusCode.Created, Subject = data.CustomerId };
    }

    public async Task<FunctionResponseModel> UpdateCustomerAsync(CustomerPublisherModel data)
    {
        var customer = await _customerDatabase.GetByIdAsync(data.CustomerId);
        if (customer is null)
        {
            _logger.LogWarning("Customer {CustomerId} does not exist", data.CustomerId);
            return new FunctionResponseModel { StatusCode = HttpStatusCode.NotFound, Subject = data.CustomerId };
        }

        customer.FirstName = data.FirstName ?? customer.FirstName;
        customer.LastName = data.LastName ?? customer.LastName;
        customer.Email = data.Email ?? customer.Email;
        customer.Phone = data.Phone ?? customer.Phone;
        customer.Street = data.Street ?? customer.Street;
        customer.City = data.City ?? customer.City;
        customer.ZipCode = data.ZipCode ?? customer.ZipCode;
        customer.Country = data.Country ?? customer.Country;
        customer.Status = data.Status ?? customer.Status;
        customer.UpdatedAt = DateTime.UtcNow;

        await _customerDatabase.UpdateAsync(customer);
                
        _logger.LogInformation("Customer {CustomerId} was updated", data.CustomerId);
        
        return new FunctionResponseModel { StatusCode = HttpStatusCode.OK, Subject = data.CustomerId };
    }
}
