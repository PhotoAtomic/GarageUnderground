using GarageUnderground.Models;
using GarageUnderground.Persistence;

namespace GarageUnderground.Api;

/// <summary>
/// Endpoints API per l'anagrafica delle auto.
/// </summary>
public static class AutoEndpoints
{
    public static IEndpointRouteBuilder MapAutoEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/auto")
            .RequireAuthorization();

        group.MapGet("/targa/{targa}", GetByTargaAsync)
            .WithName("GetAutoByTarga")
            .WithDescription("Ottiene la scheda auto per targa");

        group.MapGet("/{id:guid}", GetByIdAsync)
            .WithName("GetAutoById");

        group.MapPost("/", CreateAsync)
            .WithName("CreateAuto")
            .WithDescription("Crea una scheda auto");

        group.MapPut("/{id:guid}", UpdateAsync)
            .WithName("UpdateAuto")
            .WithDescription("Aggiorna una scheda auto");

        group.MapDelete("/{id:guid}", DeleteAsync)
            .WithName("DeleteAuto");

        return endpoints;
    }

    private static async Task<IResult> GetByTargaAsync(
        string targa,
        IAutoRepository repository,
        CancellationToken cancellationToken)
    {
        var auto = await repository.GetByTargaAsync(targa, cancellationToken);
        return auto is null ? Results.NotFound() : Results.Ok(auto.ToDto());
    }

    private static async Task<IResult> GetByIdAsync(
        Guid id,
        IAutoRepository repository,
        CancellationToken cancellationToken)
    {
        var auto = await repository.GetByIdAsync(id, cancellationToken);
        return auto is null ? Results.NotFound() : Results.Ok(auto.ToDto());
    }

    private static async Task<IResult> CreateAsync(
        SalvaAutoDto request,
        IAutoRepository repository,
        CancellationToken cancellationToken)
    {
        var error = AutoValidator.Validate(request);
        if (error is not null)
        {
            return Results.BadRequest(error);
        }

        var created = await repository.CreateAsync(request.ToNewEntity(), cancellationToken);
        if (created is null)
        {
            return Results.Conflict("Esiste già una scheda per questa targa");
        }

        return Results.Created($"/api/auto/{created.Id}", created.ToDto());
    }

    private static async Task<IResult> UpdateAsync(
        Guid id,
        SalvaAutoDto request,
        IAutoRepository repository,
        CancellationToken cancellationToken)
    {
        var error = AutoValidator.Validate(request);
        if (error is not null)
        {
            return Results.BadRequest(error);
        }

        var existing = await repository.GetByIdAsync(id, cancellationToken);
        if (existing is null)
        {
            return Results.NotFound();
        }

        var updated = request.ApplyTo(existing);
        var success = await repository.UpdateAsync(updated, cancellationToken);

        return success
            ? Results.Ok(updated.ToDto())
            : Results.Conflict("Esiste già una scheda per questa targa");
    }

    private static async Task<IResult> DeleteAsync(
        Guid id,
        IAutoRepository repository,
        CancellationToken cancellationToken)
    {
        var success = await repository.DeleteAsync(id, cancellationToken);
        return success ? Results.NoContent() : Results.NotFound();
    }
}
