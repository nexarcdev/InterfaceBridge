using Examples.Client;
using Examples.Shared;

// Ensure the server from Examples.Server is running on http://localhost:5199

var http = new HttpClient
{
    BaseAddress = new Uri("http://localhost:5199/")
};

var client = new HelloClient(http);

var result = await client.Greet("World", new GreetingRequest() { Location = "Earth" });

Console.WriteLine(result);
