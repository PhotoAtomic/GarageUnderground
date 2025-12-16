using GarageUnderground.Client.Authentication;
using GarageUnderground.Client.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Configure HttpClient for API calls
builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
});

// Add authentication services
builder.Services.AddScoped<ApiAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp =>
    sp.GetRequiredService<ApiAuthenticationStateProvider>());
builder.Services.AddAuthorizationCore();

// Add application services
builder.Services.AddScoped<IInterventiService, InterventiService>();

await builder.Build().RunAsync();
