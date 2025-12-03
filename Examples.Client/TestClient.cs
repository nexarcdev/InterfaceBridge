using Examples.Shared;
using NexArc.InterfaceBridge;

namespace Examples.Client;

[Bridge<ITestApi>(JsonSerializerContext = typeof(TestJsonContext))]
public partial class TestClient(HttpClient httpClient)
{
    private HttpClient HttpClient { get; } = httpClient;
}
