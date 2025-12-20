using GarageUnderground.Client.Authentication;
using GarageUnderground.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using System.Security.Claims;

namespace GarageUnderground.Authentication;

/// <summary>
/// Extension methods for configuring authentication services.
/// </summary>
public static class AuthenticationServiceExtensions
{
    /// <summary>
    /// Authentication scheme name for Microsoft Entra ID.
    /// </summary>
    public const string MicrosoftScheme = "Microsoft";

    /// <summary>
    /// Adds authentication services based on configuration.
    /// </summary>
    public static IServiceCollection AddAppAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var authConfig = configuration
            .GetSection(AuthenticationConfiguration.SectionName)
            .Get<AuthenticationConfiguration>() ?? new AuthenticationConfiguration();

        services.Configure<AuthenticationConfiguration>(
            configuration.GetSection(AuthenticationConfiguration.SectionName));

        // Register claims enrichment service
        services.AddScoped<IClaimsEnrichmentService, ClaimsEnrichmentService>();

        var authBuilder = services.AddAuthentication(options =>
        {
            options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        })
        .AddCookie(options =>
        {
            options.LoginPath = "/";
            options.LogoutPath = "/";
            options.ExpireTimeSpan = TimeSpan.FromDays(30);
            options.SlidingExpiration = true;
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
            options.Events.OnRedirectToLogin = context =>
            {
                context.Response.StatusCode = 401;
                return Task.CompletedTask;
            };
            // Enrich claims when validating the cookie principal
            options.Events.OnValidatePrincipal = async context =>
            {
                await EnrichPrincipalAsync(context);
            };
        });

        var availableProviders = new List<AuthProviderInfo>();

        // Configure Microsoft Entra ID using OpenID Connect (to get App Roles)
        if (authConfig.Microsoft?.IsConfigured == true)
        {
            var tenantId = string.IsNullOrWhiteSpace(authConfig.Microsoft.TenantId)
                ? "common"
                : authConfig.Microsoft.TenantId;

            authBuilder.AddOpenIdConnect(MicrosoftScheme, "Microsoft", options =>
            {
                options.Authority = $"https://login.microsoftonline.com/{tenantId}/v2.0";
                options.ClientId = authConfig.Microsoft.ClientId!;
                options.ClientSecret = authConfig.Microsoft.ClientSecret!;
                options.ResponseType = OpenIdConnectResponseType.Code;
                options.SaveTokens = true;
                options.CallbackPath = "/signin-microsoft";
                options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;

                // Request scopes
                options.Scope.Clear();
                options.Scope.Add("openid");
                options.Scope.Add("profile");
                options.Scope.Add("email");

                // Map claims correctly
                options.TokenValidationParameters.NameClaimType = "name";
                options.TokenValidationParameters.RoleClaimType = "roles"; // Entra ID uses "roles" for App Roles

                // Get claims from ID token
                options.GetClaimsFromUserInfoEndpoint = true;

                options.Events = new OpenIdConnectEvents
                {
                    OnTicketReceived = async context =>
                    {
                        context.ReturnUri = "/dashboard";

                        var identity = context.Principal?.Identity as ClaimsIdentity;
                        if (identity != null)
                        {
                            // Add auth_provider claim for tracking
                            identity.AddClaim(new Claim("auth_provider", MicrosoftScheme));

                            // Map Entra ID "roles" claim to standard ClaimTypes.Role
                            var entraRoles = context.Principal?.FindAll("roles").ToList() ?? [];
                            foreach (var roleClaim in entraRoles)
                            {
                                if (!identity.HasClaim(ClaimTypes.Role, roleClaim.Value))
                                {
                                    identity.AddClaim(new Claim(ClaimTypes.Role, roleClaim.Value));
                                }
                            }

                            // Log roles for debugging
                            var allRoles = context.Principal?.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList() ?? [];
                            var logger = context.HttpContext.RequestServices.GetService<ILogger<ClaimsEnrichmentService>>();
                            logger?.LogInformation("User authenticated via Microsoft. Entra roles: {Roles}", 
                                string.Join(", ", allRoles));
                        }

                        // Now enrich with internal database roles
                        await EnrichTicketAsync(context);
                    }
                };
            });

            availableProviders.Add(new AuthProviderInfo(
                MicrosoftScheme,
                "Microsoft",
                "microsoft-icon"));
        }

        // Configure Google if available
        if (authConfig.Google?.IsConfigured == true)
        {
            authBuilder.AddGoogle(options =>
            {
                options.ClientId = authConfig.Google.ClientId!;
                options.ClientSecret = authConfig.Google.ClientSecret!;
                options.SaveTokens = true;
                options.CallbackPath = "/signin-google";

                // Request scopes to get user profile info
                options.Scope.Add("openid");
                options.Scope.Add("profile");
                options.Scope.Add("email");

                // After successful authentication, enrich claims and redirect to dashboard
                options.Events.OnTicketReceived = async context =>
                {
                    context.ReturnUri = "/dashboard";
                    
                    // Add auth_provider claim
                    var identity = context.Principal?.Identity as ClaimsIdentity;
                    identity?.AddClaim(new Claim("auth_provider", "Google"));
                    
                    await EnrichTicketAsync(context);
                };
            });

            availableProviders.Add(new AuthProviderInfo(
                GoogleDefaults.AuthenticationScheme,
                "Google",
                "google-icon"));
        }

        // If no providers configured, use mock authentication
        var useMockAuth = !authConfig.HasConfiguredProviders;
        if (useMockAuth)
        {
            authBuilder.AddScheme<AuthenticationSchemeOptions, MockAuthenticationHandler>(
                MockAuthenticationHandler.SchemeName,
                MockAuthenticationHandler.DisplayName,
                _ => { });

            availableProviders.Add(new AuthProviderInfo(
                MockAuthenticationHandler.SchemeName,
                MockAuthenticationHandler.DisplayName,
                "dev-icon"));
        }

        services.AddSingleton<IAuthenticationProviderService>(
            new AuthenticationProviderService(availableProviders, useMockAuth));

        // Add authorization services (required for UseAuthorization middleware)
        services.AddAuthorization();

        // Add server-side AuthenticationStateProvider for Blazor SSR
        services.AddHttpContextAccessor();
        services.AddScoped<AuthenticationStateProvider, ServerAuthenticationStateProvider>();
        services.AddCascadingAuthenticationState();

        // Register ApiAuthenticationStateProvider for server-side pre-rendering
        // This uses a server-side implementation that reads from HttpContext
        services.AddScoped<ApiAuthenticationStateProvider, ServerApiAuthenticationStateProvider>();

        return services;
    }

    /// <summary>
    /// Enriches claims in the authentication ticket during OAuth callback.
    /// </summary>
    private static async Task EnrichTicketAsync(TicketReceivedContext context)
    {
        if (context.Principal == null)
        {
            return;
        }

        var enrichmentService = context.HttpContext.RequestServices
            .GetService<IClaimsEnrichmentService>();

        if (enrichmentService == null)
        {
            return;
        }

        var enrichedPrincipal = await enrichmentService.EnrichClaimsAsync(context.Principal);
        context.Principal = enrichedPrincipal;
    }

    /// <summary>
    /// Enriches claims during cookie validation (for subsequent requests).
    /// </summary>
    private static async Task EnrichPrincipalAsync(CookieValidatePrincipalContext context)
    {
        if (context.Principal == null)
        {
            return;
        }

        // Check if already enriched in this request to avoid repeated DB calls
        var alreadyEnriched = context.Principal.Claims
            .Any(c => c.Type == ClaimsEnrichmentService.InternalRoleClaimType);

        if (alreadyEnriched)
        {
            return;
        }

        var logger = context.HttpContext.RequestServices.GetService<ILogger<ClaimsEnrichmentService>>();
        
        var enrichmentService = context.HttpContext.RequestServices
            .GetService<IClaimsEnrichmentService>();

        if (enrichmentService == null)
        {
            logger?.LogWarning("ClaimsEnrichmentService not available during cookie validation");
            return;
        }

        try
        {
            var enrichedPrincipal = await enrichmentService.EnrichClaimsAsync(context.Principal);
            
            // Log the roles after enrichment
            var roles = enrichedPrincipal.Claims
                .Where(c => c.Type == ClaimTypes.Role)
                .Select(c => c.Value)
                .ToArray();
            
            logger?.LogInformation("After enrichment, user has {RoleCount} roles: {Roles}", 
                roles.Length, 
                string.Join(", ", roles));
            
            context.ReplacePrincipal(enrichedPrincipal);
            context.ShouldRenew = true;
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error enriching claims during cookie validation");
        }
    }

    /// <summary>
    /// Adds authentication middleware to the pipeline.
    /// </summary>
    public static IApplicationBuilder UseAppAuthentication(this IApplicationBuilder app)
    {
        app.UseAuthentication();
        app.UseAuthorization();
        return app;
    }
}

/// <summary>
/// Implementation of authentication provider service.
/// </summary>
internal sealed class AuthenticationProviderService : IAuthenticationProviderService
{
    private readonly IReadOnlyList<AuthProviderInfo> providers;

    public AuthenticationProviderService(IReadOnlyList<AuthProviderInfo> providers, bool isMockActive)
    {
        this.providers = providers;
        IsMockAuthenticationActive = isMockActive;
    }

    public IReadOnlyList<AuthProviderInfo> GetAvailableProviders() => providers;

    public bool IsMockAuthenticationActive { get; }
}
