using LiteDB;
using LiteDB.Engine;

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
            ?? "Filename=/app/data/garageunderground.db;Connection=shared";

        // Ensure the directory exists
        EnsureDatabaseDirectoryExists(connectionString, services);

        services.AddSingleton<ILiteDatabase>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<LiteDatabase>>();

            try
            {
                logger.LogInformation("=== LiteDB Initialization with In-Memory Log ===");
                logger.LogInformation("Connection String: {ConnectionString}", connectionString);

                // Parse filename from connection string
                var filename = ExtractFilename(connectionString);
                var fullPath = Path.GetFullPath(filename);
                var directory = Path.GetDirectoryName(fullPath);

                logger.LogInformation("Database File: {Filename}", filename);
                logger.LogInformation("Full Path: {FullPath}", fullPath);
                logger.LogInformation("Directory: {Directory}", directory);

                // Check directory permissions
                if (directory != null && Directory.Exists(directory))
                {
                    var testFile = Path.Combine(directory, ".write-test");
                    try
                    {
                        File.WriteAllText(testFile, "test");
                        File.Delete(testFile);
                        logger.LogInformation("? Directory is writable");
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "? Directory is NOT writable!");
                    }
                }

                // Create custom engine settings
                // Open file streams with explicit permissions to work around Azure Files issues

                // Data file: open with full read/write permissions
                var dataStream = new FileStream(
                    fullPath,
                    FileMode.OpenOrCreate,
                    FileAccess.ReadWrite,
                    FileShare.ReadWrite,
                    bufferSize: 8192,
                    useAsync: false);

                // Log file: on /tmp to avoid Azure Files completely
                var logPath = Path.Combine("/tmp", $"{Path.GetFileNameWithoutExtension(fullPath)}-log.db");
                var logStream = new FileStream(
                    logPath,
                    FileMode.OpenOrCreate,
                    FileAccess.ReadWrite,
                    FileShare.ReadWrite,
                    bufferSize: 4096,
                    useAsync: false);

                var settings = new EngineSettings
                {
                    // Use explicit streams instead of Filename to control permissions
                    DataStream = dataStream,
                    LogStream = logStream,
                    // Use MemoryStream for temp operations (sorting, etc.)
                    TempStream = new MemoryStream()
                };

                logger.LogInformation("Opened data file with ReadWrite permissions: {DataPath}", fullPath);
                logger.LogInformation("Opened log file in /tmp: {LogPath}", logPath);
                logger.LogInformation("Using MemoryStream for temp operations");
                logger.LogInformation("=== Creating LiteDatabase instance ===");

                var engine = new LiteEngine(settings);
                var db = new LiteDatabase(engine);

                logger.LogInformation("? LiteDatabase instance created successfully with in-memory temp stream");

                return db;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "? Failed to create LiteDatabase instance");
                throw;
            }
        });

        services.AddScoped<IInterventiRepository, LiteDbInterventiRepository>();
        services.AddScoped<IUserRolesRepository, LiteDbUserRolesRepository>();
        services.AddScoped<IUserRegistrationRepository, LiteDbUserRegistrationRepository>();

        // Aggiungi notifier per tracking delle modifiche
        services.AddSingleton<DatabaseChangeNotifier>();
        services.AddSingleton<IDatabaseChangeNotifier>(sp => sp.GetRequiredService<DatabaseChangeNotifier>());

        // Aggiungi servizio di checkpoint periodico
        services.AddHostedService<LiteDbCheckpointService>(sp =>
        {
            var checkpointIntervalSeconds = configuration.GetValue<int?>("LiteDb:CheckpointIntervalSeconds") ?? 5;

            var service = new LiteDbCheckpointService(
                sp.GetRequiredService<ILiteDatabase>(),
                sp.GetRequiredService<ILogger<LiteDbCheckpointService>>(),
                sp.GetRequiredService<IHostApplicationLifetime>(),
                sp.GetRequiredService<DatabaseChangeNotifier>(),
                TimeSpan.FromSeconds(checkpointIntervalSeconds));

            // Collega il notifier al servizio
            sp.GetRequiredService<DatabaseChangeNotifier>().SetCheckpointService(service);

            return service;
        });

        return services;
    }

    /// <summary>
    /// Extracts filename from LiteDB connection string.
    /// </summary>
    private static string ExtractFilename(string connectionString)
    {
        var parts = connectionString.Split(';');
        foreach (var part in parts)
        {
            var trimmed = part.Trim();
            if (trimmed.StartsWith("Filename=", StringComparison.OrdinalIgnoreCase))
            {
                return trimmed["Filename=".Length..];
            }
        }
        return "garageunderground.db";
    }

    /// <summary>
    /// Ensures the directory for the database file exists.
    /// </summary>
    private static void EnsureDatabaseDirectoryExists(string connectionString, IServiceCollection services)
    {
        var serviceProvider = services.BuildServiceProvider();
        var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
        var logger = loggerFactory?.CreateLogger("LiteDB.Startup");

        var parts = connectionString.Split(';');
        foreach (var part in parts)
        {
            var trimmed = part.Trim();
            if (trimmed.StartsWith("Filename=", StringComparison.OrdinalIgnoreCase))
            {
                var filename = trimmed["Filename=".Length..];
                var directory = Path.GetDirectoryName(filename);

                logger?.LogInformation("Ensuring directory exists: {Directory}", directory ?? "(current directory)");

                if (!string.IsNullOrEmpty(directory))
                {
                    try
                    {
                        Directory.CreateDirectory(directory);
                        logger?.LogInformation("? Directory created/verified: {Directory}", directory);
                    }
                    catch (Exception ex)
                    {
                        logger?.LogError(ex, "? Failed to create directory: {Directory}", directory);
                        throw;
                    }
                }
                break;
            }
        }
    }
}
