using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace NexArc.InterfaceBridge.CodeGenerator;

public class ClientDefinition
{
    public const string BridgeAttributeName = "BridgeAttribute";

    public INamedTypeSymbol ClientType { get; }

    public List<Bridge> Bridges { get; }

    internal static readonly HashSet<string> BuildInTypes =
    [
        "global::System.Guid",
        "int", "long", "short", "byte", "decimal", "double", "float", "bool",
        "uint", "ulong", "ushort", "sbyte", "string"
    ];

    internal const string FilePartTypeName = "global::NexArc.InterfaceBridge.FilePart";
    internal const string JsonPatchDocumentTypeName = "global::SystemTextJsonPatch.JsonPatchDocument";

    public ClientDefinition(INamedTypeSymbol clientType)
    {
        ClientType = clientType;
        Bridges = ClientType.GetAttributes().Where(attribute => attribute.AttributeClass?.Name == BridgeAttributeName)
            .Select(attribute => new Bridge(this, attribute))
            .ToList();
    }
}