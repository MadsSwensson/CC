using CustomerCase.Functions.Data;
using CustomerCase.Functions.Models;
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

    public ProcessCustomerRepository(
        ICustomerDatabase customerDatabase,
        ILogger<ProcessCustomerRepository> logger)
    {
        _customerDatabase = customerDatabase;
        _logger = logger;
    }

    public async Task<FunctionResponseModel> CreateCustomerAsync(CustomerPublisherModel data)
    {
        throw new NotImplementedException();
    }

    public async Task<FunctionResponseModel> UpdateCustomerAsync(CustomerPublisherModel data)
    {
        throw new NotImplementedException();
    }
}
