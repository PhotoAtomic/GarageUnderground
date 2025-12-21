using GarageUnderground.Models;
using LiteDB;

namespace GarageUnderground.Persistence;

/// <summary>
/// LiteDB implementation of user registration repository.
/// </summary>
public sealed class LiteDbUserRegistrationRepository : IUserRegistrationRepository
{
    private const string CollectionName = "user_registrations";
    private readonly ILiteDatabase database;

    public LiteDbUserRegistrationRepository(ILiteDatabase database)
    {
        this.database = database ?? throw new ArgumentNullException(nameof(database));

        var collection = this.database.GetCollection<UserRegistration>(CollectionName);
        collection.EnsureIndex(x => x.Email, unique: true);
    }

    public Task<UserRegistration?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return Task.FromResult<UserRegistration?>(null);
        }

        var normalizedEmail = NormalizeEmail(email);
        var collection = database.GetCollection<UserRegistration>(CollectionName);

        var registration = collection.FindOne(x => x.Email == normalizedEmail);
        return Task.FromResult(registration);
    }

    public Task<IReadOnlyList<UserRegistration>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var collection = database.GetCollection<UserRegistration>(CollectionName);
        var registrations = collection.FindAll()
            .OrderByDescending(x => x.LastLoginAt)
            .ToList();

        return Task.FromResult<IReadOnlyList<UserRegistration>>(registrations);
    }

    public Task<UserRegistration> RegisterOrUpdateAsync(
        string email,
        string? displayName,
        string? provider,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = NormalizeEmail(email);
        var collection = database.GetCollection<UserRegistration>(CollectionName);

        var existing = collection.FindOne(x => x.Email == normalizedEmail);

        if (existing != null)
        {
            var updated = existing with
            {
                DisplayName = displayName ?? existing.DisplayName,
                Provider = provider ?? existing.Provider,
                LastLoginAt = DateTimeOffset.UtcNow,
                LoginCount = existing.LoginCount + 1
            };
            collection.Update(updated);
            return Task.FromResult(updated);
        }

        var registration = new UserRegistration
        {
            Id = Guid.NewGuid(),
            Email = normalizedEmail,
            DisplayName = displayName,
            Provider = provider,
            FirstLoginAt = DateTimeOffset.UtcNow,
            LastLoginAt = DateTimeOffset.UtcNow,
            LoginCount = 1
        };

        collection.Insert(registration);
        return Task.FromResult(registration);
    }

    public Task<IReadOnlyList<UserRegistration>> SearchAsync(
        string? searchTerm,
        CancellationToken cancellationToken = default)
    {
        var collection = database.GetCollection<UserRegistration>(CollectionName);

        IEnumerable<UserRegistration> results;

        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            results = collection.FindAll();
        }
        else
        {
            var term = searchTerm.ToLowerInvariant();
            results = collection.FindAll()
                .Where(x => 
                    x.Email.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    (x.DisplayName?.Contains(term, StringComparison.OrdinalIgnoreCase) == true));
        }

        return Task.FromResult<IReadOnlyList<UserRegistration>>(
            results.OrderByDescending(x => x.LastLoginAt).ToList());
    }

    public Task<bool> DeleteAsync(string email, CancellationToken cancellationToken = default)
    {
        var collection = database.GetCollection<UserRegistration>(CollectionName);
        var normalizedEmail = NormalizeEmail(email);
        var deleted = collection.DeleteMany(x => x.Email == normalizedEmail);
        return Task.FromResult(deleted > 0);
    }

    private static string NormalizeEmail(string email)
    {
        return email.Trim().ToLowerInvariant();
    }
}
