namespace CustomerCase.Functions.Models;

/// <summary>
/// Message payload for customer create and update operations.
/// For create: all fields should be populated.
/// For update: only non-null fields will be applied to the existing customer account.
/// </summary>
public class CustomerPublisherModel
{
    public string CustomerId { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Street { get; set; }
    public string? City { get; set; }
    public string? ZipCode { get; set; }
    public string? Country { get; set; }
    public string? Status { get; set; }
}
