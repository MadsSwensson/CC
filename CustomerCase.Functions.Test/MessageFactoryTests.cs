using System.Text.Json;
using System.Text.Json.Nodes;
using CustomerCase.Functions.Models;
using Infrastructure.Factories;
using Infrastructure.Models;

namespace CustomerCase.Functions.Test;

public class MessageFactoryTests
{
    [Fact]
    public void CreateMessage_UsesAppSerializerAndSetsServiceBusProperties()
    {
        var messageBody = new MessageBody<CustomerPublisherModel>
        {
            EventType = EventType.Update,
            EntityName = "customer",
            EntitySource = "website",
            CorrelationId = Guid.Parse("9ac90177-48bc-4113-afdb-ee5506d921fd"),
            Payload = new Payload<CustomerPublisherModel>
            {
                EntityData = new CustomerPublisherModel
                {
                    CustomerId = "customer-1",
                    FirstName = "Anna"
                },
                ChangedFields = [nameof(CustomerPublisherModel.FirstName)]
            }
        };

        var message = MessageFactory.CreateMessage(messageBody);

        var expectedJsonFormatted = """
            {
              "eventType": "Update",
              "timestamp": "0001-01-01T00:00:00",
              "entityName": "customer",
              "entitySource": "website",
              "correlationId": "9ac90177-48bc-4113-afdb-ee5506d921fd",
              "payload": {
                "entityData": {
                  "customerId": "customer-1",
                  "firstName": "Anna",
                  "lastName": null,
                  "email": null,
                  "phone": null,
                  "street": null,
                  "city": null,
                  "zipCode": null,
                  "country": null,
                  "status": null
                },
                "changedFields": [ "FirstName" ]
              },
              "topic": "website--customer"
            }
            """;

        var expectedJson = JsonNode.Parse(expectedJsonFormatted)!.ToJsonString();
        var actualJson = message.Body.ToString();

        Assert.Equal(expectedJson, actualJson);
    }
}
