using GarageUnderground.Api;
using GarageUnderground.Authentication;
using GarageUnderground.Authentication.Client;
using GarageUnderground.Services.Client;
using GarageUnderground.Components;
using GarageUnderground.Persistence;
using GarageUnderground.Services;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Components.Authorization;

var builder = WebApplication.CreateBuilder(args);

// Configure forwarded headers EARLY for reverse proxy scenarios
// This is needed so that services can correctly determine the scheme (http vs https)
if (builder.Configuration.GetValue<bool>("ReverseProxy:Enabled"))
{
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | 
                                  ForwardedHeaders.XForwardedProto | 
                                  ForwardedHeaders.XForwardedHost;
        // Trust all proxies (for Azure Container Apps, App Service, etc.)
        options.KnownNetworks.Clear();
        options.KnownProxies.Clear();
    });
}

builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add authentication services
builder.Services.AddAppAuthentication(builder.Configuration);

// Add persistence services
builder.Services.AddPersistence(builder.Configuration);

// Add server-side services (usano direttamente il repository, non fanno chiamate HTTP)
builder.Services.AddScoped<IInterventiService, ServerInterventiService>();

// Add server-side authentication state provider
builder.Services.AddScoped<ApiAuthenticationStateProvider, ServerApiAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => 
    sp.GetRequiredService<ApiAuthenticationStateProvider>());

// HttpContextAccessor needed for ServerApiAuthenticationStateProvider
builder.Services.AddHttpContextAccessor();

// HttpClient for components that still need to call API endpoints (like AdminRoles)
// Configure it to call the local API
builder.Services.AddScoped(sp =>
{
    var httpContextAccessor = sp.GetRequiredService<IHttpContextAccessor>();
    var request = httpContextAccessor.HttpContext?.Request;
    
    var baseAddress = request != null 
        ? $"{request.Scheme}://{request.Host}"
        : "http://localhost:5000";
    
    return new HttpClient { BaseAddress = new Uri(baseAddress) };
});

var app = builder.Build();

// Apply forwarded headers middleware for reverse proxy (nginx, Azure App Service, Azure Container Apps, etc.)
// This MUST be called BEFORE any other middleware that depends on the request URL
if (builder.Configuration.GetValue<bool>("ReverseProxy:Enabled"))
{
    app.UseForwardedHeaders();
    
    // Temporary logging middleware to debug forwarded headers
    app.Use(async (context, next) =>
    {
        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
        logger.LogInformation(
            "Request: {Method} {Path}, Scheme: {Scheme}, Host: {Host}, X-Forwarded-Proto: {ForwardedProto}, X-Forwarded-Host: {ForwardedHost}",
            context.Request.Method,
            context.Request.Path,
            context.Request.Scheme,
            context.Request.Host,
            context.Request.Headers["X-Forwarded-Proto"].FirstOrDefault() ?? "none",
            context.Request.Headers["X-Forwarded-Host"].FirstOrDefault() ?? "none");
        
        await next();
    });
}

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

// UseHttpsRedirection should NOT be used when behind a reverse proxy with TLS termination
// The proxy handles HTTPS, the app receives HTTP
if (!builder.Configuration.GetValue<bool>("ReverseProxy:Enabled"))
{
    app.UseHttpsRedirection();
}

// Add authentication middleware
app.UseAppAuthentication();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Map authentication endpoints
app.MapAuthenticationEndpoints();

// Map API endpoints
app.MapInterventiEndpoints();
app.MapAdminRolesEndpoints();
app.MapDiagnosticEndpoints();

app.Run();
