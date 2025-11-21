using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NexArc.InterfaceBridge.CodeGenerator;

[Generator(LanguageNames.CSharp)]
public class ManagerSourceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var types =
            context.SyntaxProvider.CreateSyntaxProvider(IsModel, SourceGenerator.GetTypeFromAttribute)
                .Where(t => t != null)
                .Collect();

        context.RegisterSourceOutput(types, GenerateSource!);
    }

    private static bool IsModel(SyntaxNode syntaxNode, CancellationToken cancellationToken) =>
        syntaxNode is AttributeSyntax attribute
        && attribute.Name switch
        {
            SimpleNameSyntax ins => SourceGenerator.AppendAttributeSuffix(ins.Identifier.Text),
            QualifiedNameSyntax qns => SourceGenerator.AppendAttributeSuffix(qns.Right.Identifier.Text),
            _ => null
        } is ClientDefinition.BridgeAttributeName;

    private static void GenerateSource(SourceProductionContext context, ImmutableArray<ITypeSymbol> types)
    {
        if (types.IsDefaultOrEmpty)
            return;

        foreach (var type in types.Distinct(SymbolEqualityComparer.Default))
        {
            if (type is null) continue;

            context.CancellationToken.ThrowIfCancellationRequested();

            var model = new ClientDefinition((INamedTypeSymbol)type);
            var source = ClientSourceBuilder.GenerateClient(model);

            var hintSymbolDisplayFormat = new SymbolDisplayFormat(
                globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
                genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
                miscellaneousOptions: SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers |
                                      SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

            var hintName = type.ToDisplayString(hintSymbolDisplayFormat)
                .Replace('<', '[')
                .Replace('>', ']');

            context.AddSource($"{hintName}.g.cs", source);
        }
    }
}