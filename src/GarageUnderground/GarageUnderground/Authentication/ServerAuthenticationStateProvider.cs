using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace GarageUnderground.Authentication;

/// <summary>
/// Server-side authentication state provider using HttpContext.
/// </summary>
public sealed class ServerAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly IHttpContextAccessor httpContextAccessor;

    public ServerAuthenticationStateProvider(IHttpContextAccessor httpContextAccessor)
    {
        this.httpContextAccessor = httpContextAccessor;
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var user = httpContextAccessor.HttpContext?.User ?? new ClaimsPrincipal(new ClaimsIdentity());
        return Task.FromResult(new AuthenticationState(user));
    }
}
