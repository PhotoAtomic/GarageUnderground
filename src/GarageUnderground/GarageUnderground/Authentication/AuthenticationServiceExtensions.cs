using GarageUnderground.Client.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.MicrosoftAccount;
using Microsoft.AspNetCore.Components.Authorization;

namespace GarageUnderground.Authentication;

/// <summary>
/// Extension methods for configuring authentication services.
/// </summary>
public static class AuthenticationServiceExtensions
{
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

        var authBuilder = services.AddAuthentication(options =>
        {
            options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        })
        .AddCookie(options =>
        {
            options.LoginPath = "/login";
            options.LogoutPath = "/logout";
            options.ExpireTimeSpan = TimeSpan.FromDays(30);
            options.SlidingExpiration = true;
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        });

        var availableProviders = new List<AuthProviderInfo>();

        // Configure Microsoft if available
        if (authConfig.Microsoft?.IsConfigured == true)
        {
            authBuilder.AddMicrosoftAccount(options =>
            {
                options.ClientId = authConfig.Microsoft.ClientId!;
                options.ClientSecret = authConfig.Microsoft.ClientSecret!;
                options.SaveTokens = true;
            });

            availableProviders.Add(new AuthProviderInfo(
                MicrosoftAccountDefaults.AuthenticationScheme,
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
