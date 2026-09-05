using GarageUnderground.Models;
using GarageUnderground.Persistence;

namespace GarageUnderground.Api;

/// <summary>
/// Endpoints API per la gestione degli interventi.
/// </summary>
public static class InterventiEndpoints
{
    /// <summary>
    /// Mappa gli endpoints per gli interventi.
    /// </summary>
    public static IEndpointRouteBuilder MapInterventiEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/interventi")
            .RequireAuthorization();

        group.MapGet("/targa/{targa}", GetByTargaAsync)
            .WithName("GetInterventiByTarga")
            .WithDescription("Ottiene tutti gli interventi per una specifica targa");

        group.MapGet("/{id:guid}", GetByIdAsync)
            .WithName("GetInterventoById")
            .WithDescription("Ottiene un intervento per ID");

        group.MapPost("/", CreateAsync)
            .WithName("CreateIntervento")
            .WithDescription("Crea un nuovo intervento");

        group.MapPut("/{id:guid}", UpdateAsync)
            .WithName("UpdateIntervento")
            .WithDescription("Aggiorna un intervento esistente");

        group.MapDelete("/{id:guid}", DeleteAsync)
            .WithName("DeleteIntervento")
            .WithDescription("Elimina un intervento");

        group.MapGet("/targhe", GetRiepilogoTargheAsync)
            .WithName("GetRiepilogoTarghe")
            .WithDescription("Riepilogo delle targhe in archivio, dalla più recente");

        group.MapPatch("/{id:guid}/pagato", SetPagatoAsync)
            .WithName("SetInterventoPagato")
            .WithDescription("Cambia lo stato di pagamento di un intervento");

        group.MapGet("/targa/{targa}/csv", ExportCsvAsync)
            .WithName("ExportInterventiCsv")
            .WithDescription("Esporta in CSV gli interventi di una targa");

        return endpoints;
    }

    private static async Task<IResult> GetRiepilogoTargheAsync(
        IInterventiRepository repository,
        CancellationToken cancellationToken,
        int limit = 50)
    {
        var riepilogo = await repository.GetRiepilogoTargheAsync(Math.Clamp(limit, 1, 500), cancellationToken);
        return Results.Ok(riepilogo);
    }

    private static async Task<IResult> SetPagatoAsync(
        Guid id,
        PagatoRequest request,
        IInterventiRepository repository,
        CancellationToken cancellationToken)
    {
        var existing = await repository.GetByIdAsync(id, cancellationToken);
        if (existing is null)
        {
            return Results.NotFound();
        }

        var updated = existing with { Pagato = request.Pagato };
        var success = await repository.UpdateAsync(updated, cancellationToken);

        return success
            ? Results.Ok(updated.ToDto())
            : Results.Problem("Errore durante l'aggiornamento");
    }

    private static async Task<IResult> ExportCsvAsync(
        string targa,
        IInterventiRepository repository,
        CancellationToken cancellationToken)
    {
        var interventi = await repository.GetByTargaAsync(targa, cancellationToken);
        var csv = InterventiCsv.Build(interventi);
        var fileName = $"interventi-{TargaNormalizer.Normalize(targa)}.csv";

        // BOM UTF-8 così Excel riconosce gli accenti
        var bytes = System.Text.Encoding.UTF8.GetPreamble()
            .Concat(System.Text.Encoding.UTF8.GetBytes(csv))
            .ToArray();

        return Results.File(bytes, "text/csv; charset=utf-8", fileName);
    }

    private static async Task<IResult> GetByTargaAsync(
        string targa,
        IInterventiRepository repository,
        CancellationToken cancellationToken)
    {
        var interventi = await repository.GetByTargaAsync(targa, cancellationToken);
        return Results.Ok(interventi.Select(i => i.ToDto()).ToList());
    }

    private static async Task<IResult> GetByIdAsync(
        Guid id,
        IInterventiRepository repository,
        CancellationToken cancellationToken)
    {
        var intervento = await repository.GetByIdAsync(id, cancellationToken);
        return intervento is null ? Results.NotFound() : Results.Ok(intervento.ToDto());
    }

    private static async Task<IResult> CreateAsync(
        NuovoInterventoDto request,
        IInterventiRepository repository,
        CancellationToken cancellationToken)
    {
        var error = InterventoValidator.Validate(request);
        if (error is not null)
        {
            return Results.BadRequest(error);
        }

        var created = await repository.CreateAsync(request.ToNewEntity(), cancellationToken);
        return Results.Created($"/api/interventi/{created.Id}", created.ToDto());
    }

    private static async Task<IResult> UpdateAsync(
        Guid id,
        NuovoInterventoDto request,
        IInterventiRepository repository,
        CancellationToken cancellationToken)
    {
        var error = InterventoValidator.Validate(request);
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
            : Results.Problem("Errore durante l'aggiornamento");
    }

    private static async Task<IResult> DeleteAsync(
        Guid id,
        IInterventiRepository repository,
        CancellationToken cancellationToken)
    {
        var success = await repository.DeleteAsync(id, cancellationToken);
        return success ? Results.NoContent() : Results.NotFound();
    }
}
