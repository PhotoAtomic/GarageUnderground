using GarageUnderground.Models;

namespace GarageUnderground.Tests;

public class LiteDbInterventiRepositoryTests
{
    private static Intervento Nuovo(string targa, DateOnly data, decimal costo = 10m, bool pagato = false) => new()
    {
        Targa = targa,
        Data = data,
        Descrizione = "Test",
        Costo = costo,
        Pagato = pagato
    };

    [Fact]
    public async Task Create_NormalizzaTargaELeggePerTarga()
    {
        using var db = new TempDatabase();
        var repo = db.Interventi;

        await repo.CreateAsync(Nuovo("ab 123 cd", new DateOnly(2025, 12, 20)));

        var letti = await repo.GetByTargaAsync("AB123CD");
        Assert.Single(letti);
        Assert.Equal("AB123CD", letti[0].Targa);
    }

    [Fact]
    public async Task DateOnly_SopravviveAlRoundTrip()
    {
        using var db = new TempDatabase();
        var repo = db.Interventi;
        var data = new DateOnly(2024, 2, 29);

        var creato = await repo.CreateAsync(Nuovo("AA000AA", data));
        var riletto = await repo.GetByIdAsync(creato.Id);

        Assert.NotNull(riletto);
        Assert.Equal(data, riletto.Data);
    }

    [Fact]
    public async Task GetByTarga_OrdinatiPerDataDecrescente()
    {
        using var db = new TempDatabase();
        var repo = db.Interventi;

        await repo.CreateAsync(Nuovo("AA000AA", new DateOnly(2025, 1, 1)));
        await repo.CreateAsync(Nuovo("AA000AA", new DateOnly(2025, 6, 1)));
        await repo.CreateAsync(Nuovo("AA000AA", new DateOnly(2025, 3, 1)));

        var letti = await repo.GetByTargaAsync("AA000AA");

        Assert.Equal([new DateOnly(2025, 6, 1), new DateOnly(2025, 3, 1), new DateOnly(2025, 1, 1)], letti.Select(i => i.Data));
    }

    [Fact]
    public async Task Update_E_Delete()
    {
        using var db = new TempDatabase();
        var repo = db.Interventi;

        var creato = await repo.CreateAsync(Nuovo("AA000AA", new DateOnly(2025, 1, 1)));
        Assert.True(await repo.UpdateAsync(creato with { Pagato = true, Costo = 99m }));

        var aggiornato = await repo.GetByIdAsync(creato.Id);
        Assert.True(aggiornato!.Pagato);
        Assert.Equal(99m, aggiornato.Costo);

        Assert.True(await repo.DeleteAsync(creato.Id));
        Assert.Null(await repo.GetByIdAsync(creato.Id));
        Assert.False(await repo.DeleteAsync(creato.Id));
    }

    [Fact]
    public async Task Riepilogo_RaggruppaPerTargaConTotali()
    {
        using var db = new TempDatabase();
        var repo = db.Interventi;

        await repo.CreateAsync(Nuovo("AA000AA", new DateOnly(2025, 1, 1), 100m, pagato: true));
        await repo.CreateAsync(Nuovo("AA000AA", new DateOnly(2025, 5, 1), 50m, pagato: false));
        await repo.CreateAsync(Nuovo("BB111BB", new DateOnly(2025, 3, 1), 30m, pagato: false));

        var riepilogo = await repo.GetRiepilogoTargheAsync(10);

        Assert.Equal(["AA000AA", "BB111BB"], riepilogo.Select(r => r.Targa));
        var aa = riepilogo[0];
        Assert.Equal(2, aa.NumeroInterventi);
        Assert.Equal(new DateOnly(2025, 5, 1), aa.UltimaData);
        Assert.Equal(150m, aa.TotaleCosto);
        Assert.Equal(50m, aa.TotaleDaPagare);
    }
}
