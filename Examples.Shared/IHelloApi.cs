using System.Threading;
using System.Threading.Tasks;
using NexArc.InterfaceBridge;

namespace Examples.Shared;

public class GreetingRequest
{
    public string? Location { get; set; }
}

public class GreetingResponse
{
    public string? Name { get; set; }
    public string? Location { get; set; }
    public string? Greeting { get; set; }
}

[RestConnector("api/hello")]
public interface IHelloApi
{
    [Rest(HttpMethod.Get, "greet/{name}")]
    Task<GreetingResponse> Greet(string name, GreetingRequest request, CancellationToken cancellationToken = default);
}
