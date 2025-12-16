namespace GarageUnderground.Authentication;

/// <summary>
/// Represents an available authentication provider.
/// </summary>
public record AuthProviderInfo(string Scheme, string DisplayName, string IconClass);

/// <summary>
/// Service for managing authentication providers.
/// </summary>
public interface IAuthenticationProviderService
{
    /// <summary>
    /// Gets all available authentication providers.
    /// </summary>
    IReadOnlyList<AuthProviderInfo> GetAvailableProviders();

    /// <summary>
    /// Determines if mock authentication is active (no real providers configured).
    /// </summary>
    bool IsMockAuthenticationActive { get; }
}
