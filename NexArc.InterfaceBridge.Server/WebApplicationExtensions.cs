using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace NexArc.InterfaceBridge.Server;

public static class WebApplicationExtensions
{
    private record InterfaceBridgeDefinition(Type ManagerInterface, Type? ManagerImplementation,
        JsonSerializerOptions? JsonSerializerOptions);

    public static IServiceCollection AddInterfaceBridge<TManagerInterface, TManagerImplementation>(
        this IServiceCollection serviceCollection, JsonSerializerOptions? jsonSerializerOptions = null)
        where TManagerImplementation : class, TManagerInterface
        where TManagerInterface : class
    {
        
        serviceCollection.AddSingleton(new InterfaceBridgeDefinition(typeof(TManagerInterface),
            typeof(TManagerImplementation), jsonSerializerOptions));
        serviceCollection.AddScoped<TManagerInterface, TManagerImplementation>();

        return serviceCollection;
    }

    public static WebApplication MapInterfaceBridges(this WebApplication app)
    {
        var bridges = app.Services.GetServices<InterfaceBridgeDefinition>();

        var defaultJsonOptions = app.Services.GetService<IOptions<JsonOptions>>()?.Value.JsonSerializerOptions;

        foreach (var bridge in bridges)
        {
            var jsonSerializerOptions = bridge.JsonSerializerOptions ?? defaultJsonOptions ?? JsonSerializerOptions.Web;

            foreach (var method in bridge.ManagerInterface.GetMethods(BindingFlags.Instance | BindingFlags.Public))
            {
                RouteMapper.Map(app, bridge.ManagerInterface, method, jsonSerializerOptions, bridge.ManagerImplementation);
            }
        }

        return app;
    }

    public static WebApplication MapInterfaceBridge<TManagerInterface>(this WebApplication app,
        JsonSerializerOptions? jsonSerializerOptions = null)
        where TManagerInterface : class
    {
        if (jsonSerializerOptions == null)
        {
            var jsonOptions = app.Services.GetService<IOptions<JsonOptions>>();
            jsonSerializerOptions = jsonOptions?.Value.JsonSerializerOptions;
        }

        foreach (var method in typeof(TManagerInterface).GetMethods(BindingFlags.Instance | BindingFlags.Public))
        {
            RouteMapper.Map(app, typeof(TManagerInterface), method, jsonSerializerOptions ?? JsonSerializerOptions.Web);
        }

        return app;
    }
}
