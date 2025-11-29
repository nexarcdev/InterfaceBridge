using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace NexArc.InterfaceBridge.CodeGenerator;

public class Argument
{
    public Argument(Method method, IParameterSymbol parameterSymbol, HashSet<string> routeParameters)
    {
        Name = parameterSymbol.Name;
        IsInRoute = routeParameters.Contains(Name);
        Type = parameterSymbol.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        IsCancellationToken = Type == "global::System.Threading.CancellationToken";
        IsNullable = parameterSymbol.Type is INamedTypeSymbol
        {
            ConstructedFrom.SpecialType: SpecialType.System_Nullable_T
        };
        if (parameterSymbol.Type is INamedTypeSymbol { IsGenericType: true } namedTypeSymbol)
        {
            SubType = namedTypeSymbol.TypeArguments[0].Name;
        }

        IsFilePart = Type == ClientDefinition.FilePartTypeName;

        Serializer = BuildSerializer(method, parameterSymbol);
    }

    private string BuildSerializer(Method method, IParameterSymbol parameterSymbol)
    {
        if (parameterSymbol.Type.TypeKind == TypeKind.Enum)
            return $"{Name}.ToString()";

        if (Type == "string")
            return Name;
        
        if (!ClientDefinition.BuildInTypes.Contains(Type.TrimEnd('?')))
        {
            if (string.IsNullOrEmpty(method.Bridge.JsonSerializerContext))
                return $"global::System.Text.Json.JsonSerializer.Serialize({Name})";

            if (Type == ClientDefinition.JsonPatchDocumentTypeName)
                return $"global::System.Text.Json.JsonSerializer.Serialize({Name}, {method.Bridge.JsonSerializerContext}.Default.JsonPatchDocument{Type})";

            if (Type.StartsWith(ClientDefinition.JsonPatchDocumentTypeName))
                return $"global::System.Text.Json.JsonSerializer.Serialize({Name}, {method.Bridge.JsonSerializerContext}.Default.JsonPatchDocument{SubType})";

            if (parameterSymbol.Type.Kind == SymbolKind.ArrayType && parameterSymbol.Type is IArrayTypeSymbol arrayTypeSymbol)
                return $"global::System.Text.Json.JsonSerializer.Serialize({Name}, {method.Bridge.JsonSerializerContext}.Default.{arrayTypeSymbol.ElementType.Name}Array)";

            return $"global::System.Text.Json.JsonSerializer.Serialize({Name}, {method.Bridge.JsonSerializerContext}.Default.{parameterSymbol.Type.Name})";
        }

        return $"{Name}.ToString()";
    }

    public string Name { get; }
    public string Type { get; }
    public string? SubType { get; }
    public bool IsNullable { get; }
    public bool IsFilePart { get; }
    public bool IsInRoute { get; }
    public bool IsCancellationToken { get; set; }
    public string? Serializer { get; }
}