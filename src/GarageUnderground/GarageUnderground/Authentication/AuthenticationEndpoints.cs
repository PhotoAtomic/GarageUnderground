using System.Security.Claims;
using GarageUnderground.Persistence;
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

        // Debug: Get all claims (useful to see what Microsoft returns)
        group.MapGet("/claims", GetAllClaims);

        // Get available providers
        group.MapGet("/providers", GetProviders);

        // Challenge with specific provider (initiates OAuth flow)
        group.MapGet("/login/{scheme}", ChallengeProvider);

        // Mock login (only for mock auth)
        group.MapPost("/mock-login", MockLogin);

        // Logout
        group.MapPost("/logout", Logout);

        // Debug endpoints - protected with CanAdmin policy (case-insensitive)
        var debugGroup = endpoints.MapGroup("/api/auth/debug")
            .RequireAuthorization("CanAdmin");

        // Debug: Add role to current user (temporary for testing)
        debugGroup.MapPost("/add-role", AddRoleToCurrentUser);

        // Debug: Get internal roles for current user
        debugGroup.MapGet("/internal-roles", GetInternalRoles);

        return endpoints;
    }

    private static IResult GetAllClaims(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            return Results.Ok(new { authenticated = false, claims = Array.Empty<object>() });
        }

        var claims = context.User.Claims.Select(c => new 
        { 
            type = c.Type, 
            value = c.Value 
        }).ToArray();

        return Results.Ok(new { authenticated = true, claims });
    }

    private static IResult GetCurrentUser(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            return Results.Ok(new UserInfo(false, null, null, null, []));
        }

        var claims = context.User.Claims;
        
        // Name: prefer givenname (first name) over display name
        // Google uses: ClaimTypes.GivenName, "given_name"
        // Microsoft uses: ClaimTypes.GivenName
        var name = claims.FirstOrDefault(c => c.Type == ClaimTypes.GivenName)?.Value
                ?? claims.FirstOrDefault(c => c.Type == "given_name")?.Value
                ?? claims.FirstOrDefault(c => c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/givenname")?.Value
                ?? claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value
                ?? claims.FirstOrDefault(c => c.Type == "name")?.Value;
        
        // Email: try various claim types
        // Google uses: ClaimTypes.Email, "email"
        // Microsoft uses: ClaimTypes.Email, "preferred_username"
        var email = claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value
                 ?? claims.FirstOrDefault(c => c.Type == "email")?.Value
                 ?? claims.FirstOrDefault(c => c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress")?.Value
                 ?? claims.FirstOrDefault(c => c.Type == "preferred_username")?.Value;
        
        // Provider: determine based on authentication type or claims
        var provider = DetermineProvider(context.User);
        
        var roles = claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToArray();

        return Results.Ok(new UserInfo(true, name, email, provider, roles));
    }

    private static string DetermineProvider(ClaimsPrincipal user)
    {
        var claims = user.Claims;
        
        // Check for explicit auth_provider claim (set by mock login)
        var explicitProvider = claims.FirstOrDefault(c => c.Type == "auth_provider")?.Value;
        if (!string.IsNullOrEmpty(explicitProvider))
        {
            return explicitProvider;
        }
        
        // Check for Microsoft identity provider claim
        var msIdentityProvider = claims.FirstOrDefault(c => 
            c.Type == "http://schemas.microsoft.com/identity/claims/identityprovider")?.Value;
        if (!string.IsNullOrEmpty(msIdentityProvider))
        {
            return "Microsoft";
        }
        
        // Check issuer claim for Microsoft
        var issuer = claims.FirstOrDefault(c => c.Type == "iss")?.Value;
        if (issuer?.Contains("login.microsoftonline.com") == true ||
            issuer?.Contains("sts.windows.net") == true)
        {
            return "Microsoft";
        }
        
        // Check for Google
        if (issuer?.Contains("accounts.google.com") == true)
        {
            return "Google";
        }
        
        // Check authentication type
        var authType = user.Identity?.AuthenticationType;
        if (authType?.Contains("Microsoft", StringComparison.OrdinalIgnoreCase) == true)
        {
            return "Microsoft";
        }
        if (authType?.Contains("Google", StringComparison.OrdinalIgnoreCase) == true)
        {
            return "Google";
        }
        
        return authType ?? "Unknown";
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
        IAuthenticationProviderService providerService,
        IClaimsEnrichmentService claimsEnrichmentService)
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

        // Create base claims with ALL available roles for mock user
        var email = $"{displayName.ToLowerInvariant().Replace(" ", ".")}@mock.local";
        var baseClaims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, "mock-user-id"),
            new Claim(ClaimTypes.Name, displayName),
            new Claim(ClaimTypes.Email, email),
            // Mock user gets all roles for testing purposes
            new Claim(ClaimTypes.Role, "canAdmin"),
            new Claim(ClaimTypes.Role, "canLogin"),
            new Claim("auth_provider", MockAuthenticationHandler.SchemeName)
        };

        var identity = new ClaimsIdentity(baseClaims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        // Enrich with internal roles from database (this is a new login)
        var enrichedPrincipal = await claimsEnrichmentService.EnrichClaimsAsync(principal, isNewLogin: true);

        await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, enrichedPrincipal,
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

    private static async Task<IResult> AddRoleToCurrentUser(
        AddRoleRequest request,
        HttpContext context,
        IUserRolesRepository userRolesRepository)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            return Results.Unauthorized();
        }

        var claims = context.User.Claims;
        
        // Get user identifier (prefer email)
        var email = claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value
                 ?? claims.FirstOrDefault(c => c.Type == "email")?.Value;
        
        if (string.IsNullOrWhiteSpace(email))
        {
            return Results.BadRequest("Cannot determine user identifier");
        }

        var name = claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;
        var provider = claims.FirstOrDefault(c => c.Type == "auth_provider")?.Value;

        // Add role to database
        var userRole = await userRolesRepository.AddRolesAsync(
            email,
            "email",
            [request.Role],
            name,
            provider);

        return Results.Ok(new { 
            success = true, 
            userIdentifier = email,
            roles = userRole.Roles,
            message = "Role added. Log out and log in again to see the changes."
        });
    }

    private static async Task<IResult> GetInternalRoles(
        HttpContext context,
        IUserRolesRepository userRolesRepository)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            return Results.Ok(new { authenticated = false, internalRoles = Array.Empty<string>(), currentRoles = Array.Empty<string>() });
        }

        var claims = context.User.Claims;
        
        // Get current roles from claims
        var currentRoles = claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToArray();
        
        // Get user identifier
        var email = claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value
                 ?? claims.FirstOrDefault(c => c.Type == "email")?.Value;

        string[] internalRoles = [];
        if (!string.IsNullOrWhiteSpace(email))
        {
            var userRole = await userRolesRepository.GetByUserIdentifierAsync(email);
            internalRoles = userRole?.Roles.ToArray() ?? [];
        }

        return Results.Ok(new { 
            authenticated = true,
            userIdentifier = email,
            internalRoles,
            currentRoles
        });
    }
}

/// <summary>
/// Request to add a role.
/// </summary>
public record AddRoleRequest(string Role);

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
