using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using Examples.Shared;
using NexArc.InterfaceBridge;

namespace Examples.Server;

public class TestApi : ITestApi
{
    public Task<TestResponse> Get(Guid id, TestEnum? e, TestRequest request) =>
        Task.FromResult(new TestResponse(id, request.FullName, request.Age, request.PocoData));

    public Task<string> NullTest(string email, string? code, string? token) =>
        Task.FromResult($"{email} Code={code} Token={token}");

    public Task<TestResponse[]> Get(TestRequest[] requests) =>
        Task.FromResult(requests
            .Select(request => new TestResponse(Guid.NewGuid(), request.FullName, request.Age, request.PocoData))
            .ToArray());

    public Task<TestResponse> Post(Guid id, TestEnum e, TestRequest request) => 
        Task.FromResult(new TestResponse(id, request.FullName, request.Age, request.PocoData));

    public Task<Guid> Put(Guid id, FilePart file) =>
        id != Guid.Empty 
            ? Task.FromResult(id)
            : throw new HttpResponseException(HttpStatusCode.NotFound, "That file not found");

    public Task<FilePart> Download()
    {
        return Task.FromResult(FilePart.Create([1, 2, 3], "test.txt", "text/plain"));
    }
    
    public Task<string> StringTest(string? value)
    {
        Debug.Assert(value == "Hello, World!");
        return Task.FromResult("World says hello!");
    }
}