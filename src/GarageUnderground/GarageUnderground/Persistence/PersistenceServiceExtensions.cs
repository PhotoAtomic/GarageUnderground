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
            ?? "Filename=data/garageunderground.db;Connection=shared";

        // Ensure the directory exists
        EnsureDatabaseDirectoryExists(connectionString);

        services.AddSingleton<ILiteDatabase>(_ => new LiteDatabase(connectionString));
        services.AddScoped<IInterventiRepository, LiteDbInterventiRepository>();
        services.AddScoped<IUserRolesRepository, LiteDbUserRolesRepository>();
        services.AddScoped<IUserRegistrationRepository, LiteDbUserRegistrationRepository>();

        return services;
    }

    /// <summary>
    /// Ensures the directory for the database file exists.
    /// </summary>
    private static void EnsureDatabaseDirectoryExists(string connectionString)
    {
        // Parse the filename from the connection string
        var parts = connectionString.Split(';');
        foreach (var part in parts)
        {
            var trimmed = part.Trim();
            if (trimmed.StartsWith("Filename=", StringComparison.OrdinalIgnoreCase))
            {
                var filename = trimmed["Filename=".Length..];
                var directory = Path.GetDirectoryName(filename);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                break;
            }
        }
    }
}
