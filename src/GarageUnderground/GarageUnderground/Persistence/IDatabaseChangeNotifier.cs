namespace GarageUnderground.Persistence;

/// <summary>
/// Interfaccia per notificare che ci sono modifiche pending nel database.
/// </summary>
public interface IDatabaseChangeNotifier
{
    /// <summary>
    /// Notifica che ci sono state modifiche al database che richiedono un checkpoint.
    /// </summary>
    void NotifyChange();
}

/// <summary>
/// Implementazione che notifica il servizio di checkpoint.
/// </summary>
public class DatabaseChangeNotifier : IDatabaseChangeNotifier
{
    private LiteDbCheckpointService? checkpointService;

    public void SetCheckpointService(LiteDbCheckpointService service)
    {
        checkpointService = service;
    }

    public void NotifyChange()
    {
        checkpointService?.MarkDirty();
    }
}
