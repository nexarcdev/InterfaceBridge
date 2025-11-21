using Examples.Shared;
using NexArc.InterfaceBridge.Server;

var builder = WebApplication.CreateBuilder(args);

// Register the InterfaceBridge for IHelloApi and its implementation
builder.UseInterfaceBridge<IHelloApi, HelloApi>();

var app = builder.Build();

// Map routes based on interface methods
app.UseInterfaceBridges();

app.Run("http://localhost:5199");

public class HelloApi : IHelloApi
{
    public Task<string> Greet(string name, CancellationToken cancellationToken = default)
    {
        return Task.FromResult($"Hello, {name}! Time: {DateTimeOffset.Now:O}");
    }
}
