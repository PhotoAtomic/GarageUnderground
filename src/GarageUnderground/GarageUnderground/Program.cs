using GarageUnderground.Api;
using GarageUnderground.Authentication;
using GarageUnderground.Client.Services;
using GarageUnderground.Components;
using GarageUnderground.Persistence;
using GarageUnderground.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();

// Add authentication services
builder.Services.AddAppAuthentication(builder.Configuration);

// Add persistence services
builder.Services.AddPersistence(builder.Configuration);

// Add server-side implementation of client services (for SSR pre-rendering)
builder.Services.AddScoped<IInterventiService, ServerInterventiService>();

var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

// Add authentication middleware
app.UseAppAuthentication();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(GarageUnderground.Client._Imports).Assembly);

// Map authentication endpoints
app.MapAuthenticationEndpoints();

// Map API endpoints
app.MapInterventiEndpoints();

app.Run();
