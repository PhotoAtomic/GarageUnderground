using GarageUnderground.Models;

namespace GarageUnderground.Persistence;

/// <summary>
/// Repository for managing user roles in the internal database.
/// </summary>
public interface IUserRolesRepository
{
    /// <summary>
    /// Gets roles for a user by their identifier.
    /// </summary>
    /// <param name="userIdentifier">The user's unique identifier (e.g., email or object ID).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The user's roles, or null if not found.</returns>
    Task<UserRole?> GetByUserIdentifierAsync(string userIdentifier, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets roles for a user, trying multiple identifiers in order.
    /// This is useful when different providers use different identifier types.
    /// </summary>
    /// <param name="identifiers">Dictionary of identifier type to identifier value.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The first matching user role, or null if none found.</returns>
    Task<UserRole?> GetByAnyIdentifierAsync(
        IEnumerable<(string IdentifierType, string Value)> identifiers,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all user roles.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>All user roles in the database.</returns>
    Task<IReadOnlyList<UserRole>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates or updates a user's roles.
    /// </summary>
    /// <param name="userRole">The user role to save.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The saved user role.</returns>
    Task<UserRole> UpsertAsync(UserRole userRole, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds roles to an existing user, or creates a new user with the roles.
    /// </summary>
    /// <param name="userIdentifier">The user's unique identifier.</param>
    /// <param name="identifierType">The type of identifier.</param>
    /// <param name="roles">The roles to add.</param>
    /// <param name="displayName">Optional display name.</param>
    /// <param name="provider">Optional provider name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated user role.</returns>
    Task<UserRole> AddRolesAsync(
        string userIdentifier,
        string identifierType,
        IEnumerable<string> roles,
        string? displayName = null,
        string? provider = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes roles from a user.
    /// </summary>
    /// <param name="userIdentifier">The user's unique identifier.</param>
    /// <param name="roles">The roles to remove.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated user role, or null if user not found.</returns>
    Task<UserRole?> RemoveRolesAsync(
        string userIdentifier,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a user's role assignment.
    /// </summary>
    /// <param name="userIdentifier">The user's unique identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if deleted, false if not found.</returns>
    Task<bool> DeleteAsync(string userIdentifier, CancellationToken cancellationToken = default);
}
