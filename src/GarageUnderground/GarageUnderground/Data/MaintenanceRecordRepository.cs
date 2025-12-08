using GarageUnderground.Shared.Models;
using LiteDB;
using Microsoft.Extensions.Options;

namespace GarageUnderground.Data;

public sealed class MaintenanceRecordRepository : IDisposable
{
    private readonly ILiteCollection<MaintenanceRecord> records;
    private readonly LiteDatabase database;

    public MaintenanceRecordRepository(IOptions<LiteDbOptions> options, IWebHostEnvironment environment)
    {
        var configuredPath = options.Value.DatabasePath;
        var databasePath = string.IsNullOrWhiteSpace(configuredPath)
            ? Path.Combine(environment.ContentRootPath, "App_Data", "garage-underground.db")
            : configuredPath;

        if (!Path.IsPathRooted(databasePath))
        {
            databasePath = Path.Combine(environment.ContentRootPath, databasePath);
        }

        Directory.CreateDirectory(Path.GetDirectoryName(databasePath)!);

        database = new LiteDatabase(databasePath);
        records = database.GetCollection<MaintenanceRecord>("maintenance");
        records.EnsureIndex(record => record.LicensePlate);
        records.EnsureIndex(record => record.Date);
    }

    public IReadOnlyList<MaintenanceRecord> Search(string? licensePlate, string? query)
    {
        var filter = BuildFilter(licensePlate, query);
        var result = records.Find(filter)
            .OrderByDescending(record => record.Date)
            .ThenBy(record => record.LicensePlate)
            .ToList();

        return result;
    }

    public MaintenanceRecord? GetById(ObjectId id)
    {
        return records.FindById(id);
    }

    public MaintenanceRecord Upsert(MaintenanceRecord record)
    {
        Validate(record);

        if (record.Id == ObjectId.Empty)
        {
            record.Id = ObjectId.NewObjectId();
        }

        record.LicensePlate = record.LicensePlate.Trim().ToUpperInvariant();
        record.Description = record.Description.Trim();

        records.Upsert(record);
        return record;
    }

    public bool Delete(ObjectId id)
    {
        return records.Delete(id);
    }

    private static void Validate(MaintenanceRecord record)
    {
        if (string.IsNullOrWhiteSpace(record.LicensePlate))
        {
            throw new ArgumentException("License plate is required.", nameof(record));
        }

        if (record.Date == default)
        {
            throw new ArgumentException("Date is required.", nameof(record));
        }

        if (string.IsNullOrWhiteSpace(record.Description))
        {
            throw new ArgumentException("Description is required.", nameof(record));
        }

        if (record.Price < 0)
        {
            throw new ArgumentException("Price cannot be negative.", nameof(record));
        }
    }

    private static BsonExpression BuildFilter(string? licensePlate, string? query)
    {
        var filters = new List<BsonExpression>();

        if (!string.IsNullOrWhiteSpace(licensePlate))
        {
            var normalized = licensePlate.Trim().ToUpperInvariant().Replace("'", "''");
            filters.Add(BsonExpression.Create($"LIKE(UPPER($.{nameof(MaintenanceRecord.LicensePlate)}), '%{normalized}%')"));
        }

        if (!string.IsNullOrWhiteSpace(query))
        {
            var normalized = query.Trim().ToUpperInvariant().Replace("'", "''");
            filters.Add(BsonExpression.Create(
                $"LIKE(UPPER($.{nameof(MaintenanceRecord.Description)}), '%{normalized}%') OR LIKE(UPPER($.{nameof(MaintenanceRecord.LicensePlate)}), '%{normalized}%')"));
        }

        if (!filters.Any())
        {
            return BsonExpression.Create("true");
        }

        return filters.Count == 1 ? filters[0] : Query.And(filters.ToArray());
    }

    public void Dispose()
    {
        database.Dispose();
    }
}
