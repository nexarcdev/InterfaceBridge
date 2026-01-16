using System.Text.Json;
using Examples.Server;
using Examples.Shared;
using Microsoft.AspNetCore.Authentication.Cookies;
using NexArc.InterfaceBridge.Server;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddHttpContextAccessor();
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Events = new CookieAuthenticationEvents
        {
            OnRedirectToLogin = context =>
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            },
            OnRedirectToAccessDenied = context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            }
        };
    });
builder.Services.AddAuthorization();

// Register the InterfaceBridge for IHelloApi and its implementation
builder.Services.AddInterfaceBridge<IHelloApi, HelloApi>(jsonSerializerOptions: JsonSerializerOptions.Web);
builder.Services.AddInterfaceBridge<ITestApi, TestApi>(jsonSerializerOptions: TestJsonContext.Default.Options);
builder.Services.AddInterfaceBridge<IAuthTest, AuthTest>(jsonSerializerOptions: AuthJsonContext.Default.Options);

var app = builder.Build();

app.MapDefaultEndpoints();
app.UseAuthentication();
app.UseAuthorization();

// Map routes based on interface methods
app.MapInterfaceBridges();

app.MapGet("/", () => "Interface bridge is ready");

await app.RunAsync();
