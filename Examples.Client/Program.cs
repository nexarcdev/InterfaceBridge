using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Text;
using HttpMethod = System.Net.Http.HttpMethod;
using Examples.Client;
using Examples.Shared;
using Microsoft.AspNetCore.Mvc;
using NexArc.InterfaceBridge;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddHttpClient<HelloClient>(client => client.BaseAddress = new Uri("https+http://server"));
builder.Services.AddHttpClient<TestClient>(client => client.BaseAddress = new Uri("https+http://server"));

var handler = new SocketsHttpHandler
{
    UseCookies = true,
    CookieContainer = new CookieContainer(),
    AllowAutoRedirect = false
};

builder.Services.AddHttpClient<AuthClient>(client => client.BaseAddress = new Uri("https+http://server"))
    .ConfigurePrimaryHttpMessageHandler(() => handler);

var app = builder.Build();

var guid = Guid.Parse("a0000000-b000-c000-d000-e00000000000");

app.MapDefaultEndpoints();

app.MapGet("/", async ([FromServices]HelloClient client) => 
    await client.Greet("World", new GreetingRequest() { Location = "Earth" }));

app.MapGet("/test/1", async ([FromServices]TestClient client) =>
{
    var request = new TestRequest("Bob", 42, new PocoTest("Bobbie", 20));
    return await client.Get(guid, TestEnum.B, request);
});

app.MapGet("/test/2", async ([FromServices]TestClient client) =>
{
    var request = new TestRequest("Bob", 42, new PocoTest("Bobbie", 20));
    return await client.Get([request]);
});

app.MapGet("/test/3", async ([FromServices]TestClient client) =>
{
    var request = new TestRequest("Bob", 42, new PocoTest("Bobbie", 20));
    return await client.Post(guid, TestEnum.B, request);
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
    
    return await client.Put(guid, file);
});

app.MapGet("/test/5", async ([FromServices]TestClient client) =>
{
    var response = await client.StringTest("Hello, World!");
    Debug.Assert(response == "World says hello!");
    return Results.Text($"Response: {response}");
});

app.MapGet("/test/6", async ([FromServices]TestClient client) =>
{
    var file = new FilePart()
    {
        Content = new MemoryStream(),
        ContentType = "image/png",
        FileName = "nothing.png",
        Length = 0
    };
    
    return await client.Put(guid, file);
});

app.MapGet("/test/7", async ([FromServices]TestClient client) =>
{
    var file = await client.Download();
    return Results.File(file.Content, file.ContentType, file.FileName);
});

app.MapGet("/test/stream", async (HttpContext httpContext, [FromServices]TestClient client) =>
{
    var request = new TestRequest("Streamed Bob", 42, new PocoTest("Streamed Bobbie", 20));
    var log = new StringBuilder();

    httpContext.Response.Headers.ContentType = "text/plain; charset=utf-8";
    await using var writer = new StreamWriter(httpContext.Response.Body, Encoding.UTF8);
    
    await foreach (var item in client.Stream(5, request, 250))
    {
        await writer.WriteLineAsync($"{item.TestId}: {item.FullName} ({item.Age})");
        await writer.FlushAsync();
    }
});

app.MapGet("/test/auth", async ([FromServices]AuthClient client) =>
{
    var log = new StringBuilder();
    
    async Task Test(string actionName, Func<Task> action)
    {
        log.Append($"Testing {actionName}...");
        try
        {
            await action();
            log.AppendLine($"Success");
        }
        catch (HttpRequestException ex)
        {
            log.AppendLine($"Failure: {ex.StatusCode}");
        }
    }

    log.AppendLine("Anonymous access:");
    await Test("  Anonymous", () => client.AuthorizeAnonymous());
    await Test("  Authorize", () => client.Authorize());
    await Test("  Admin", () => client.AuthorizeAdmin());

    log.AppendLine().AppendLine("User access:");
    await Test("  Sign-In", () => client.SignIn("User", "User"));
    await Test("  Anonymous", () => client.AuthorizeAnonymous());
    await Test("  Authorize", () => client.Authorize());
    await Test("  Admin", () => client.AuthorizeAdmin());
    await Test("  Sign-Out", () => client.SignOut());

    log.AppendLine().AppendLine("Admin access:");
    await Test("  Sign-In", () => client.SignIn("Admin", "Admin"));
    await Test("  Anonymous", () => client.AuthorizeAnonymous());
    await Test("  Authorize", () => client.Authorize());
    await Test("  Admin", () => client.AuthorizeAdmin());
    await Test("  Sign-Out", () => client.SignOut());

    return Results.Text(log.ToString());
});

await app.RunAsync();
