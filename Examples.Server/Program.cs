using System.Text.Json;
using Examples.Server;
using Examples.Shared;
using NexArc.InterfaceBridge.Server;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Register the InterfaceBridge for IHelloApi and its implementation
builder.Services.AddInterfaceBridge<IHelloApi, HelloApi>(jsonSerializerOptions: JsonSerializerOptions.Web);
builder.Services.AddInterfaceBridge<ITestApi, TestApi>(jsonSerializerOptions: TestJsonContext.Default.Options);

var app = builder.Build();

app.MapDefaultEndpoints();

// Map routes based on interface methods
app.MapInterfaceBridges();

app.MapGet("/", () => "Interface bridge is ready");

await app.RunAsync();