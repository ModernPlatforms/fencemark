using fencemark.ApiService.Data;
using fencemark.ApiService.Data.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace fencemark.ApiService.Services;

/// <summary>
/// Service for handling authentication and user management
/// </summary>
public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<bool> VerifyEmailAsync(string userId, string token, CancellationToken cancellationToken = default);
}

/// <summary>
/// Implementation of authentication service
/// </summary>
public class AuthService(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    ApplicationDbContext context) : IAuthService
{
    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Check if user already exists
        var existingUser = await userManager.FindByEmailAsync(request.Email);
        if (existingUser is not null)
        {
            return new AuthResponse
            {
                Success = false,
                Message = "User already exists with this email"
            };
        }

        // Create the user
        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            IsGuest = true,
            IsEmailVerified = false,
            CreatedAt = DateTime.UtcNow
        };

        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            return new AuthResponse
            {
                Success = false,
                Message = string.Join(", ", result.Errors.Select(e => e.Description))
            };
        }

        // Create organization
        var organization = new Organization
        {
            Name = request.OrganizationName,
            CreatedAt = DateTime.UtcNow
        };
        context.Organizations.Add(organization);

        // Add user as owner of the organization
        var membership = new OrganizationMember
        {
            UserId = user.Id,
            OrganizationId = organization.Id,
            Role = Role.Owner,
            JoinedAt = DateTime.UtcNow,
            IsAccepted = true
        };
        context.OrganizationMembers.Add(membership);

        await context.SaveChangesAsync(cancellationToken);

        // Generate email verification token (for future use)
        var emailToken = await userManager.GenerateEmailConfirmationTokenAsync(user);

        return new AuthResponse
        {
            Success = true,
            Message = "Registration successful. Please verify your email.",
            UserId = user.Id,
            OrganizationId = organization.Id,
            Email = user.Email,
            IsGuest = user.IsGuest
        };
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null)
        {
            return new AuthResponse
            {
                Success = false,
                Message = "Invalid email or password"
            };
        }

        var result = await signInManager.PasswordSignInAsync(
            user,
            request.Password,
            isPersistent: true,
            lockoutOnFailure: false);

        if (!result.Succeeded)
        {
            return new AuthResponse
            {
                Success = false,
                Message = "Invalid email or password"
            };
        }

        // Get user's organization
        var membership = await context.OrganizationMembers
            .Include(m => m.Organization)
            .FirstOrDefaultAsync(m => m.UserId == user.Id, cancellationToken);

        return new AuthResponse
        {
            Success = true,
            Message = "Login successful",
            UserId = user.Id,
            OrganizationId = membership?.OrganizationId,
            Email = user.Email,
            IsGuest = user.IsGuest
        };
    }

    public async Task<bool> VerifyEmailAsync(string userId, string token, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentException.ThrowIfNullOrWhiteSpace(token);

        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return false;
        }

        var result = await userManager.ConfirmEmailAsync(user, token);
        if (!result.Succeeded)
        {
            return false;
        }

        // Remove guest status after email verification
        user.IsEmailVerified = true;
        user.IsGuest = false;
        await userManager.UpdateAsync(user);

        return true;
    }
}
