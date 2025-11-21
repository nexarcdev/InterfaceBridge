using System;
using System.Text;
using Microsoft.CodeAnalysis.Text;

namespace NexArc.InterfaceBridge.CodeGenerator;

public static class ClientSourceBuilder
{
    public static SourceText GenerateClient(ClientDefinition clientDefinition)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"#nullable enable").AppendLine();
        sb.AppendLine($"namespace {clientDefinition.ClientType.ContainingNamespace.ToDisplayString()};").AppendLine();

        foreach (var bridge in clientDefinition.Bridges)
            bridge.BuildSource(sb);

        return SourceText.From(sb.ToString(), Encoding.UTF8);
    }
}