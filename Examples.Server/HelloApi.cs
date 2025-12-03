using Examples.Shared;

namespace Examples.Server;

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