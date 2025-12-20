using GarageUnderground.Models;
using LiteDB;

namespace GarageUnderground.Persistence;

/// <summary>
/// LiteDB implementation of user roles repository.
/// </summary>
public sealed class LiteDbUserRolesRepository : IUserRolesRepository
{
    private const string CollectionName = "user_roles";
    private readonly ILiteDatabase database;

    public LiteDbUserRolesRepository(ILiteDatabase database)
    {
        this.database = database ?? throw new ArgumentNullException(nameof(database));

        var collection = this.database.GetCollection<UserRole>(CollectionName);
        collection.EnsureIndex(x => x.UserIdentifier);
        collection.EnsureIndex(x => x.IdentifierType);
    }

    public Task<UserRole?> GetByUserIdentifierAsync(string userIdentifier, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userIdentifier))
        {
            return Task.FromResult<UserRole?>(null);
        }

        var normalizedIdentifier = NormalizeIdentifier(userIdentifier);
        var collection = database.GetCollection<UserRole>(CollectionName);

        var userRole = collection.FindOne(x => x.UserIdentifier == normalizedIdentifier);
        return Task.FromResult(userRole);
    }

    public Task<UserRole?> GetByAnyIdentifierAsync(
        IEnumerable<(string IdentifierType, string Value)> identifiers,
        CancellationToken cancellationToken = default)
    {
        var collection = database.GetCollection<UserRole>(CollectionName);

        foreach (var (identifierType, value) in identifiers)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            var normalizedValue = NormalizeIdentifier(value);
            var userRole = collection.FindOne(x => 
                x.UserIdentifier == normalizedValue && 
                x.IdentifierType == identifierType);

            if (userRole != null)
            {
                return Task.FromResult<UserRole?>(userRole);
            }
        }

        // Also try matching just by identifier value (for flexibility)
        foreach (var (_, value) in identifiers)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            var normalizedValue = NormalizeIdentifier(value);
            var userRole = collection.FindOne(x => x.UserIdentifier == normalizedValue);

            if (userRole != null)
            {
                return Task.FromResult<UserRole?>(userRole);
            }
        }

        return Task.FromResult<UserRole?>(null);
    }

    public Task<IReadOnlyList<UserRole>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var collection = database.GetCollection<UserRole>(CollectionName);
        var userRoles = collection.FindAll().ToList();
        return Task.FromResult<IReadOnlyList<UserRole>>(userRoles);
    }

    public Task<UserRole> UpsertAsync(UserRole userRole, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(userRole);

        var normalizedRole = userRole with
        {
            UserIdentifier = NormalizeIdentifier(userRole.UserIdentifier),
            ModifiedAt = DateTimeOffset.UtcNow
        };

        var collection = database.GetCollection<UserRole>(CollectionName);
        
        var existing = collection.FindOne(x => x.UserIdentifier == normalizedRole.UserIdentifier);
        if (existing != null)
        {
            normalizedRole = normalizedRole with { Id = existing.Id };
            collection.Update(normalizedRole);
        }
        else
        {
            normalizedRole = normalizedRole with
            {
                Id = Guid.NewGuid(),
                CreatedAt = DateTimeOffset.UtcNow
            };
            collection.Insert(normalizedRole);
        }

        return Task.FromResult(normalizedRole);
    }

    public async Task<UserRole> AddRolesAsync(
        string userIdentifier,
        string identifierType,
        IEnumerable<string> roles,
        string? displayName = null,
        string? provider = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedIdentifier = NormalizeIdentifier(userIdentifier);
        var existing = await GetByUserIdentifierAsync(normalizedIdentifier, cancellationToken);

        var rolesToAdd = roles.ToList();

        if (existing != null)
        {
            var updatedRoles = existing.Roles.Union(rolesToAdd).Distinct().ToList();
            var updated = existing with
            {
                Roles = updatedRoles,
                DisplayName = displayName ?? existing.DisplayName,
                Provider = provider ?? existing.Provider,
                ModifiedAt = DateTimeOffset.UtcNow
            };
            return await UpsertAsync(updated, cancellationToken);
        }

        var newUserRole = new UserRole
        {
            Id = Guid.NewGuid(),
            UserIdentifier = normalizedIdentifier,
            IdentifierType = identifierType,
            Roles = rolesToAdd,
            DisplayName = displayName,
            Provider = provider,
            CreatedAt = DateTimeOffset.UtcNow,
            ModifiedAt = DateTimeOffset.UtcNow
        };

        return await UpsertAsync(newUserRole, cancellationToken);
    }

    public async Task<UserRole?> RemoveRolesAsync(
        string userIdentifier,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default)
    {
        var existing = await GetByUserIdentifierAsync(userIdentifier, cancellationToken);
        if (existing == null)
        {
            return null;
        }

        var rolesToRemove = roles.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var updatedRoles = existing.Roles
            .Where(r => !rolesToRemove.Contains(r))
            .ToList();

        var updated = existing with
        {
            Roles = updatedRoles,
            ModifiedAt = DateTimeOffset.UtcNow
        };

        return await UpsertAsync(updated, cancellationToken);
    }

    public Task<bool> DeleteAsync(string userIdentifier, CancellationToken cancellationToken = default)
    {
        var normalizedIdentifier = NormalizeIdentifier(userIdentifier);
        var collection = database.GetCollection<UserRole>(CollectionName);

        var existing = collection.FindOne(x => x.UserIdentifier == normalizedIdentifier);
        if (existing == null)
        {
            return Task.FromResult(false);
        }

        var deleted = collection.Delete(existing.Id);
        return Task.FromResult(deleted);
    }

    private static string NormalizeIdentifier(string identifier)
    {
        return identifier.Trim().ToLowerInvariant();
    }
}
