using System.Security.Claims;

namespace GarageUnderground.Authentication;

/// <summary>
/// Service for enriching user claims with additional roles from the internal database.
/// </summary>
public interface IClaimsEnrichmentService
{
    /// <summary>
    /// Enriches the claims principal with additional roles from the internal database.
    /// </summary>
    /// <param name="principal">The original claims principal from the authentication provider.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A new claims principal with enriched claims.</returns>
    Task<ClaimsPrincipal> EnrichClaimsAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default);

    /// <summary>
    /// Enriches the claims principal and optionally registers the user on new login.
    /// </summary>
    /// <param name="principal">The original claims principal from the authentication provider.</param>
    /// <param name="isNewLogin">True if this is a new login (to register/update user), false for cookie validation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A new claims principal with enriched claims.</returns>
    Task<ClaimsPrincipal> EnrichClaimsAsync(ClaimsPrincipal principal, bool isNewLogin, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the list of internal roles for a user based on their claims.
    /// </summary>
    /// <param name="principal">The claims principal.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of internal roles for the user.</returns>
    Task<IReadOnlyList<string>> GetInternalRolesAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default);
}
