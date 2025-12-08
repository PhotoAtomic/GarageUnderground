using LiteDB;

namespace GarageUnderground.Shared.Models;

public class MaintenanceRecord
{
    [BsonId]
    public ObjectId Id { get; set; }

    public string LicensePlate { get; set; } = string.Empty;

    public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);

    public string Description { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public bool IsPaid { get; set; }
}
