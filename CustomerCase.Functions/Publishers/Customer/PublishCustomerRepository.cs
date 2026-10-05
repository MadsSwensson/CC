using CustomerCase.Functions.Models;
using Microsoft.Extensions.Logging;

namespace CustomerCase.Functions.Publishers.Customer;

public interface IPublishCustomerRepository
{
    Task<FunctionResponseModel> PublishCustomerAsync(CustomerPublisherModel message, EventType eventType, Guid correlationId);
}

public class PublishCustomerRepository : IPublishCustomerRepository
{
    private readonly IServiceBusRepository _serviceBusRepository;
    private readonly ILogger<PublishCustomerRepository> _logger;

    public PublishCustomerRepository(
        IServiceBusRepository serviceBusRepository,
        ILogger<PublishCustomerRepository> logger)
    {
        _serviceBusRepository = serviceBusRepository;
        _logger = logger;
    }

    public async Task<FunctionResponseModel> PublishCustomerAsync(
        CustomerPublisherModel message, 
        EventType eventType,
        Guid correlationId)
    {
        throw new NotImplementedException();
    }
}
