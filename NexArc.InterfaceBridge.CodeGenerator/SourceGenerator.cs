using System;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NexArc.InterfaceBridge.CodeGenerator;

public class SourceGenerator
{
    internal static string AppendAttributeSuffix(string identifierText) =>
        identifierText.EndsWith("Attribute", StringComparison.OrdinalIgnoreCase)
            ? identifierText
            : identifierText + "Attribute";

    internal static ITypeSymbol? GetTypeFromAttribute(GeneratorSyntaxContext context,
        CancellationToken cancellationToken)
    {
        var attributeSyntax = (AttributeSyntax)context.Node;

        // "attribute.Parent" is "AttributeListSyntax"
        // "attribute.Parent.Parent" is a C# fragment the attributes are applied to
        TypeDeclarationSyntax? typeNode = attributeSyntax.Parent?.Parent switch
        {
            ClassDeclarationSyntax classDeclarationSyntax => classDeclarationSyntax,
            RecordDeclarationSyntax recordDeclarationSyntax => recordDeclarationSyntax,
            _ => null
        };

        if (typeNode == null)
            return null;

        if (context.SemanticModel.GetDeclaredSymbol(typeNode) is not ITypeSymbol type)
            return null;

        return type;
    }
}