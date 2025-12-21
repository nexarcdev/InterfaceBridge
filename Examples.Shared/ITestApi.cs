using System;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using NexArc.InterfaceBridge;

namespace Examples.Shared;

public enum TestEnum { A, B, C }

public record PocoTest(string ChildName, int ChildAge);

public record TestRequest(string FullName, int Age, PocoTest PocoData);

public record TestResponse(Guid TestId, string FullName, int Age, PocoTest PocoData);

[RestConnector("api/fulltest")]
public interface ITestApi
{
    [Rest(HttpMethod.Get, "{id:guid}")]
    Task<TestResponse> Get(Guid id, TestEnum? e, TestRequest request);
    
    [Rest(HttpMethod.Get, "nulltest")]
    Task<string> NullTest(string email, string? code, string? token);

    [Rest(HttpMethod.Get, "all")]
    Task<TestResponse[]> Get(TestRequest[] requests);

    [Rest(HttpMethod.Post, "{id:guid}")]
    Task<TestResponse> Post(Guid id, TestEnum e, TestRequest request);

    [Rest(HttpMethod.Put, "{id:guid}")]
    Task<Guid> Put(Guid id, FilePart file);
    
    [Rest(HttpMethod.Post, "test")]
    Task<string> StringTest(string? value);
    
    [Rest(HttpMethod.Get, "download")]
    Task<FilePart> Download();
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower)]
[JsonConverter(typeof(JsonStringEnumConverter))]
[JsonSerializable(typeof(TestRequest))]
[JsonSerializable(typeof(TestRequest[]))]
[JsonSerializable(typeof(TestResponse))]
[JsonSerializable(typeof(TestResponse[]))]
[JsonSerializable(typeof(TestEnum?))]
public partial class TestJsonContext : JsonSerializerContext { }