using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace GarageUnderground.Authentication.Client;

/// <summary>
/// Authentication state provider for Blazor WebAssembly.
/// </summary>
public class ApiAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly HttpClient? httpClient;
    private readonly ILogger<ApiAuthenticationStateProvider> logger;
    private UserInfo? cachedUser;

    public ApiAuthenticationStateProvider(HttpClient httpClient, ILogger<ApiAuthenticationStateProvider> logger)
    {
        this.httpClient = httpClient;
        this.logger = logger;
    }

    // Protected constructor for derived classes that don't need HttpClient
    protected ApiAuthenticationStateProvider(ILogger<ApiAuthenticationStateProvider> logger)
    {
        this.logger = logger;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            var user = await GetCurrentUserAsync();

            if (user?.IsAuthenticated == true)
            {
                var claims = new List<Claim>
                {
                    new(ClaimTypes.Name, user.Name ?? "Unknown"),
                    new(ClaimTypes.Email, user.Email ?? ""),
                    new("auth_provider", user.Provider ?? "")
                };

                foreach (var role in user.Roles ?? [])
                {
                    claims.Add(new Claim(ClaimTypes.Role, role));
                }

                var identity = new ClaimsIdentity(claims, "ApiAuth");
                return new AuthenticationState(new ClaimsPrincipal(identity));
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to get authentication state");
        }

        return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
    }

    /// <summary>
    /// Gets the current user information.
    /// </summary>
    public virtual async Task<UserInfo?> GetCurrentUserAsync(bool forceRefresh = false)
    {
        if (cachedUser != null && !forceRefresh)
        {
            return cachedUser;
        }

        if (httpClient == null)
        {
            return null;
        }

        try
        {
            cachedUser = await httpClient.GetFromJsonAsync<UserInfo>("/api/auth/user");
            return cachedUser;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to fetch user info");
            return null;
        }
    }

    /// <summary>
    /// Gets available authentication providers.
    /// </summary>
    public virtual async Task<ProvidersResponse?> GetProvidersAsync()
    {
        if (httpClient == null)
        {
            return null;
        }

        try
        {
            return await httpClient.GetFromJsonAsync<ProvidersResponse>("/api/auth/providers");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to fetch providers");
            return null;
        }
    }

    /// <summary>
    /// Performs mock login.
    /// </summary>
    public virtual async Task<bool> MockLoginAsync(string? displayName)
    {
        if (httpClient == null)
        {
            return false;
        }

        try
        {
            var response = await httpClient.PostAsJsonAsync("/api/auth/mock-login",
                new MockLoginRequest(displayName));

            if (response.IsSuccessStatusCode)
            {
                cachedUser = null;
                NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
                return true;
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Mock login failed");
        }

        return false;
    }

    /// <summary>
    /// Performs logout.
    /// </summary>
    public virtual async Task<bool> LogoutAsync()
    {
        if (httpClient == null)
        {
            return false;
        }

        try
        {
            var response = await httpClient.PostAsync("/api/auth/logout", null);

            if (response.IsSuccessStatusCode)
            {
                cachedUser = null;
                NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
                return true;
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Logout failed");
        }

        return false;
    }

    /// <summary>
    /// Clears cached user and notifies state change.
    /// </summary>
    protected void ClearCacheAndNotify()
    {
        cachedUser = null;
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    /// <summary>
    /// Notifies that authentication state has changed.
    /// </summary>
    public void NotifyStateChanged()
    {
        ClearCacheAndNotify();
    }
}

/// <summary>
/// User information from API.
/// </summary>
public record UserInfo(
    bool IsAuthenticated,
    string? Name,
    string? Email,
    string? Provider,
    string[]? Roles);

/// <summary>
/// Provider information.
/// </summary>
public record ProviderInfo(string Scheme, string DisplayName, string IconClass);

/// <summary>
/// Response with available providers.
/// </summary>
public record ProvidersResponse(ProviderInfo[] Providers, bool IsMockAuthEnabled);

/// <summary>
/// Request for mock login.
/// </summary>
public record MockLoginRequest(string? DisplayName);
