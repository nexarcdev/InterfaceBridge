using Examples.Shared;
using NexArc.InterfaceBridge.Server;

var builder = WebApplication.CreateBuilder(args);

// Register the InterfaceBridge for IHelloApi and its implementation
builder.UseInterfaceBridge<IHelloApi, HelloApi>();

var app = builder.Build();

// Map routes based on interface methods
app.UseInterfaceBridges();

await app.RunAsync("http://localhost:5199");

public class HelloApi : IHelloApi
{
    public Task<GreetingResponse> Greet(string name, GreetingRequest request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(
            new GreetingResponse 
            {
                Name = name,
                Location = request.Location,
                Greeting = $"Hello, {name} from {request.Location}! Time: {DateTimeOffset.Now:O}",
            }
        );
    }
}
