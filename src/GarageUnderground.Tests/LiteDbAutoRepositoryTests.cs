using GarageUnderground.Models;

namespace GarageUnderground.Tests;

public class LiteDbAutoRepositoryTests
{
    [Fact]
    public async Task Create_NormalizzaTargaELeggePerTarga()
    {
        using var db = new TempDatabase();
        var repo = db.Auto;

        var creata = await repo.CreateAsync(new Auto { Targa = "ab 123 cd", Marca = "Fiat" });

        Assert.NotNull(creata);
        Assert.Equal("AB123CD", creata.Targa);

        var letta = await repo.GetByTargaAsync(" ab123cd ");
        Assert.NotNull(letta);
        Assert.Equal("Fiat", letta.Marca);
    }

    [Fact]
    public async Task Create_TargaDuplicata_RestituisceNull()
    {
        using var db = new TempDatabase();
        var repo = db.Auto;

        Assert.NotNull(await repo.CreateAsync(new Auto { Targa = "AB123CD" }));
        Assert.Null(await repo.CreateAsync(new Auto { Targa = "ab123cd" }));
    }

    [Fact]
    public async Task Update_NonPuoRubareTargaDiAltraScheda()
    {
        using var db = new TempDatabase();
        var repo = db.Auto;

        var prima = (await repo.CreateAsync(new Auto { Targa = "AA000AA" }))!;
        await repo.CreateAsync(new Auto { Targa = "BB111BB" });

        Assert.False(await repo.UpdateAsync(prima with { Targa = "BB111BB" }));
        Assert.True(await repo.UpdateAsync(prima with { Colore = "Rosso" }));
        Assert.Equal("Rosso", (await repo.GetByIdAsync(prima.Id))!.Colore);
    }

    [Fact]
    public async Task GetByTarga_Vuota_RestituisceNull()
    {
        using var db = new TempDatabase();
        Assert.Null(await db.Auto.GetByTargaAsync("  "));
    }

    [Fact]
    public async Task Delete()
    {
        using var db = new TempDatabase();
        var repo = db.Auto;
        var creata = (await repo.CreateAsync(new Auto { Targa = "AA000AA" }))!;

        Assert.True(await repo.DeleteAsync(creata.Id));
        Assert.Null(await repo.GetByTargaAsync("AA000AA"));
    }
}
