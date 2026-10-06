using System.Net;
using Azure.Messaging.ServiceBus;
using CustomerCase.Functions.Models;
using CustomerCase.Functions.Serialization;
using CustomerCase.Functions.Subscribers.Customer;
using Infrastructure.Factories;
using Infrastructure.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Moq;

namespace CustomerCase.Functions.Test;

public class ProcessCustomerFunctionTests
{
    private readonly Mock<IProcessCustomerRepository> _repository = new();
    private readonly Mock<ServiceBusMessageActions> _actions = new();
    private readonly ProcessCustomerFunction _function;

    public ProcessCustomerFunctionTests()
    {
        _function = new ProcessCustomerFunction(
            _repository.Object,
            new AppJsonSerializer(),
            Mock.Of<ILogger<ProcessCustomerFunction>>());
    }

    [Fact]
    public async Task InvalidJson_IsDeadLetteredWithoutProcessing()
    {
        var message = Receive("{broken");

        await _function.Run(message, _actions.Object);

        AssertDeadLettered(message, "InvalidJson");
        _repository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task EmptyBody_IsDeadLetteredAsInvalidJson()
    {
        var message = Receive("");

        await _function.Run(message, _actions.Object);

        AssertDeadLettered(message, "InvalidJson");
        _repository.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("null")]
    [InlineData("""{"eventType":"Create","payload":null}""")]
    [InlineData("""{"eventType":"Create","payload":{"entityData":null}}""")]
    [InlineData("""{"eventType":"Create","payload":{"entityData":{}}}""")]
    [InlineData("""{"eventType":"Create","payload":{"entityData":{"customerId":"  "}}}""")]
    public async Task MissingCustomerData_IsDeadLetteredWithoutProcessing(string json)
    {
        var message = Receive(json);

        await _function.Run(message, _actions.Object);

        AssertDeadLettered(message, "InvalidCustomerData");
        _repository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UnsupportedEvent_IsDeadLetteredWithoutProcessing()
    {
        var message = CustomerMessage(EventType.Delete);

        await _function.Run(message, _actions.Object);

        AssertDeadLettered(message, "UnsupportedEventType");
        _repository.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(EventType.Create, HttpStatusCode.Created)]
    [InlineData(EventType.Update, HttpStatusCode.OK)]
    public async Task SuccessfulOperation_CompletesMessage(EventType eventType, HttpStatusCode status)
    {
        var message = CustomerMessage(eventType);
        _repository.Setup(r => r.CreateCustomerAsync(It.IsAny<CustomerPublisherModel>()))
            .ReturnsAsync(new FunctionResponseModel { StatusCode = status });
        _repository.Setup(r => r.UpdateCustomerAsync(It.IsAny<CustomerPublisherModel>()))
            .ReturnsAsync(new FunctionResponseModel { StatusCode = status });

        await _function.Run(message, _actions.Object);

        _actions.Verify(a => a.CompleteMessageAsync(message, default), Times.Once);
        _actions.VerifyNoOtherCalls();
        if (eventType == EventType.Create)
            _repository.Verify(r => r.CreateCustomerAsync(It.Is<CustomerPublisherModel>(c => c.CustomerId == "customer-1")), Times.Once);
        else
            _repository.Verify(r => r.UpdateCustomerAsync(It.Is<CustomerPublisherModel>(c => c.CustomerId == "customer-1")), Times.Once);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.Conflict)]
    public async Task RejectedOperation_IsDeadLettered(HttpStatusCode status)
    {
        var message = CustomerMessage(EventType.Create);
        _repository.Setup(r => r.CreateCustomerAsync(It.IsAny<CustomerPublisherModel>()))
            .ReturnsAsync(new FunctionResponseModel { StatusCode = status });

        await _function.Run(message, _actions.Object);

        AssertDeadLettered(message, $"CustomerRejected_{(int)status}");
    }

    [Fact]
    public async Task UpdateBeforeCreate_NotFound_IsAbandonedAndThrowsForRetry()
    {
        var message = CustomerMessage(EventType.Update);
        _repository.Setup(r => r.UpdateCustomerAsync(It.IsAny<CustomerPublisherModel>()))
            .ReturnsAsync(new FunctionResponseModel { StatusCode = HttpStatusCode.NotFound });

        await Assert.ThrowsAsync<InvalidOperationException>(() => _function.Run(message, _actions.Object));

        AssertAbandoned(message);
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task ServerError_IsAbandonedAndThrowsForRetry(HttpStatusCode status)
    {
        var message = CustomerMessage(EventType.Create);
        _repository.Setup(r => r.CreateCustomerAsync(It.IsAny<CustomerPublisherModel>()))
            .ReturnsAsync(new FunctionResponseModel { StatusCode = status });

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _function.Run(message, _actions.Object));

        Assert.Equal($"Customer Create failed with status {(int)status}.", ex.Message);
        AssertAbandoned(message);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public async Task DatabaseFailure_IsAbandonedAndThrowsForRetry(int deliveryCount)
    {
        var message = CustomerMessage(EventType.Update, deliveryCount);
        _repository.Setup(r => r.UpdateCustomerAsync(It.IsAny<CustomerPublisherModel>()))
            .ThrowsAsync(new IOException("database unavailable"));

        await Assert.ThrowsAsync<IOException>(() => _function.Run(message, _actions.Object));

        AssertAbandoned(message);
    }

    [Fact]
    public async Task AbandonFailure_StillThrowsOriginalError()
    {
        var message = CustomerMessage(EventType.Update, deliveryCount: 1);
        _repository.Setup(r => r.UpdateCustomerAsync(It.IsAny<CustomerPublisherModel>()))
            .ThrowsAsync(new IOException("database unavailable"));
        _actions.Setup(a => a.AbandonMessageAsync(message, null, default))
            .ThrowsAsync(new ServiceBusException("lock lost", ServiceBusFailureReason.MessageLockLost));

        var ex = await Assert.ThrowsAsync<IOException>(() => _function.Run(message, _actions.Object));

        Assert.Equal("database unavailable", ex.Message);
    }

    [Fact]
    public async Task Cancellation_IsNotSettled()
    {
        var message = CustomerMessage(EventType.Update, deliveryCount: 3);
        _repository.Setup(r => r.UpdateCustomerAsync(It.IsAny<CustomerPublisherModel>()))
            .ThrowsAsync(new OperationCanceledException());

        await Assert.ThrowsAsync<OperationCanceledException>(() => _function.Run(message, _actions.Object));

        _actions.VerifyNoOtherCalls();
    }

    private void AssertAbandoned(ServiceBusReceivedMessage message)
    {
        _actions.Verify(a => a.AbandonMessageAsync(message, null, default), Times.Once);
        _actions.VerifyNoOtherCalls();
    }

    private void AssertDeadLettered(ServiceBusReceivedMessage message, string reason)
    {
        _actions.Verify(a => a.DeadLetterMessageAsync(message, null, reason, null, default), Times.Once);
        _actions.VerifyNoOtherCalls();
    }

    private static ServiceBusReceivedMessage Receive(string json) =>
        ServiceBusModelFactory.ServiceBusReceivedMessage(body: BinaryData.FromString(json), correlationId: Guid.NewGuid().ToString());

    private static ServiceBusReceivedMessage CustomerMessage(EventType eventType, int deliveryCount = 1)
    {
        var outgoing = new MessageFactory().CreateMessage(new MessageBody<CustomerPublisherModel>
        {
            EventType = eventType,
            EntityName = "customer",
            EntitySource = "website",
            Payload = new Payload<CustomerPublisherModel>
            {
                EntityData = new CustomerPublisherModel { CustomerId = "customer-1" }
            }
        });
        return ServiceBusModelFactory.ServiceBusReceivedMessage(
            body: outgoing.Body, correlationId: outgoing.CorrelationId, deliveryCount: deliveryCount);
    }
}
