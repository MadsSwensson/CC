using System.Net;

namespace Infrastructure.Models;

public class FunctionResponseModel
{
    public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.OK;
    public string Subject { get; set; } = string.Empty;
    public string? AdditionalDescription { get; set; }
}
