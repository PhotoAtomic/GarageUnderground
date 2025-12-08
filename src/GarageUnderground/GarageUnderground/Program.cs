using GarageUnderground.Client.Pages;
using GarageUnderground.Components;
using GarageUnderground.Data;
using GarageUnderground.Shared.Json;
using GarageUnderground.Shared.Models;
using LiteDB;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new ObjectIdJsonConverter());
});
builder.Services.Configure<LiteDbOptions>(builder.Configuration.GetSection(LiteDbOptions.SectionName));
builder.Services.AddSingleton<MaintenanceRecordRepository>();

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

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(GarageUnderground.Client._Imports).Assembly);

var maintenanceApi = app.MapGroup("/api/maintenance");

maintenanceApi.MapGet("/", (string? licensePlate, string? query, MaintenanceRecordRepository repository) =>
{
    var items = repository.Search(licensePlate, query);
    return Results.Ok(items);
});

maintenanceApi.MapGet("/{id}", (string id, MaintenanceRecordRepository repository) =>
{
    if (!TryParseObjectId(id, out var objectId))
    {
        return Results.BadRequest("Invalid record id.");
    }

    var record = repository.GetById(objectId);
    return record is null ? Results.NotFound() : Results.Ok(record);
});

maintenanceApi.MapPost("/", (MaintenanceRecord record, MaintenanceRecordRepository repository) =>
{
    var saved = repository.Upsert(record);
    return Results.Created($"/api/maintenance/{saved.Id}", saved);
});

maintenanceApi.MapPut("/{id}", (string id, MaintenanceRecord record, MaintenanceRecordRepository repository) =>
{
    if (!TryParseObjectId(id, out var objectId))
    {
        return Results.BadRequest("Invalid record id.");
    }

    record.Id = objectId;
    var saved = repository.Upsert(record);
    return Results.Ok(saved);
});

maintenanceApi.MapDelete("/{id}", (string id, MaintenanceRecordRepository repository) =>
{
    if (!TryParseObjectId(id, out var objectId))
    {
        return Results.BadRequest("Invalid record id.");
    }

    var deleted = repository.Delete(objectId);
    return deleted ? Results.NoContent() : Results.NotFound();
});

app.Run();

static bool TryParseObjectId(string value, out ObjectId objectId)
{
    try
    {
        objectId = new ObjectId(value);
        return true;
    }
    catch
    {
        objectId = ObjectId.Empty;
        return false;
    }
}
