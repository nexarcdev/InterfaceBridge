using Examples.Shared;
using NexArc.InterfaceBridge;

namespace Examples.Server;

public class TestApi : ITestApi
{
    public Task<TestResponse> Get(Guid id, TestEnum e, TestRequest request) =>
        Task.FromResult(new TestResponse(id, request.FullName, request.Age, request.PocoData));

    public Task<TestResponse[]> Get(TestRequest[] requests) =>
        Task.FromResult(requests
            .Select(request => new TestResponse(Guid.NewGuid(), request.FullName, request.Age, request.PocoData))
            .ToArray());

    public Task<TestResponse> Post(Guid id, TestEnum e, TestRequest request) => 
        Task.FromResult(new TestResponse(id, request.FullName, request.Age, request.PocoData));

    public Task<Guid> Put(Guid id, FilePart file) => 
        Task.FromResult(id);
}