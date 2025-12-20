namespace GarageUnderground.Models;

/// <summary>
/// Represents a user's assigned roles in the internal database.
/// Roles are identified by the user's unique identifier from the authentication provider.
/// </summary>
public record UserRole
{
    /// <summary>
    /// Internal database identifier.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// The unique identifier for the user from the authentication provider.
    /// This could be an email, object ID, or other stable identifier.
    /// </summary>
    public required string UserIdentifier { get; init; }

    /// <summary>
    /// The type of identifier used (e.g., "email", "oid", "sub").
    /// Helps distinguish between different identifier formats.
    /// </summary>
    public required string IdentifierType { get; init; }

    /// <summary>
    /// The roles assigned to this user from the internal database.
    /// </summary>
    public required List<string> Roles { get; init; }

    /// <summary>
    /// Optional display name for easier identification in admin UI.
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// The authentication provider this user typically uses.
    /// </summary>
    public string? Provider { get; set; }

    /// <summary>
    /// When this record was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// When this record was last modified.
    /// </summary>
    public DateTimeOffset ModifiedAt { get; set; }
}
