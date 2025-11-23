using System.Diagnostics;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace NexArc.InterfaceBridge.CodeGenerator.Tests;

[TestClass]
public sealed class ManagerSourceGeneratorTests
{
    [TestClass]
    public class GeneratorTests
    {
        [TestMethod]
        public void SimpleGeneratorTest()
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

        private static Compilation CreateCompilation(string source)
        {
            var references = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
                .Select(a => MetadataReference.CreateFromFile(a.Location))
                .Concat([
                    MetadataReference.CreateFromFile(typeof(System.Net.Http.HttpClient).GetTypeInfo().Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(RestConnectorAttribute).GetTypeInfo().Assembly.Location)
                ])
                .ToArray();

            return CSharpCompilation.Create("compilation",
                [CSharpSyntaxTree.ParseText(source)],
                references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        }
    }
}