using System.Security.Claims;
using Examples.Shared;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace Examples.Server;

public class AuthTest : IAuthTest
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuthTest(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    [Authorize]
    public Task<string> Authorize() =>
        Task.FromResult("Authorized");

    [Authorize(Roles = "Admin")]
    public Task<string> AuthorizeAdmin() =>
        Task.FromResult("Admin Authorized");

    [AllowAnonymous]
    public Task<string> AuthorizeAnonymous() =>
        Task.FromResult("Anonymous Authorized");

    public Task<UserInfo> WhoAmI()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user is null)
            throw new InvalidOperationException("No active HttpContext.");

        var roles = user.FindAll(ClaimTypes.Role)
            .Select(claim => claim.Value)
            .Distinct()
            .ToArray();

        var info = new UserInfo(user.Identity?.IsAuthenticated == true, user.Identity?.Name, roles);
        return Task.FromResult(info);
    }

    public async Task SignIn(string email, string? role)
    {
        var context = _httpContextAccessor.HttpContext;
        if (context is null)
            throw new InvalidOperationException("No active HttpContext.");

        var claims = new List<Claim> { new(ClaimTypes.Name, email) };
        if (!string.IsNullOrWhiteSpace(role))
            claims.Add(new Claim(ClaimTypes.Role, role));

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
    }

    public Task SignOut()
    {
        var context = _httpContextAccessor.HttpContext;
        if (context is null)
            throw new InvalidOperationException("No active HttpContext.");

        return context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    }
}
