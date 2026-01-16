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
    
    return await client.Put(Guid.Empty, file);
});

app.MapGet("/test/7", async ([FromServices]TestClient client) =>
{
    var file = await client.Download();
    return Results.File(file.Content, file.ContentType, file.FileName);
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
