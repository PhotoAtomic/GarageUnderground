using GarageUnderground.Models;
using GarageUnderground.Persistence;

namespace GarageUnderground.Api;

/// <summary>
/// API endpoints for managing user roles (admin only).
/// </summary>
public static class AdminRolesEndpoints
{
    /// <summary>
    /// Maps admin role management endpoints.
    /// </summary>
    public static IEndpointRouteBuilder MapAdminRolesEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/admin/roles")
            .RequireAuthorization("CanAdmin");

        // Get all registered users with their roles
        group.MapGet("/users", GetAllUsers);

        // Get roles for a specific user
        group.MapGet("/users/{email}", GetUserRoles);

        // Set roles for a user (replaces existing roles)
        group.MapPut("/users/{email}", SetUserRoles);

        // Add a role to a user
        group.MapPost("/users/{email}/roles", AddUserRole);

        // Remove a role from a user
        group.MapDelete("/users/{email}/roles/{role}", RemoveUserRole);

        // Get list of available roles
        group.MapGet("/available", GetAvailableRoles);

        return endpoints;
    }

    private static async Task<IResult> GetAllUsers(
        IUserRegistrationRepository userRegistrationRepository,
        IUserRolesRepository userRolesRepository)
    {
        var registrations = await userRegistrationRepository.GetAllAsync();
        
        var users = new List<UserWithRolesDto>();
        
        foreach (var reg in registrations)
        {
            var userRole = await userRolesRepository.GetByUserIdentifierAsync(reg.Email);
            users.Add(new UserWithRolesDto(
                reg.Email,
                reg.DisplayName,
                reg.Provider,
                userRole?.Roles.ToArray() ?? [],
                reg.FirstLoginAt,
                reg.LastLoginAt,
                reg.LoginCount));
        }

        return Results.Ok(users);
    }

    private static async Task<IResult> GetUserRoles(
        string email,
        IUserRegistrationRepository userRegistrationRepository,
        IUserRolesRepository userRolesRepository)
    {
        var registration = await userRegistrationRepository.GetByEmailAsync(email);
        if (registration == null)
        {
            return Results.NotFound($"User with email '{email}' not found");
        }

        var userRole = await userRolesRepository.GetByUserIdentifierAsync(email);

        return Results.Ok(new UserWithRolesDto(
            registration.Email,
            registration.DisplayName,
            registration.Provider,
            userRole?.Roles.ToArray() ?? [],
            registration.FirstLoginAt,
            registration.LastLoginAt,
            registration.LoginCount));
    }

    private static async Task<IResult> SetUserRoles(
        string email,
        SetRolesRequest request,
        IUserRegistrationRepository userRegistrationRepository,
        IUserRolesRepository userRolesRepository)
    {
        var registration = await userRegistrationRepository.GetByEmailAsync(email);
        if (registration == null)
        {
            return Results.NotFound($"User with email '{email}' not found");
        }

        var userRole = new UserRole
        {
            Id = Guid.NewGuid(),
            UserIdentifier = email.ToLowerInvariant(),
            IdentifierType = "email",
            Roles = request.Roles.Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
            DisplayName = registration.DisplayName,
            Provider = registration.Provider,
            CreatedAt = DateTimeOffset.UtcNow,
            ModifiedAt = DateTimeOffset.UtcNow
        };

        await userRolesRepository.UpsertAsync(userRole);

        return Results.Ok(new { 
            success = true, 
            email, 
            roles = userRole.Roles,
            message = "Roles updated. User needs to log out and log in again to see changes."
        });
    }

    private static async Task<IResult> AddUserRole(
        string email,
        AddRoleRequest request,
        IUserRegistrationRepository userRegistrationRepository,
        IUserRolesRepository userRolesRepository)
    {
        var registration = await userRegistrationRepository.GetByEmailAsync(email);
        if (registration == null)
        {
            return Results.NotFound($"User with email '{email}' not found");
        }

        var userRole = await userRolesRepository.AddRolesAsync(
            email,
            "email",
            [request.Role],
            registration.DisplayName,
            registration.Provider);

        return Results.Ok(new { 
            success = true, 
            email, 
            roles = userRole.Roles,
            message = "Role added. User needs to log out and log in again to see changes."
        });
    }

    private static async Task<IResult> RemoveUserRole(
        string email,
        string role,
        IUserRegistrationRepository userRegistrationRepository,
        IUserRolesRepository userRolesRepository)
    {
        var registration = await userRegistrationRepository.GetByEmailAsync(email);
        if (registration == null)
        {
            return Results.NotFound($"User with email '{email}' not found");
        }

        var userRole = await userRolesRepository.RemoveRolesAsync(email, [role]);

        return Results.Ok(new { 
            success = true, 
            email, 
            roles = userRole?.Roles ?? [],
            message = "Role removed. User needs to log out and log in again to see changes."
        });
    }

    private static IResult GetAvailableRoles()
    {
        // List of predefined roles available in the system
        var roles = new[]
        {
            new RoleInfo("canAdmin", "Amministratore", "Può gestire i ruoli utente e le impostazioni di sistema"),
            new RoleInfo("canLogin", "Accesso", "Può accedere all'applicazione")
        };

        return Results.Ok(roles);
    }
}

/// <summary>
/// User information with their assigned roles.
/// </summary>
public record UserWithRolesDto(
    string Email,
    string? DisplayName,
    string? Provider,
    string[] Roles,
    DateTimeOffset FirstLoginAt,
    DateTimeOffset LastLoginAt,
    int LoginCount);

/// <summary>
/// Request to set roles for a user.
/// </summary>
public record SetRolesRequest(List<string> Roles);

/// <summary>
/// Request to add a role to a user.
/// </summary>
public record AddRoleRequest(string Role);

/// <summary>
/// Information about an available role.
/// </summary>
public record RoleInfo(string Name, string DisplayName, string Description);
