using System.Net;
using CustomerCase.Functions.Data;
using CustomerCase.Functions.Models;
using CustomerCase.Functions.Subscribers.Customer;
using Microsoft.Extensions.Logging;
using Moq;

namespace CustomerCase.Functions.Test;

public class ProcessCustomerRepositoryTests
{
    private readonly Mock<ICustomerDatabase> _database = new();
    private readonly ProcessCustomerRepository _repository;

    public ProcessCustomerRepositoryTests()
    {
        _repository = new ProcessCustomerRepository(_database.Object, Mock.Of<ILogger<ProcessCustomerRepository>>());
    }

    [Fact]
    public async Task Create_PersistsCustomerWithDefaultsAndTimestamps()
    {
        CustomerModel? saved = null;
        _database.Setup(db => db.AddAsync(It.IsAny<CustomerModel>()))
            .Callback<CustomerModel>(customer => saved = customer)
            .Returns(Task.CompletedTask);

        var result = await _repository.CreateCustomerAsync(new CustomerPublisherModel
        {
            CustomerId = "customer-1",
            FirstName = "Anna"
        });

        Assert.Equal(HttpStatusCode.Created, result.StatusCode);
        Assert.NotNull(saved);
        Assert.Equal("customer-1", saved.CustomerId);
        Assert.Equal("Anna", saved.FirstName);
        Assert.Equal("Active", saved.Status);
        Assert.Equal(saved.CreatedAt, saved.UpdatedAt);
        Assert.True(saved.CreatedAt > DateTime.UtcNow.AddMinutes(-1));
    }

    [Fact]
    public async Task Create_ExistingCustomerDoesNotOverwrite()
    {
        _database.Setup(db => db.GetByIdAsync("customer-1"))
            .ReturnsAsync(new CustomerModel { CustomerId = "customer-1" });

        var result = await _repository.CreateCustomerAsync(new CustomerPublisherModel { CustomerId = "customer-1" });

        Assert.Equal(HttpStatusCode.Conflict, result.StatusCode);
        _database.Verify(db => db.AddAsync(It.IsAny<CustomerModel>()), Times.Never);
    }

    [Fact]
    public async Task Update_OnlyAppliesProvidedFieldsAndPreservesCreatedAt()
    {
        var createdAt = DateTime.UtcNow.AddDays(-1);
        var existing = new CustomerModel
        {
            CustomerId = "customer-1",
            FirstName = "Anna",
            LastName = "Jensen",
            CreatedAt = createdAt,
            UpdatedAt = createdAt
        };
        _database.Setup(db => db.GetByIdAsync("customer-1")).ReturnsAsync(existing);

        var result = await _repository.UpdateCustomerAsync(new CustomerPublisherModel
        {
            CustomerId = "customer-1",
            FirstName = "Anne"
        });

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Equal("Anne", existing.FirstName);
        Assert.Equal("Jensen", existing.LastName);
        Assert.Equal(createdAt, existing.CreatedAt);
        Assert.True(existing.UpdatedAt > createdAt);
        _database.Verify(db => db.UpdateAsync(existing), Times.Once);
    }

    [Fact]
    public async Task Update_UnknownCustomerDoesNotCreate()
    {
        var result = await _repository.UpdateCustomerAsync(new CustomerPublisherModel { CustomerId = "missing" });

        Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
        _database.Verify(db => db.UpdateAsync(It.IsAny<CustomerModel>()), Times.Never);
    }
}
