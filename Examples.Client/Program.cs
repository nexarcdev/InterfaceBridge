using System.Text.Json;
using Examples.Client;
using Examples.Shared;
using Microsoft.AspNetCore.Mvc;
using NexArc.InterfaceBridge;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddHttpClient<HelloClient>(client => client.BaseAddress = new Uri("https+http://server"));
builder.Services.AddHttpClient<TestClient>(client => client.BaseAddress = new Uri("https+http://server"));

var app = builder.Build();

app.MapDefaultEndpoints();

app.MapGet("/", async ([FromServices]HelloClient client) => 
    await client.Greet("World", new GreetingRequest() { Location = "Earth" }));

app.MapGet("/test/1", async ([FromServices]TestClient client) =>
{
    var request = new TestRequest("Bob", 42, new PocoTest("Bobbie", 20));
    return await client.Get(Guid.NewGuid(), TestEnum.B, request);
});

app.MapGet("/test/2", async ([FromServices]TestClient client) =>
{
    var request = new TestRequest("Bob", 42, new PocoTest("Bobbie", 20));
    return await client.Get([request]);
});

app.MapGet("/test/3", async ([FromServices]TestClient client) =>
{
    var request = new TestRequest("Bob", 42, new PocoTest("Bobbie", 20));
    return await client.Post(Guid.NewGuid(), TestEnum.B, request);
});

app.MapGet("/test/4", async ([FromServices]TestClient client) =>
{
    var file = new FilePart()
    {
        Content = new MemoryStream(),
        ContentType = "image/png",
        FileName = "nothing.png",
        Length = 0
    };
    
    return await client.Put(Guid.NewGuid(), file);
});

await app.RunAsync();