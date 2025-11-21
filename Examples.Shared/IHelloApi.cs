using System.Threading;
using System.Threading.Tasks;
using NexArc.InterfaceBridge;

namespace Examples.Shared;

[RestConnector("api/hello")]
public interface IHelloApi
{
    [Rest(HttpMethod.Get, "greet/{name}")]
    Task<string> Greet(string name, CancellationToken cancellationToken = default);
}
