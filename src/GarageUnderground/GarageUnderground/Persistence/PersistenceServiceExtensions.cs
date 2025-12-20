using LiteDB;

namespace GarageUnderground.Persistence;

/// <summary>
/// Estensioni per la configurazione della persistenza.
/// </summary>
public static class PersistenceServiceExtensions
{
    /// <summary>
    /// Aggiunge i servizi di persistenza al container DI.
    /// </summary>
    /// <param name="services">Collezione dei servizi.</param>
    /// <param name="configuration">Configurazione dell'applicazione.</param>
    /// <returns>La collezione dei servizi per il chaining.</returns>
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetValue<string>("LiteDb:ConnectionString") 
            ?? "Filename=garageunderground.db;Connection=shared";

        services.AddSingleton<ILiteDatabase>(_ => new LiteDatabase(connectionString));
        services.AddScoped<IInterventiRepository, LiteDbInterventiRepository>();
        services.AddScoped<IUserRolesRepository, LiteDbUserRolesRepository>();
        services.AddScoped<IUserRegistrationRepository, LiteDbUserRegistrationRepository>();

        return services;
    }
}
