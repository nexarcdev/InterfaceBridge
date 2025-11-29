using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace NexArc.InterfaceBridge.CodeGenerator.Tests;

[TestClass]
public class ManagerSourceGeneratorTests
{
    
    private static Compilation CreateCompilation(string source)
    {
        var references = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
            .Select(a => MetadataReference.CreateFromFile(a.Location))
            .Concat([
                MetadataReference.CreateFromFile(typeof(System.Text.Json.JsonDocument).GetTypeInfo().Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Net.Http.HttpClient).GetTypeInfo().Assembly.Location),
                MetadataReference.CreateFromFile(typeof(RestConnectorAttribute).GetTypeInfo().Assembly.Location)
            ])
            .ToArray();

        return CSharpCompilation.Create("compilation",
            [CSharpSyntaxTree.ParseText(source)],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }
    
    
    [TestMethod]
    public void Namespace_Works()
    {
        // Create the 'input' compilation that the generator will act on
        var inputCompilation = CreateCompilation(
            """
            using System.Net.Http;
            using System.Threading;
            using System.Threading.Tasks;
            using NexArc.InterfaceBridge;

            namespace Examples.Shared;

            [RestConnector("api/hello")]
            public interface IHelloApi
            {
                [Rest(NexArc.InterfaceBridge.HttpMethod.Get, "greet/{name}")]
                Task<string> Greet(string name, CancellationToken cancellationToken = default);
            }

            [Bridge(typeof(IHelloApi))]
            public partial class HelloClient
            {
                public HttpClient HttpClient { get; }

                public HelloClient(HttpClient httpClient)
                {
                    HttpClient = httpClient;
                }
            }
            """);

        var compilationDiagnostics = inputCompilation.GetDiagnostics();

        // inputCompilation needs to build without errors
        foreach (var error in compilationDiagnostics.Where(d => d.Severity == DiagnosticSeverity.Error))
            Assert.Fail(error.ToString());

        // Create an instance of the generator
        var generator = new ManagerSourceGenerator();

        // Create the driver that will control the generation, passing in our generator
        // Run the generation pass
        CSharpGeneratorDriver
            .Create(generator)
            .RunGeneratorsAndUpdateCompilation(inputCompilation, out var outputCompilation, out var diagnostics);

        Assert.IsEmpty(diagnostics); // No diagnostics created by the generators
        Assert.HasCount(2, outputCompilation.SyntaxTrees); // Two syntax trees: 'inputCompilation' and the one added by the generator
        Assert.IsEmpty(outputCompilation.GetDiagnostics()); // Verify the compilation with the added source has no diagnostics
    }

    [TestMethod]
    public void NoNameSpace_Works()
    {
        // Create the 'input' compilation that the generator will act on
        var inputCompilation = CreateCompilation(
            """
            using System.Net.Http;
            using System.Threading;
            using System.Threading.Tasks;
            using NexArc.InterfaceBridge;

            [RestConnector("api/hello")]
            public interface IHelloApi
            {
                [Rest(NexArc.InterfaceBridge.HttpMethod.Get, "greet/{name}")]
                Task<string> Greet(string name, CancellationToken cancellationToken = default);
            }

            [Bridge(typeof(IHelloApi))]
            public partial class HelloClient
            {
                public HttpClient HttpClient { get; }

                public HelloClient(HttpClient httpClient)
                {
                    HttpClient = httpClient;
                }
            }
            """);

        var compilationDiagnostics = inputCompilation.GetDiagnostics();

        // inputCompilation needs to build without errors
        foreach (var error in compilationDiagnostics.Where(d => d.Severity == DiagnosticSeverity.Error))
            Assert.Fail(error.ToString());

        // Create an instance of the generator
        var generator = new ManagerSourceGenerator();

        // Create the driver that will control the generation, passing in our generator
        // Run the generation pass
        CSharpGeneratorDriver
            .Create(generator)
            .RunGeneratorsAndUpdateCompilation(inputCompilation, out var outputCompilation, out var diagnostics);

        Assert.IsEmpty(diagnostics); // No diagnostics created by the generators
        Assert.HasCount(2, outputCompilation.SyntaxTrees); // Two syntax trees: 'inputCompilation' and the one added by the generator
        Assert.IsEmpty(outputCompilation.GetDiagnostics()); // Verify the compilation with the added source has no diagnostics
    }

    [TestMethod]
    public void Poco_Works()
    {
        // Create the 'input' compilation that the generator will act on
        var inputCompilation = CreateCompilation(
            """
            using System;
            using System.Net.Http;
            using System.Threading;
            using System.Threading.Tasks;
            using NexArc.InterfaceBridge;

            namespace Examples.Shared;

            public class DataRequest  
            {
                public Guid RequestId { get; set; }
                public string Query { get; set; }
            }
            
            public class DataObject 
            {
                public Guid RequestId { get; set; }
                public Guid Id { get; set; }
                public string Name { get; set; }
            }

            [RestConnector("api/hello")]
            public interface IHelloApi
            {
                [Rest(NexArc.InterfaceBridge.HttpMethod.Get, "data")]
                Task<DataObject[]> GetData(DataRequest request, CancellationToken cancellationToken = default);

                [Rest(NexArc.InterfaceBridge.HttpMethod.Get, "data/{id:guid}")]
                Task<DataObject> GetData(Guid id, CancellationToken cancellationToken = default);
            }

            [Bridge(typeof(IHelloApi))]
            public partial class HelloClient
            {
                public HttpClient HttpClient { get; }

                public HelloClient(HttpClient httpClient)
                {
                    HttpClient = httpClient;
                }
            }
            """);

        var compilationDiagnostics = inputCompilation.GetDiagnostics();

        // inputCompilation needs to build without errors
        foreach (var error in compilationDiagnostics.Where(d => d.Severity == DiagnosticSeverity.Error))
            Assert.Fail(error.ToString());

        // Create an instance of the generator
        var generator = new ManagerSourceGenerator();

        // Create the driver that will control the generation, passing in our generator
        // Run the generation pass
        CSharpGeneratorDriver
            .Create(generator)
            .RunGeneratorsAndUpdateCompilation(inputCompilation, out var outputCompilation, out var diagnostics);

        Assert.IsEmpty(diagnostics); // No diagnostics created by the generators
        Assert.HasCount(2, outputCompilation.SyntaxTrees); // Two syntax trees: 'inputCompilation' and the one added by the generator
        
        foreach (var error in outputCompilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error))
            Assert.Fail(error.ToString());
    }
    
    
    [TestMethod]
    public void PocoSerializer_Works()
    {
        // Create the 'input' compilation that the generator will act on
        var inputCompilation = CreateCompilation(
            """
            using System;
            using System.Net.Http;
            using System.Threading;
            using System.Threading.Tasks;
            using NexArc.InterfaceBridge;
            using System.Text.Json.Serialization;
            
            namespace Examples.Shared;
            
            public class DataRequest 
            {
                public Guid Id { get; set; }
                public string Name { get; set; }
            }
            
            public class DataResponse 
            {
                public Guid Id { get; set; }
                public string Name { get; set; }
                public DataRequest Request { get; set; }
            }

            [JsonSourceGenerationOptions(WriteIndented = true)]
            [JsonSerializable(typeof(DataRequest))]
            [JsonSerializable(typeof(DataRequest[]))]
            [JsonSerializable(typeof(DataResponse))]
            [JsonSerializable(typeof(DataResponse[]))]
            public partial class AppJsonContext : JsonSerializerContext {
                // STUB: Satisfy the abstract base class requirements
                public static AppJsonContext Default { get; } = new AppJsonContext();
                public AppJsonContext() : base(null) { }
                protected override System.Text.Json.JsonSerializerOptions? GeneratedSerializerOptions => null;
                public override global::System.Text.Json.Serialization.Metadata.JsonTypeInfo? GetTypeInfo(Type type) => null;
                
                public global::System.Text.Json.Serialization.Metadata.JsonTypeInfo<DataRequest> DataRequest => null!;
                public global::System.Text.Json.Serialization.Metadata.JsonTypeInfo<DataRequest[]> DataRequestArray => null!;
                public global::System.Text.Json.Serialization.Metadata.JsonTypeInfo<DataResponse> DataResponse => null!;
                public global::System.Text.Json.Serialization.Metadata.JsonTypeInfo<DataResponse[]> DataResponseArray => null!;
             }
            
            [RestConnector("api/hello")]
            public interface IHelloApi
            {
                [Rest(NexArc.InterfaceBridge.HttpMethod.Post, "data/{id:guid}")]
                Task<DataResponse> SendData(Guid id, DataRequest request, CancellationToken cancellationToken = default);

                [Rest(NexArc.InterfaceBridge.HttpMethod.Post, "data")]
                Task<DataResponse[]> SendData(DataRequest[] requests, CancellationToken cancellationToken = default);
            }

            [Bridge(typeof(IHelloApi), JsonSerializerContext = typeof(AppJsonContext))]
            public partial class HelloClient
            {
                public HttpClient HttpClient { get; }

                public HelloClient(HttpClient httpClient)
                {
                    HttpClient = httpClient;
                }
            }
            """);

        var compilationDiagnostics = inputCompilation.GetDiagnostics();

        // inputCompilation needs to build without errors
        foreach (var error in compilationDiagnostics.Where(d => d.Severity == DiagnosticSeverity.Error))
            Assert.Fail(error.ToString());

        // Create an instance of the generator
        var generator = new ManagerSourceGenerator();

        // Create the driver that will control the generation, passing in our generator
        // Run the generation pass
        CSharpGeneratorDriver
            .Create(generator)
            .RunGeneratorsAndUpdateCompilation(inputCompilation, out var outputCompilation, out var diagnostics);

        Assert.IsEmpty(diagnostics); // No diagnostics created by the generators
        Assert.HasCount(2, outputCompilation.SyntaxTrees); // Two syntax trees: 'inputCompilation' and the one added by the generator
        
        foreach (var error in outputCompilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error))
            Assert.Fail(error.ToString());
    }
}