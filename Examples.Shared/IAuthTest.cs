using System.Text.Json.Serialization;
using System.Threading.Tasks;
using NexArc.InterfaceBridge;

namespace Examples.Shared;

public record UserInfo(bool IsAuthenticated, string? Name, string[] Roles);

[RestConnector("api/auth")]
public interface IAuthTest
{
    [Rest(HttpMethod.Get, "authorize")]
    Task<string> Authorize();

    [Rest(HttpMethod.Get, "admin")]
    Task<string> AuthorizeAdmin();

    [Rest(HttpMethod.Get, "anonymous")]
    Task<string> AuthorizeAnonymous();

    [Rest(HttpMethod.Get, "me")]
    Task<UserInfo> WhoAmI();

    [Rest(HttpMethod.Post, "sign-in")]
    Task SignIn(string email, string? role);

    [Rest(HttpMethod.Post, "sign-out")]
    Task SignOut();
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower)]
[JsonSerializable(typeof(UserInfo))]
public partial class AuthJsonContext : JsonSerializerContext { }
