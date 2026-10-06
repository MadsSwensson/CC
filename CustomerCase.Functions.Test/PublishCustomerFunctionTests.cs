using System.Net;
using System.Text;
using CustomerCase.Functions.Models;
using CustomerCase.Functions.Publishers.Customer;
using Infrastructure.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Moq;

namespace CustomerCase.Functions.Test;

public class PublishCustomerFunctionTests
{
    private readonly Mock<IPublishCustomerRepository> _repository = new();
    private readonly PublishCustomerFunction _function;

    public PublishCustomerFunctionTests()
    {
        _function = new PublishCustomerFunction(_repository.Object, Mock.Of<ILogger<PublishCustomerFunction>>());
    }

    [Theory]
    [InlineData("Create", EventType.Create, HttpStatusCode.Accepted)]
    [InlineData("uPdAtE", EventType.Update, HttpStatusCode.OK)]
    [InlineData("create", EventType.Create, HttpStatusCode.ServiceUnavailable)]
    public async Task ValidAction_ReturnsRepositoryStatusAndPassesEventAndCorrelationId(
        string action, EventType eventType, HttpStatusCode repositoryStatus)
    {
        var (request, _) = Request("""{"customerId":"customer-1","firstName":"Anna"}""");
        Guid passedCorrelationId = Guid.Empty;
        _repository.Setup(r => r.PublishCustomerAsync(
                It.IsAny<CustomerPublisherModel>(), eventType, It.IsAny<Guid>()))
            .Callback<CustomerPublisherModel, EventType, Guid>((_, _, id) => passedCorrelationId = id)
            .ReturnsAsync(new FunctionResponseModel { StatusCode = repositoryStatus });

        var response = await _function.Run(request, action);

        Assert.Equal(repositoryStatus, response.StatusCode);
        Assert.NotEqual(Guid.Empty, passedCorrelationId);
        _repository.Verify(r => r.PublishCustomerAsync(
            It.Is<CustomerPublisherModel>(c => c.CustomerId == "customer-1" && c.FirstName == "Anna"),
            eventType, passedCorrelationId), Times.Once);
        _repository.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("unknown")]
    [InlineData("7")]
    [InlineData("Delete")]
    [InlineData("dElEtE")]
    public async Task UnsupportedAction_ReturnsBadRequestWithoutPublishing(string action)
    {
        var (request, _) = Request("""{"customerId":"customer-1"}""");

        var response = await _function.Run(request, action);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        _repository.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("""{"foo":"bar"}""")]
    [InlineData("""{"customerId":""}""")]
    [InlineData("""{"customerId":"  "}""")]
    [InlineData("null")]
    public async Task MissingCustomerId_ReturnsBadRequest(string json)
    {
        var (request, _) = Request(json);

        var response = await _function.Run(request, "create");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        _repository.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("{broken")]
    [InlineData("")]
    public async Task InvalidJson_ReturnsBadRequest(string json)
    {
        var (request, _) = Request(json);

        var response = await _function.Run(request, "create");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        _repository.VerifyNoOtherCalls();
    }

    private static (HttpRequestData Request, HttpResponseData Response) Request(string json)
    {
        var context = Mock.Of<FunctionContext>();
        var response = new Mock<HttpResponseData>(context);
        response.SetupProperty(r => r.StatusCode);
        var request = new Mock<HttpRequestData>(context);
        request.SetupGet(r => r.Body).Returns(new MemoryStream(Encoding.UTF8.GetBytes(json)));
        request.Setup(r => r.CreateResponse()).Returns(response.Object);
        return (request.Object, response.Object);
    }
}
