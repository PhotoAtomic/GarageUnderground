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
    public MicrosoftProviderConfiguration? Microsoft { get; init; }

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
    public virtual bool IsConfigured =>
        !string.IsNullOrWhiteSpace(ClientId) && !string.IsNullOrWhiteSpace(ClientSecret);
}

/// <summary>
/// Configuration for Microsoft Entra ID (Azure AD) authentication.
/// </summary>
public record MicrosoftProviderConfiguration : OAuthProviderConfiguration
{
    /// <summary>
    /// The tenant ID. Use "common" for multi-tenant, "consumers" for personal accounts only,
    /// "organizations" for work/school accounts only, or a specific tenant GUID.
    /// </summary>
    public string? TenantId { get; init; }

    /// <summary>
    /// Determines if this provider is properly configured.
    /// For Microsoft, TenantId is optional (defaults to "common" if not specified).
    /// </summary>
    public override bool IsConfigured =>
        !string.IsNullOrWhiteSpace(ClientId) && !string.IsNullOrWhiteSpace(ClientSecret);
}
