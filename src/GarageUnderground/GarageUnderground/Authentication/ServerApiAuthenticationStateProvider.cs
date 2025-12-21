using System.Security.Claims;
using GarageUnderground.Authentication.Client;

namespace GarageUnderground.Authentication;

/// <summary>
/// Server-side implementation of ApiAuthenticationStateProvider for pre-rendering.
/// This reads authentication state directly from HttpContext instead of making HTTP calls.
/// </summary>
public sealed class ServerApiAuthenticationStateProvider : ApiAuthenticationStateProvider
{
    private readonly IHttpContextAccessor httpContextAccessor;
    private readonly IAuthenticationProviderService providerService;

    public ServerApiAuthenticationStateProvider(
        IHttpContextAccessor httpContextAccessor,
        IAuthenticationProviderService providerService,
        ILogger<ServerApiAuthenticationStateProvider> logger)
        : base(logger)
    {
        this.httpContextAccessor = httpContextAccessor;
        this.providerService = providerService;
    }

    public override Task<Client.UserInfo?> GetCurrentUserAsync(bool forceRefresh = false)
    {
        var context = httpContextAccessor.HttpContext;
        
        Console.WriteLine($"[ServerApiAuthenticationStateProvider] GetCurrentUserAsync called with forceRefresh={forceRefresh}");
        Console.WriteLine($"[ServerApiAuthenticationStateProvider] HttpContext is null: {context == null}");
        
        if (context != null)
        {
            Console.WriteLine($"[ServerApiAuthenticationStateProvider] User.Identity is null: {context.User.Identity == null}");
            Console.WriteLine($"[ServerApiAuthenticationStateProvider] User authenticated: {context.User.Identity?.IsAuthenticated}");
        }
        
        if (context?.User.Identity?.IsAuthenticated != true)
        {
            return Task.FromResult<Client.UserInfo?>(
                new Client.UserInfo(false, null, null, null, null));
        }

        var claims = context.User.Claims.ToList();
        Console.WriteLine($"[ServerApiAuthenticationStateProvider] Total claims: {claims.Count}");
        
        var name = claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;
        var email = claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
        var provider = claims.FirstOrDefault(c => c.Type == "auth_provider")?.Value;
        var roles = claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToArray();
        
        Console.WriteLine($"[ServerApiAuthenticationStateProvider] Name: {name}");
        Console.WriteLine($"[ServerApiAuthenticationStateProvider] Email: {email}");
        Console.WriteLine($"[ServerApiAuthenticationStateProvider] Provider: {provider}");
        Console.WriteLine($"[ServerApiAuthenticationStateProvider] Roles found: {roles.Length} - [{string.Join(", ", roles)}]");

        var result = new Client.UserInfo(true, name, email, provider, roles);
        Console.WriteLine($"[ServerApiAuthenticationStateProvider] Returning UserInfo with IsAuthenticated={result.IsAuthenticated}, Roles={string.Join(",", result.Roles ?? Array.Empty<string>())}");
        
        return Task.FromResult<Client.UserInfo?>(result);
    }

    public override Task<Client.ProvidersResponse?> GetProvidersAsync()
    {
        var providers = providerService.GetAvailableProviders()
            .Select(p => new Client.ProviderInfo(p.Scheme, p.DisplayName, p.IconClass))
            .ToArray();

        return Task.FromResult<Client.ProvidersResponse?>(
            new Client.ProvidersResponse(providers, providerService.IsMockAuthenticationActive));
    }

    public override Task<bool> MockLoginAsync(string? displayName)
    {
        // Mock login should be handled by navigating to the API endpoint
        // During pre-rendering, we just return false
        return Task.FromResult(false);
    }

    public override Task<bool> LogoutAsync()
    {
        // Logout should be handled by navigating to the API endpoint
        // During pre-rendering, we just return false
        return Task.FromResult(false);
    }
}
