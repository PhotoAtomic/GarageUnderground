using LiteDB;
using LiteDB.Engine;

namespace GarageUnderground.Persistence;

/// <summary>
/// Estensioni per la configurazione della persistenza.
/// </summary>
public static class PersistenceServiceExtensions
{
    private const string DefaultConnectionString = "Filename=/app/data/garageunderground.db;Connection=shared";

    /// <summary>
    /// Aggiunge i servizi di persistenza al container DI.
    /// </summary>
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetValue<string>("LiteDb:ConnectionString") ?? DefaultConnectionString;
        var dataPath = Path.GetFullPath(ExtractFilename(connectionString));

        var directory = Path.GetDirectoryName(dataPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        services.AddSingleton<ILiteDatabase>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<LiteDatabase>>();

            // Il file dati è aperto con stream espliciti per controllare i permessi di condivisione
            // (necessario su Azure Files). Il log WAL va nella cartella temporanea del sistema:
            // /tmp nel container, %TEMP% in locale su Windows.
            var dataStream = new FileStream(
                dataPath,
                FileMode.OpenOrCreate,
                FileAccess.ReadWrite,
                FileShare.ReadWrite,
                bufferSize: 8192,
                useAsync: false);

            var logPath = Path.Combine(Path.GetTempPath(), $"{Path.GetFileNameWithoutExtension(dataPath)}-log.db");
            var logStream = new FileStream(
                logPath,
                FileMode.OpenOrCreate,
                FileAccess.ReadWrite,
                FileShare.ReadWrite,
                bufferSize: 4096,
                useAsync: false);

            var settings = new EngineSettings
            {
                DataStream = dataStream,
                LogStream = logStream,
                TempStream = new MemoryStream()
            };

            logger.LogInformation("LiteDB: dati in {DataPath}, log in {LogPath}", dataPath, logPath);

            return new LiteDatabase(new LiteEngine(settings));
        });

        services.AddScoped<IInterventiRepository, LiteDbInterventiRepository>();
        services.AddScoped<IAutoRepository, LiteDbAutoRepository>();
        services.AddScoped<IUserRolesRepository, LiteDbUserRolesRepository>();
        services.AddScoped<IUserRegistrationRepository, LiteDbUserRegistrationRepository>();

        services.AddSingleton<DatabaseChangeNotifier>();
        services.AddSingleton<IDatabaseChangeNotifier>(sp => sp.GetRequiredService<DatabaseChangeNotifier>());

        services.AddHostedService(sp =>
        {
            var checkpointIntervalSeconds = configuration.GetValue<int?>("LiteDb:CheckpointIntervalSeconds") ?? 5;

            var service = new LiteDbCheckpointService(
                sp.GetRequiredService<ILiteDatabase>(),
                sp.GetRequiredService<ILogger<LiteDbCheckpointService>>(),
                sp.GetRequiredService<IHostApplicationLifetime>(),
                sp.GetRequiredService<DatabaseChangeNotifier>(),
                TimeSpan.FromSeconds(checkpointIntervalSeconds));

            sp.GetRequiredService<DatabaseChangeNotifier>().SetCheckpointService(service);

            return service;
        });

        return services;
    }

    /// <summary>
    /// Estrae il nome file da una connection string LiteDB.
    /// </summary>
    private static string ExtractFilename(string connectionString)
    {
        foreach (var part in connectionString.Split(';'))
        {
            var trimmed = part.Trim();
            if (trimmed.StartsWith("Filename=", StringComparison.OrdinalIgnoreCase))
            {
                return trimmed["Filename=".Length..];
            }
        }

        return "garageunderground.db";
    }
}
