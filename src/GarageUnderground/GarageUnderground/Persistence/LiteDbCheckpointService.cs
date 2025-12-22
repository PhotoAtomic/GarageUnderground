using LiteDB;

namespace GarageUnderground.Persistence;

/// <summary>
/// Servizio di background che esegue checkpoint periodici del database LiteDB
/// e gestisce il flush finale durante lo shutdown dell'applicazione.
/// </summary>
public class LiteDbCheckpointService : BackgroundService
{
    private readonly ILiteDatabase database;
    private readonly ILogger<LiteDbCheckpointService> logger;
    private readonly IHostApplicationLifetime lifetime;
    private readonly DatabaseChangeNotifier notifier;
    private readonly TimeSpan checkpointInterval;
    private long lastCheckpointTime;
    private bool hasPendingChanges;

    public LiteDbCheckpointService(
        ILiteDatabase database,
        ILogger<LiteDbCheckpointService> logger,
        IHostApplicationLifetime lifetime,
        DatabaseChangeNotifier notifier,
        TimeSpan checkpointInterval)
    {
        this.database = database;
        this.logger = logger;
        this.lifetime = lifetime;
        this.notifier = notifier;
        this.checkpointInterval = checkpointInterval;
        this.lastCheckpointTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }

    /// <summary>
    /// Marca che ci sono modifiche pending da persistere.
    /// Chiamato automaticamente dai repository dopo ogni write operation.
    /// </summary>
    public void MarkDirty()
    {
        hasPendingChanges = true;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("LiteDB Checkpoint Service started (interval: {Interval}s)", checkpointInterval.TotalSeconds);

        // Registra handler per lo shutdown
        lifetime.ApplicationStopping.Register(OnApplicationStopping);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(checkpointInterval, stoppingToken);

                // Esegue checkpoint solo se ci sono modifiche pending
                if (hasPendingChanges)
                {
                    try
                    {
                        var startTime = DateTimeOffset.UtcNow;

                        // Esegue checkpoint: scrive tutte le modifiche pending dal WAL al database
                        database.Checkpoint();

                        var duration = DateTimeOffset.UtcNow - startTime;
                        lastCheckpointTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                        hasPendingChanges = false;

                        logger.LogInformation("Database checkpoint completed in {Duration}ms", duration.TotalMilliseconds);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error during database checkpoint");
                    }
                }
                else
                {
                    logger.LogDebug("Skipping checkpoint - no pending changes");
                }
            }
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("LiteDB Checkpoint Service stopping");
        }
    }

    private void OnApplicationStopping()
    {
        logger.LogWarning("Application stopping - performing CRITICAL final database checkpoint");

        try
        {
            // Checkpoint finale: persiste tutte le transazioni pending
            // Questo è CRITICO per evitare perdita di dati durante shutdown
            database.Checkpoint();
            logger.LogWarning("? CRITICAL final database checkpoint completed successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "? CRITICAL ERROR during final database checkpoint - DATA MAY BE LOST!");
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogWarning("LiteDB Checkpoint Service StopAsync called");

        // Esegue un ultimo checkpoint prima di fermarsi
        try
        {
            database.Checkpoint();
            logger.LogWarning("? Final checkpoint on service stop completed");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "? ERROR during final checkpoint on service stop");
        }

        await base.StopAsync(cancellationToken);
    }
}
