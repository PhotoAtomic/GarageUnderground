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
        if (context?.User.Identity?.IsAuthenticated != true)
        {
            return Task.FromResult<Client.UserInfo?>(
                new Client.UserInfo(false, null, null, null, null));
        }

        var claims = context.User.Claims;
        var name = claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;
        var email = claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
        var provider = claims.FirstOrDefault(c => c.Type == "auth_provider")?.Value;
        var roles = claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToArray();

        return Task.FromResult<Client.UserInfo?>(
            new Client.UserInfo(true, name, email, provider, roles));
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
