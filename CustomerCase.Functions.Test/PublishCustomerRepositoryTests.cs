using System.Net;
using Azure.Messaging.ServiceBus;
using CustomerCase.Functions.Models;
using CustomerCase.Functions.Publishers.Customer;
using CustomerCase.Functions.Serialization;
using Infrastructure.Models;
using Infrastructure.Repositories;
using Microsoft.Extensions.Logging;
using Moq;

namespace CustomerCase.Functions.Test;

public class PublishCustomerRepositoryTests
{
    private readonly Mock<IServiceBusRepository> _serviceBus = new();
    private readonly PublishCustomerRepository _repository;

    public PublishCustomerRepositoryTests()
    {
        var serializer = new Mock<IAppJsonSerializer>();
        serializer.Setup(s => s.Serialize(It.IsAny<MessageBody<CustomerPublisherModel>>())).Returns("{}");
        _repository = new PublishCustomerRepository(
            _serviceBus.Object, Mock.Of<ILogger<PublishCustomerRepository>>(), serializer.Object);
    }

    [Fact]
    public async Task Publish_CreatesExpectedEnvelopeAndChangedFields()
    {
        MessageBody<CustomerPublisherModel>? sent = null;
        _serviceBus.Setup(bus => bus.WriteToTopic(It.IsAny<MessageBody<CustomerPublisherModel>>()))
            .Callback<MessageBody<CustomerPublisherModel>>(body => sent = body)
            .Returns(Task.CompletedTask);
        var correlationId = Guid.NewGuid();
        var customer = new CustomerPublisherModel
        {
            CustomerId = "customer-1",
            FirstName = "Anna",
            Status = "Active"
        };

        var result = await _repository.PublishCustomerAsync(customer, EventType.Update, correlationId);

        Assert.Equal(HttpStatusCode.Accepted, result.StatusCode);
        Assert.Equal("customer-1", result.Subject);
        Assert.NotNull(sent);
        Assert.Equal(EventType.Update, sent.EventType);
        Assert.Equal("customer", sent.EntityName);
        Assert.Equal("website", sent.EntitySource);
        Assert.Equal("website--customer", sent.Topic);
        Assert.Equal(correlationId, sent.CorrelationId);
        Assert.Same(customer, sent.Payload.EntityData);
        Assert.Equal(new[] { nameof(CustomerPublisherModel.FirstName), nameof(CustomerPublisherModel.Status) },
            sent.Payload.ChangedFields);
        Assert.InRange(sent.Timestamp, DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow);
        _serviceBus.Verify(bus => bus.WriteToTopic(It.IsAny<MessageBody<CustomerPublisherModel>>()), Times.Once);
    }

    [Fact]
    public void GetChangedFields_OnlyCustomerId_ReturnsEmpty()
    {
        var changed = PublishCustomerRepository.GetChangedFields(
            new CustomerPublisherModel { CustomerId = "customer-1" });

        Assert.Empty(changed);
    }

    [Fact]
    public void GetChangedFields_AllFieldsSet_ReturnsAllExceptCustomerIdInDeclarationOrder()
    {
        var changed = PublishCustomerRepository.GetChangedFields(new CustomerPublisherModel
        {
            CustomerId = "customer-1",
            FirstName = "Anna",
            LastName = "Jensen",
            Email = "anna@example.com",
            Phone = "+45 12345678",
            Street = "Norrebrogade 42",
            City = "Copenhagen",
            ZipCode = "2200",
            Country = "Denmark",
            Status = "Active"
        });

        Assert.Equal(new[]
        {
            nameof(CustomerPublisherModel.FirstName),
            nameof(CustomerPublisherModel.LastName),
            nameof(CustomerPublisherModel.Email),
            nameof(CustomerPublisherModel.Phone),
            nameof(CustomerPublisherModel.Street),
            nameof(CustomerPublisherModel.City),
            nameof(CustomerPublisherModel.ZipCode),
            nameof(CustomerPublisherModel.Country),
            nameof(CustomerPublisherModel.Status)
        }, changed);
    }

    [Fact]
    public void GetChangedFields_SkipsNullFields()
    {
        var changed = PublishCustomerRepository.GetChangedFields(new CustomerPublisherModel
        {
            CustomerId = "customer-1",
            Email = "anna@example.com",
            City = "Copenhagen"
        });

        Assert.Equal(new[] { nameof(CustomerPublisherModel.Email), nameof(CustomerPublisherModel.City) }, changed);
    }

    [Fact]
    public void GetChangedFields_EmptyStringCountsAsChanged()
    {
        var changed = PublishCustomerRepository.GetChangedFields(
            new CustomerPublisherModel { CustomerId = "customer-1", Phone = "" });

        Assert.Equal(new[] { nameof(CustomerPublisherModel.Phone) }, changed);
    }

    [Fact]
    public async Task ServiceBusFailure_ReturnsServiceUnavailable()
    {
        _serviceBus.Setup(bus => bus.WriteToTopic(It.IsAny<MessageBody<CustomerPublisherModel>>()))
            .ThrowsAsync(new ServiceBusException(true, "unavailable",
                reason: ServiceBusFailureReason.GeneralError));

        var result = await _repository.PublishCustomerAsync(
            new CustomerPublisherModel { CustomerId = "customer-1" }, EventType.Create, Guid.NewGuid());

        Assert.Equal(HttpStatusCode.ServiceUnavailable, result.StatusCode);
        Assert.Equal("customer-1", result.Subject);
        Assert.Equal(ServiceBusFailureReason.GeneralError.ToString(), result.AdditionalDescription);
    }
}
