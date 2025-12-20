using GarageUnderground.Models;

namespace GarageUnderground.Persistence;

/// <summary>
/// Repository for managing user registrations.
/// </summary>
public interface IUserRegistrationRepository
{
    /// <summary>
    /// Gets a user registration by email.
    /// </summary>
    Task<UserRegistration?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all registered users.
    /// </summary>
    Task<IReadOnlyList<UserRegistration>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers a user or updates their last login info if already registered.
    /// </summary>
    /// <param name="email">User's email address.</param>
    /// <param name="displayName">User's display name.</param>
    /// <param name="provider">Authentication provider used.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The user registration record.</returns>
    Task<UserRegistration> RegisterOrUpdateAsync(
        string email,
        string? displayName,
        string? provider,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches for users by email or display name.
    /// </summary>
    Task<IReadOnlyList<UserRegistration>> SearchAsync(
        string? searchTerm,
        CancellationToken cancellationToken = default);
}
