namespace GarageUnderground.Authentication;

/// <summary>
/// Configuration for authentication providers.
/// </summary>
public record AuthenticationConfiguration
{
    public const string SectionName = "Authentication";

    /// <summary>
    /// Microsoft Entra ID (Azure AD) authentication settings.
    /// </summary>
    public OAuthProviderConfiguration? Microsoft { get; init; }

    /// <summary>
    /// Google OAuth authentication settings.
    /// </summary>
    public OAuthProviderConfiguration? Google { get; init; }

    /// <summary>
    /// Determines if any external provider is configured.
    /// </summary>
    public bool HasConfiguredProviders =>
        (Microsoft?.IsConfigured ?? false) || (Google?.IsConfigured ?? false);
}

/// <summary>
/// Configuration for an OAuth provider.
/// </summary>
public record OAuthProviderConfiguration
{
    /// <summary>
    /// The client/application ID.
    /// </summary>
    public string? ClientId { get; init; }

    /// <summary>
    /// The client secret.
    /// </summary>
    public string? ClientSecret { get; init; }

    /// <summary>
    /// Determines if this provider is properly configured.
    /// </summary>
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(ClientId) && !string.IsNullOrWhiteSpace(ClientSecret);
}
