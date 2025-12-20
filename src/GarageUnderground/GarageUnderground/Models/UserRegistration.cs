namespace GarageUnderground.Models;

/// <summary>
/// Represents a registered user in the system.
/// Users are automatically registered when they log in for the first time.
/// </summary>
public record UserRegistration
{
    /// <summary>
    /// Internal database identifier.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// The user's email address (primary identifier).
    /// </summary>
    public required string Email { get; init; }

    /// <summary>
    /// The user's display name.
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// The authentication provider used (e.g., Microsoft, Google, Mock).
    /// </summary>
    public string? Provider { get; set; }

    /// <summary>
    /// When the user first logged in.
    /// </summary>
    public DateTimeOffset FirstLoginAt { get; init; }

    /// <summary>
    /// When the user last logged in.
    /// </summary>
    public DateTimeOffset LastLoginAt { get; set; }

    /// <summary>
    /// Number of times the user has logged in.
    /// </summary>
    public int LoginCount { get; set; }
}
