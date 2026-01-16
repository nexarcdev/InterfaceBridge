[![Publish NuGet Packages](https://github.com/nexarcdev/InterfaceBridge/actions/workflows/nuget-publish.yml/badge.svg)](https://github.com/nexarcdev/InterfaceBridge/actions/workflows/nuget-publish.yml) [![NuGet Package](https://img.shields.io/nuget/v/NexArc.InterfaceBridge.svg)](https://www.nuget.org/packages/NexArc.InterfaceBridge)

# NexArc.InterfaceBridge

A type-safe, interface-based REST API framework for .NET that creates tightly coupled connections between client and server with full AOT (Ahead-of-Time) compilation support.

## Features

- **Type-Safe API Contracts**: Define your REST APIs using C# interfaces with attributes
- **AOT-Compatible**: Full support for iOS and other platforms requiring AOT compilation
- **Source Generation**: Automatic client code generation using Roslyn source generators
- **System.Text.Json Integration**: Built-in support for `JsonSerializable` partial classes
- **Minimal Boilerplate**: Write interfaces once, generate both client and server implementations
- **Multiple Content Types**: Supports form data, multipart/form-data, and file uploads
- **Async/Await**: Built-in support for `Task<T>` and `CancellationToken`

## Installation

Install the NuGet packages:

```bash
# For server-side implementation
dotnet add package NexArc.InterfaceBridge
dotnet add package NexArc.InterfaceBridge.Server

# For client-side code generation
dotnet add package NexArc.InterfaceBridge
dotnet add package NexArc.InterfaceBridge.CodeGenerator
```

## Quick Start

### 1. Define Your API Interface (Shared)

```csharp
using NexArc.InterfaceBridge;

[RestConnector("api/hello")]
public interface IHelloApi
{
    [Rest(HttpMethod.Get, "greet/{name}")]
    Task<string> Greet(string name, CancellationToken cancellationToken = default);
}
```

### 2. Implement the Server

```csharp
using NexArc.InterfaceBridge.Server;

var builder = WebApplication.CreateBuilder(args);

// Register the InterfaceBridge
builder.Services.AddInterfaceBridge<IHelloApi, HelloApi>();

var app = builder.Build();

// Map routes based on interface methods
app.MapInterfaceBridges();

app.Run("http://localhost:5199");

public class HelloApi : IHelloApi
{
    public Task<string> Greet(string name, CancellationToken cancellationToken = default)
    {
        return Task.FromResult($"Hello, {name}!");
    }
}
```

### 3. Generate and Use the Client

```csharp
using Examples.Client;
using NexArc.InterfaceBridge;

// Create a partial class with the Bridge attribute
[Bridge(typeof(IHelloApi))]
public partial class HelloClient : IHelloApi
{
    public HttpClient HttpClient { get; }

    public HelloClient(HttpClient httpClient)
    {
        HttpClient = httpClient;
    }
}

// Use the generated client
var http = new HttpClient
{
    BaseAddress = new Uri("http://localhost:5199/")
};

var client = new HelloClient(http);
var result = await client.Greet("World");
Console.WriteLine(result); // Output: Hello, World!
```

## AOT Support with System.Text.Json

For iOS and other AOT platforms, use must use `JsonSerializable` for code generation. 

```csharp
// CLIENT/SERVER SHARED 
using System.Text.Json.Serialization;

// Customize your serialization options (recommended)
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonConverter(typeof(JsonStringEnumConverter))]
[JsonSourceGenerationOptions(WriteIndented = true)] // Debugging only 
// List all of the return types and parameter types, arrays must be listed separately
[JsonSerializable(typeof(YourResponseType))]
[JsonSerializable(typeof(YourResponseType[]))]
[JsonSerializable(typeof(YourRequestType))]
public partial class AppJsonContext : JsonSerializerContext { }

// CLIENT SIDE
// Supply the JsonSerializerContext to the Bridge attribute
[Bridge(typeof(IYourApi), JsonSerializerContext = typeof(AppJsonContext))]
public partial class YourClient : IYourApi
{
    public HttpClient HttpClient { get; }

    public YourClient(HttpClient httpClient)
    {
        HttpClient = httpClient;
    }
}

// SERVER SIDE
// Supply the JsonSerializerOptions to the InterfaceBridge
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddInterfaceBridge<IYourApi, YourApi>(jsonSerializerOptions: AppJsonContext.Default.Options);

public class YourApi : IYourApi { /* ... */ }
```

## Attributes

### `[RestConnector]`
Applied to interfaces to define the base route prefix for all methods.

### `[Rest]`
Applied to interface methods to define HTTP method and route template. Supports:
- **GET**: Parameters from route template, remaining in query string
- **POST/PUT/PATCH**: Parameters from route template, remaining in body (form-encoded or multipart)
- **DELETE**: Parameters from route template

### `[Bridge]`
Applied to partial client classes to generate the implementation. Parameters:
- `bridgeInterface`: The interface to implement
- `JsonSerializerContext` (optional): For AOT support
- `DefaultBodyType` (optional): Override default body encoding

## Content Types

The framework automatically selects the appropriate content type:

- **FormUrlEncoded**: Default for POST/PUT/PATCH
- **MultipartFormData**: Automatically used when `FilePart` parameters are present
- **JSON**: Use with `JsonSerializerContext` for AOT support

## Advanced Features

### Authorization Attributes

Server endpoints created via InterfaceBridge can honor `[Authorize]` and `[AllowAnonymous]` on interface or implementation methods. Add standard ASP.NET Core authentication/authorization middleware, then apply attributes on your server implementation:

```csharp
using Microsoft.AspNetCore.Authorization;

public class AuthApi : IAuthApi
{
    [Authorize]
    public Task<string> Authorize() => Task.FromResult("Authorized");

    [Authorize(Roles = "Admin")]
    public Task<string> AdminOnly() => Task.FromResult("Admin Authorized");

    [AllowAnonymous]
    public Task<string> Anonymous() => Task.FromResult("Anonymous Authorized");
}
```

To access the current user in your server implementation, inject `IHttpContextAccessor`:

```csharp
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

public class AuthApi(IHttpContextAccessor httpContextAccessor) : IAuthApi
{
    public Task<string> WhoAmI()
    {
        var user = httpContextAccessor.HttpContext?.User;
        var name = user?.Identity?.Name ?? "anonymous";
        var roles = user?.FindAll(ClaimTypes.Role).Select(c => c.Value) ?? [];
        return Task.FromResult($"{name} ({string.Join(", ", roles)})");
    }
}
```

Ensure authentication/authorization is configured in your server startup:

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHttpContextAccessor();
builder.Services.AddAuthentication().AddCookie();
builder.Services.AddAuthorization();

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.MapInterfaceBridges();
```

### File Uploads

Use `FilePart` for file uploads (automatically switches to multipart/form-data):

```csharp
[Rest(HttpMethod.Post, "upload")]
Task<UploadResponse> UploadFile(string description, FilePart file, CancellationToken cancellationToken = default);
```

InterfaceBridge takes ownership of the FilePart.Stream and is responsible for closing it.
- On the client, the FilePart is disposed automatically when the request completes.
- On the server, the FilePart is disposed after the request is processed.

`FilePart.Create(Stream stream, string? fileName, string? contentType)` will take ownership of the provided stream.
`FilePart.CreateCopy(Stream stream, string? fileName, string? contentType)` exists to create a copy of a stream for upload using `stream.CopyTo()`.

NOTE: Some Blazor UI libraries require use of `FilePart.CreateCopy` when using `IBrowserFile`.
```csharp
    IBrowserFile uploadedImage;
    using var uploadedStream = uploadedImage.OpenReadStream();
    var filePart = await FilePart.CreateCopyAsync(uploadedStream);
```

### Custom Body Types

Override the default body type:

```csharp
[Rest(HttpMethod.Post, "data", BodyType = BodyType.MultipartFormData)]
Task PostData(string name, int value, CancellationToken cancellationToken = default);
```

## License

MIT

## Repository

https://github.com/nexarcdev/InterfaceBridge
