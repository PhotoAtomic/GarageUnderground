using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace GarageUnderground.Authentication;

/// <summary>
/// Mock authentication handler that authenticates all users automatically.
/// Used when no real authentication providers are configured.
/// </summary>
public class MockAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "MockAuth";
    public const string DisplayName = "Development Login";

    public MockAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Check if we have an existing mock authentication cookie
        if (Context.Request.Cookies.TryGetValue("mock_auth", out var mockAuthValue) &&
            !string.IsNullOrEmpty(mockAuthValue))
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "mock-user-id"),
                new Claim(ClaimTypes.Name, mockAuthValue),
                new Claim(ClaimTypes.Email, $"{mockAuthValue.ToLowerInvariant().Replace(" ", ".")}@mock.local"),
                new Claim(ClaimTypes.Role, "User"),
                new Claim(ClaimTypes.Role, "Admin"),
                new Claim("auth_provider", SchemeName)
            };

            var identity = new ClaimsIdentity(claims, SchemeName);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, SchemeName);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }

        return Task.FromResult(AuthenticateResult.NoResult());
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.Redirect("/login");
        return Task.CompletedTask;
    }
}
