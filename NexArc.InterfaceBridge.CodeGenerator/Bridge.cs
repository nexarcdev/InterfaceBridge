using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace NexArc.InterfaceBridge.CodeGenerator;

public class Bridge
{
    public const string RestConnectorAttributeName = "RestConnectorAttribute";

    public ClientDefinition ClientDefinition { get; }
    public string? JsonSerializerContext { get; }
    public INamedTypeSymbol BridgeType { get; }
    public Method[] Methods { get; }
    public string? RoutePrefix { get; }
    public bool HasJsonSerializerOptions { get; }

    public Bridge(ClientDefinition clientDefinition, AttributeData bridge)
    {
        ClientDefinition = clientDefinition;

        if (bridge.AttributeClass!.IsGenericType)
            BridgeType = (INamedTypeSymbol)bridge.AttributeClass.TypeArguments[0];
        else
            BridgeType = (INamedTypeSymbol)bridge.ConstructorArguments[0].Value!;
        
        var restConnectorData = BridgeType.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name == RestConnectorAttributeName);
        RoutePrefix = (string?)restConnectorData?.ConstructorArguments[0].Value;

        var jsonSerializerContext = bridge.NamedArguments.FirstOrDefault(a => a.Key == "JsonSerializerContext").Value;
        JsonSerializerContext = jsonSerializerContext.Kind == TypedConstantKind.Type
            ? ((INamedTypeSymbol)jsonSerializerContext.Value!).ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
            : null;

        HasJsonSerializerOptions = !clientDefinition.ClientType.GetMembers("JsonSerializerOptions").IsDefaultOrEmpty;
        
        Methods = GetMethods(BridgeType).Select(p => new Method(p, this)).ToArray();
    }

    private static IEnumerable<IMethodSymbol> GetMethods(INamedTypeSymbol? type)
    {
        if (type == null || type.Name == "Object" && type.ContainingNamespace.Name == "System")
            yield break;

        foreach (var member in GetMethods(type.BaseType))
            yield return member;

        foreach (var member in type.GetMembers().OfType<IMethodSymbol>())
            yield return member;
    }

    public void BuildSource(StringBuilder sb)
    {
        sb.AppendLine(
                $"public partial class {ClientDefinition.ClientType.Name} : {BridgeType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}")
            .AppendLine("{");
        // .AppendLine("    protected partial global::System.Net.Http.HttpClient HttpClient { get; }");

        foreach (var method in Methods)
            method.BuildSource(sb);

        sb.AppendLine("}").AppendLine();
    }
};