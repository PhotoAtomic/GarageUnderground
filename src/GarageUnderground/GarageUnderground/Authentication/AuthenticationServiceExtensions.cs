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
            options.AccessDeniedPath = "/";
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
            options.Events.OnRedirectToAccessDenied = context =>
            {
                context.Response.StatusCode = 403;
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
                        var logger = context.HttpContext.RequestServices.GetService<ILogger<ClaimsEnrichmentService>>();
                        
                        if (identity != null)
                        {
                            // Log ALL claims for debugging
                            logger?.LogInformation("=== All claims from Entra ID token ===");
                            foreach (var claim in identity.Claims)
                            {
                                logger?.LogInformation("Claim: {Type} = {Value}", claim.Type, claim.Value);
                            }
                            logger?.LogInformation("=== End of claims ===");

                            // Add auth_provider claim for tracking
                            identity.AddClaim(new Claim("auth_provider", MicrosoftScheme));

                            // Map Entra ID "roles" claim to standard ClaimTypes.Role
                            // Entra ID sends roles in the "roles" claim
                            var entraRoles = context.Principal?.FindAll("roles").ToList() ?? [];
                            logger?.LogInformation("Found {Count} 'roles' claims from Entra ID", entraRoles.Count);
                            
                            foreach (var roleClaim in entraRoles)
                            {
                                logger?.LogInformation("Adding Entra role to ClaimTypes.Role: {Role}", roleClaim.Value);
                                if (!identity.HasClaim(ClaimTypes.Role, roleClaim.Value))
                                {
                                    identity.AddClaim(new Claim(ClaimTypes.Role, roleClaim.Value));
                                }
                            }

                            // Log final roles
                            var allRoles = context.Principal?.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList() ?? [];
                            logger?.LogInformation("User authenticated via Microsoft. Final roles: {Roles}", 
                                string.Join(", ", allRoles));
                        }

                        // Now enrich with internal database roles
                        await EnrichTicketAsync(context);

                        // Check if user has canLogin role, redirect to access-denied if not
                        var hasLoginRole = context.Principal?.Claims
                            .Where(c => c.Type == ClaimTypes.Role)
                            .Any(c => c.Value.Equals("canLogin", StringComparison.OrdinalIgnoreCase)) == true;

                        if (!hasLoginRole)
                        {
                            logger?.LogWarning("User does not have canLogin role, redirecting to access-denied");
                            context.ReturnUri = "/access-denied";
                        }
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

                // After successful authentication, enrich claims and redirect
                options.Events.OnTicketReceived = async context =>
                {
                    context.ReturnUri = "/dashboard";
                    
                    // Add auth_provider claim
                    var identity = context.Principal?.Identity as ClaimsIdentity;
                    identity?.AddClaim(new Claim("auth_provider", "Google"));
                    
                    await EnrichTicketAsync(context);

                    // Check if user has canLogin role, redirect to access-denied if not
                    var hasLoginRole = context.Principal?.Claims
                        .Where(c => c.Type == ClaimTypes.Role)
                        .Any(c => c.Value.Equals("canLogin", StringComparison.OrdinalIgnoreCase)) == true;

                    if (!hasLoginRole)
                    {
                        var logger = context.HttpContext.RequestServices.GetService<ILogger<ClaimsEnrichmentService>>();
                        logger?.LogWarning("User does not have canLogin role, redirecting to access-denied");
                        context.ReturnUri = "/access-denied";
                    }
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

        // Add authorization services with case-insensitive role policies
        services.AddAuthorization(options =>
        {
            options.AddPolicy("CanAdmin", policy =>
                policy.RequireAssertion(context =>
                    context.User.Claims
                        .Where(c => c.Type == ClaimTypes.Role)
                        .Any(c => c.Value.Equals("canAdmin", StringComparison.OrdinalIgnoreCase))));
        });

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
    /// Enriches claims in the authentication ticket during OAuth callback (new login).
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

        // This is a new login, so register the user
        var enrichedPrincipal = await enrichmentService.EnrichClaimsAsync(context.Principal, isNewLogin: true);
        context.Principal = enrichedPrincipal;
    }

    /// <summary>
    /// Enriches claims during cookie validation (for subsequent requests, NOT a new login).
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
            // This is NOT a new login, just cookie validation - don't increment login count
            var enrichedPrincipal = await enrichmentService.EnrichClaimsAsync(context.Principal, isNewLogin: false);
            
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
