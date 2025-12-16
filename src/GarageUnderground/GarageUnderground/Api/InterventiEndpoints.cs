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

        return endpoints;
    }

    private static async Task<IResult> GetByTargaAsync(
        string targa,
        IInterventiRepository repository,
        CancellationToken cancellationToken)
    {
        var interventi = await repository.GetByTargaAsync(targa, cancellationToken);
        var response = interventi.Select(ToResponse).ToList();
        return Results.Ok(response);
    }

    private static async Task<IResult> GetByIdAsync(
        Guid id,
        IInterventiRepository repository,
        CancellationToken cancellationToken)
    {
        var intervento = await repository.GetByIdAsync(id, cancellationToken);
        
        if (intervento is null)
        {
            return Results.NotFound();
        }

        return Results.Ok(ToResponse(intervento));
    }

    private static async Task<IResult> CreateAsync(
        NuovoInterventoRequest request,
        IInterventiRepository repository,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Targa))
        {
            return Results.BadRequest("La targa è obbligatoria");
        }

        if (string.IsNullOrWhiteSpace(request.Descrizione))
        {
            return Results.BadRequest("La descrizione è obbligatoria");
        }

        if (request.Costo < 0)
        {
            return Results.BadRequest("Il costo non può essere negativo");
        }

        var intervento = new Intervento
        {
            Targa = request.Targa,
            Data = request.Data,
            Descrizione = request.Descrizione,
            Costo = request.Costo,
            Pagato = request.Pagato
        };

        var created = await repository.CreateAsync(intervento, cancellationToken);
        return Results.Created($"/api/interventi/{created.Id}", ToResponse(created));
    }

    private static async Task<IResult> UpdateAsync(
        Guid id,
        NuovoInterventoRequest request,
        IInterventiRepository repository,
        CancellationToken cancellationToken)
    {
        var existing = await repository.GetByIdAsync(id, cancellationToken);
        
        if (existing is null)
        {
            return Results.NotFound();
        }

        var updated = existing with
        {
            Targa = request.Targa,
            Data = request.Data,
            Descrizione = request.Descrizione,
            Costo = request.Costo,
            Pagato = request.Pagato
        };

        var success = await repository.UpdateAsync(updated, cancellationToken);
        
        if (!success)
        {
            return Results.Problem("Errore durante l'aggiornamento");
        }

        return Results.Ok(ToResponse(updated));
    }

    private static async Task<IResult> DeleteAsync(
        Guid id,
        IInterventiRepository repository,
        CancellationToken cancellationToken)
    {
        var success = await repository.DeleteAsync(id, cancellationToken);
        
        if (!success)
        {
            return Results.NotFound();
        }

        return Results.NoContent();
    }

    private static InterventoResponse ToResponse(Intervento intervento) => new()
    {
        Id = intervento.Id,
        Targa = intervento.Targa,
        Data = intervento.Data,
        Descrizione = intervento.Descrizione,
        Costo = intervento.Costo,
        Pagato = intervento.Pagato,
        CreatedAt = intervento.CreatedAt
    };
}
