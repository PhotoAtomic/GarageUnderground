using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace GarageUnderground.Authentication;

/// <summary>
/// Extension methods for mapping authentication endpoints.
/// </summary>
public static class AuthenticationEndpoints
{
    /// <summary>
    /// Maps authentication-related endpoints.
    /// </summary>
    public static IEndpointRouteBuilder MapAuthenticationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/auth");

        // Get current user info
        group.MapGet("/user", GetCurrentUser);

        // Get available providers
        group.MapGet("/providers", GetProviders);

        // Challenge with specific provider (initiates OAuth flow)
        group.MapGet("/login/{scheme}", ChallengeProvider);

        // Mock login (only for mock auth)
        group.MapPost("/mock-login", MockLogin);

        // Logout
        group.MapPost("/logout", Logout);

        return endpoints;
    }

    private static IResult GetCurrentUser(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            return Results.Ok(new UserInfo(false, null, null, null, []));
        }

        var claims = context.User.Claims;
        var name = claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;
        var email = claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
        var provider = claims.FirstOrDefault(c => c.Type == "auth_provider")?.Value
                    ?? claims.FirstOrDefault(c => c.Type == "http://schemas.microsoft.com/identity/claims/identityprovider")?.Value
                    ?? "Unknown";
        var roles = claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToArray();

        return Results.Ok(new UserInfo(true, name, email, provider, roles));
    }

    private static IResult GetProviders(IAuthenticationProviderService providerService)
    {
        var providers = providerService.GetAvailableProviders()
            .Select(p => new ProviderInfo(p.Scheme, p.DisplayName, p.IconClass))
            .ToArray();

        return Results.Ok(new ProvidersResponse(providers, providerService.IsMockAuthenticationActive));
    }

    private static IResult ChallengeProvider(
        string scheme,
        HttpContext context,
        IAuthenticationProviderService providerService)
    {
        var providers = providerService.GetAvailableProviders();
        if (!providers.Any(p => p.Scheme.Equals(scheme, StringComparison.OrdinalIgnoreCase)))
        {
            return Results.BadRequest("Invalid authentication scheme");
        }

        // The redirect after successful OAuth is configured in AuthenticationServiceExtensions
        // via OnTicketReceived event
        var properties = new AuthenticationProperties
        {
            RedirectUri = "/dashboard",
            IsPersistent = true
        };

        return Results.Challenge(properties, [scheme]);
    }

    private static async Task<IResult> MockLogin(
        MockLoginRequest request,
        HttpContext context,
        IAuthenticationProviderService providerService)
    {
        if (!providerService.IsMockAuthenticationActive)
        {
            return Results.BadRequest("Mock authentication is not enabled");
        }

        var displayName = string.IsNullOrWhiteSpace(request.DisplayName)
            ? "Mock User"
            : request.DisplayName;

        // Set mock auth cookie
        context.Response.Cookies.Append("mock_auth", displayName, new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Lax,
            Expires = DateTimeOffset.UtcNow.AddDays(30)
        });

        // Sign in with cookie authentication
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "mock-user-id"),
            new Claim(ClaimTypes.Name, displayName),
            new Claim(ClaimTypes.Email, $"{displayName.ToLowerInvariant().Replace(" ", ".")}@mock.local"),
            new Claim(ClaimTypes.Role, "User"),
            new Claim(ClaimTypes.Role, "Admin"),
            new Claim("auth_provider", MockAuthenticationHandler.SchemeName)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal,
            new AuthenticationProperties { IsPersistent = true });

        return Results.Ok(new { success = true, redirectUrl = "/dashboard" });
    }

    private static async Task<IResult> Logout(HttpContext context)
    {
        // Remove mock auth cookie if present
        context.Response.Cookies.Delete("mock_auth");

        await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        return Results.Ok(new { success = true, redirectUrl = "/" });
    }
}

/// <summary>
/// Response containing current user information.
/// </summary>
public record UserInfo(
    bool IsAuthenticated,
    string? Name,
    string? Email,
    string? Provider,
    string[] Roles);

/// <summary>
/// Information about an authentication provider.
/// </summary>
public record ProviderInfo(string Scheme, string DisplayName, string IconClass);

/// <summary>
/// Response containing available providers.
/// </summary>
public record ProvidersResponse(ProviderInfo[] Providers, bool IsMockAuthEnabled);

/// <summary>
/// Request for mock login.
/// </summary>
public record MockLoginRequest(string? DisplayName);
