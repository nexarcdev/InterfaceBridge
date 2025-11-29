using Examples.Shared;
using NexArc.InterfaceBridge;

namespace Examples.Client;

[Bridge(typeof(IHelloApi))]
public partial class HelloClient
{
    public HttpClient HttpClient { get; }

    public HelloClient(HttpClient httpClient)
    {
        HttpClient = httpClient;
    }
}
