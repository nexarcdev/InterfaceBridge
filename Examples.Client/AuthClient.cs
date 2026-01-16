using Examples.Shared;
using NexArc.InterfaceBridge;

namespace Examples.Client;

[Bridge<IAuthTest>(JsonSerializerContext = typeof(AuthJsonContext))]
public partial class AuthClient(HttpClient httpClient)
{
    private HttpClient HttpClient { get; } = httpClient;
}
