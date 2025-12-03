using System.Text.Json;
using Examples.Shared;
using NexArc.InterfaceBridge;

namespace Examples.Client;

[Bridge<IHelloApi>]
public partial class HelloClient(HttpClient httpClient)
{
    private HttpClient HttpClient { get; } = httpClient;
    private JsonSerializerOptions JsonSerializerOptions { get; } = JsonSerializerOptions.Web;
}
