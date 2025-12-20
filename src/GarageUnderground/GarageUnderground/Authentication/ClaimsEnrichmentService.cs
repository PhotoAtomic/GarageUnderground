using System.Security.Claims;
using GarageUnderground.Persistence;

namespace GarageUnderground.Authentication;

/// <summary>
/// Service that enriches user claims with additional roles from the internal database.
/// Combines roles from authentication providers (e.g., Entra ID App Roles) with internal database roles.
/// </summary>
public sealed class ClaimsEnrichmentService : IClaimsEnrichmentService
{
    private readonly IUserRolesRepository userRolesRepository;
    private readonly ILogger<ClaimsEnrichmentService> logger;

    /// <summary>
    /// Custom claim type for roles added from the internal database.
    /// </summary>
    public const string InternalRoleClaimType = "internal_role";

    /// <summary>
    /// Custom claim type for roles from the authentication provider (e.g., Entra ID).
    /// </summary>
    public const string ProviderRoleClaimType = "provider_role";

    public ClaimsEnrichmentService(
        IUserRolesRepository userRolesRepository,
        ILogger<ClaimsEnrichmentService> logger)
    {
        this.userRolesRepository = userRolesRepository;
        this.logger = logger;
    }

    public async Task<ClaimsPrincipal> EnrichClaimsAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default)
    {
        if (principal.Identity?.IsAuthenticated != true)
        {
            return principal;
        }

        var originalIdentity = principal.Identity as ClaimsIdentity;
        if (originalIdentity == null)
        {
            return principal;
        }

        // Get existing roles from provider (e.g., Entra ID App Roles)
        var providerRoles = principal.Claims
            .Where(c => c.Type == ClaimTypes.Role || c.Type == "roles")
            .Select(c => c.Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        // Get internal roles from database
        var internalRoles = await GetInternalRolesAsync(principal, cancellationToken);

        // Combine all roles (distinct, case-insensitive)
        var allRoles = providerRoles
            .Union(internalRoles, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var userIdentifier = GetPrimaryIdentifier(principal);

        logger.LogInformation(
            "User {UserIdentifier}: Provider roles: [{ProviderRoles}], Internal roles: [{InternalRoles}], Combined: [{AllRoles}]",
            userIdentifier,
            string.Join(", ", providerRoles),
            string.Join(", ", internalRoles),
            string.Join(", ", allRoles));

        // If no changes needed, return original
        if (internalRoles.Count == 0 && providerRoles.Count == allRoles.Count)
        {
            return principal;
        }

        // Clone the existing claims (excluding existing role claims to rebuild them)
        var nonRoleClaims = originalIdentity.Claims
            .Where(c => c.Type != ClaimTypes.Role && c.Type != "roles")
            .ToList();

        var enrichedIdentity = new ClaimsIdentity(
            nonRoleClaims,
            originalIdentity.AuthenticationType,
            originalIdentity.NameClaimType,
            ClaimTypes.Role); // Use standard role claim type

        // Add all combined roles as standard Role claims
        foreach (var role in allRoles)
        {
            enrichedIdentity.AddClaim(new Claim(ClaimTypes.Role, role));
        }

        // Track origin of roles for debugging/UI
        foreach (var role in providerRoles)
        {
            enrichedIdentity.AddClaim(new Claim(ProviderRoleClaimType, role));
        }

        foreach (var role in internalRoles)
        {
            enrichedIdentity.AddClaim(new Claim(InternalRoleClaimType, role));
        }

        return new ClaimsPrincipal(enrichedIdentity);
    }

    public async Task<IReadOnlyList<string>> GetInternalRolesAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default)
    {
        var identifiers = ExtractUserIdentifiers(principal);
        if (identifiers.Count == 0)
        {
            logger.LogWarning("No identifiers found for user");
            return [];
        }

        logger.LogInformation("Looking for internal roles with identifiers: {Identifiers}",
            string.Join(", ", identifiers.Select(i => $"{i.IdentifierType}={i.Value}")));

        var userRole = await userRolesRepository.GetByAnyIdentifierAsync(identifiers, cancellationToken);
        if (userRole == null)
        {
            logger.LogInformation("No internal roles found in database for this user");
            return [];
        }

        logger.LogInformation("Found internal roles in database: {Roles}", string.Join(", ", userRole.Roles));
        return userRole.Roles;
    }

    /// <summary>
    /// Extracts all possible user identifiers from the claims principal.
    /// Different providers use different claim types for user identification.
    /// </summary>
    private static List<(string IdentifierType, string Value)> ExtractUserIdentifiers(ClaimsPrincipal principal)
    {
        var identifiers = new List<(string IdentifierType, string Value)>();
        var claims = principal.Claims;

        // Email is the most universal identifier
        var email = claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value
                 ?? claims.FirstOrDefault(c => c.Type == "email")?.Value
                 ?? claims.FirstOrDefault(c => c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress")?.Value
                 ?? claims.FirstOrDefault(c => c.Type == "preferred_username")?.Value;

        if (!string.IsNullOrWhiteSpace(email))
        {
            identifiers.Add(("email", email));
        }

        // Microsoft Entra ID object ID
        var oid = claims.FirstOrDefault(c => c.Type == "oid")?.Value
               ?? claims.FirstOrDefault(c => c.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value;

        if (!string.IsNullOrWhiteSpace(oid))
        {
            identifiers.Add(("oid", oid));
        }

        // Standard subject claim (used by most OIDC providers)
        var sub = claims.FirstOrDefault(c => c.Type == "sub")?.Value
               ?? claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

        if (!string.IsNullOrWhiteSpace(sub))
        {
            identifiers.Add(("sub", sub));
        }

        return identifiers;
    }

    private static string GetPrimaryIdentifier(ClaimsPrincipal principal)
    {
        var identifiers = ExtractUserIdentifiers(principal);
        return identifiers.FirstOrDefault().Value ?? "unknown";
    }
}
