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
    Task<AuthResponse> ExternalLoginAsync(ExternalLoginRequest request, CancellationToken cancellationToken = default);
    Task<bool> VerifyEmailAsync(string userId, string token, CancellationToken cancellationToken = default);
}

/// <summary>
/// Implementation of authentication service
/// </summary>
public class AuthService(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    ApplicationDbContext context,
    ISeedDataService seedDataService) : IAuthService
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

        // Seed standard data for new organization
        await seedDataService.SeedSampleDataAsync(organization.Id);

        // Add organization ID as a claim
        await userManager.AddClaimAsync(user, new System.Security.Claims.Claim("OrganizationId", organization.Id));

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

        // Get user's organization before sign-in to add as claim
        var membership = await context.OrganizationMembers
            .Include(m => m.Organization)
            .FirstOrDefaultAsync(m => m.UserId == user.Id, cancellationToken);

        // Add organization ID as a claim if the user has one
        if (membership is not null)
        {
            // Remove any existing OrganizationId claims first
            var existingClaims = await userManager.GetClaimsAsync(user);
            var orgClaims = existingClaims.Where(c => c.Type == "OrganizationId").ToList();
            foreach (var claim in orgClaims)
            {
                await userManager.RemoveClaimAsync(user, claim);
            }
            
            // Add the current organization ID claim
            await userManager.AddClaimAsync(user, new System.Security.Claims.Claim("OrganizationId", membership.OrganizationId));
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

    public async Task<AuthResponse> ExternalLoginAsync(ExternalLoginRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Try to find user by external ID first, then by email
        var user = await userManager.Users
            .FirstOrDefaultAsync(u => u.ExternalId == request.ExternalId && u.ExternalProvider == request.Provider, cancellationToken);

        if (user is null)
        {
            // Try to find by email
            user = await userManager.FindByEmailAsync(request.Email);
            
            if (user is not null)
            {
                // Existing user, link external identity
                user.ExternalId = request.ExternalId;
                user.ExternalProvider = request.Provider;
                user.IsEmailVerified = true; // External provider verified the email
                user.IsGuest = false;
                await userManager.UpdateAsync(user);
            }
            else
            {
                // Create new user
                user = new ApplicationUser
                {
                    UserName = request.Email,
                    Email = request.Email,
                    ExternalId = request.ExternalId,
                    ExternalProvider = request.Provider,
                    IsEmailVerified = true, // External provider verified the email
                    IsGuest = false,
                    CreatedAt = DateTime.UtcNow
                };

                var result = await userManager.CreateAsync(user);
                if (!result.Succeeded)
                {
                    return new AuthResponse
                    {
                        Success = false,
                        Message = string.Join(", ", result.Errors.Select(e => e.Description))
                    };
                }
            }
        }

        // Ensure the user has an organization membership
        // This handles cases where:
        // 1. A new user was just created and needs an organization
        // 2. An existing user was found but has no organization (e.g., deleted from DB)
        var existingMembership = await context.OrganizationMembers
            .FirstOrDefaultAsync(m => m.UserId == user.Id, cancellationToken);

        if (existingMembership is null)
        {
            // Find or create organization
            var organization = await context.Organizations
                .FirstOrDefaultAsync(o => o.Name == request.OrganizationName, cancellationToken);

            if (organization is null)
            {
                organization = new Organization
                {
                    Name = request.OrganizationName,
                    CreatedAt = DateTime.UtcNow
                };
                context.Organizations.Add(organization);
            }

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

            // Seed standard data for new organization
            await seedDataService.SeedSampleDataAsync(organization.Id);
        }

        // Get user's organization to add as claim
        var userMembership = await context.OrganizationMembers
            .Include(m => m.Organization)
            .FirstOrDefaultAsync(m => m.UserId == user.Id, cancellationToken);

        // Add organization ID as a claim if the user has one
        if (userMembership is not null)
        {
            // Remove any existing OrganizationId claims first
            var existingClaims = await userManager.GetClaimsAsync(user);
            var orgClaims = existingClaims.Where(c => c.Type == "OrganizationId").ToList();
            foreach (var claim in orgClaims)
            {
                await userManager.RemoveClaimAsync(user, claim);
            }
            
            // Add the current organization ID claim
            await userManager.AddClaimAsync(user, new System.Security.Claims.Claim("OrganizationId", userMembership.OrganizationId));
        }

        return new AuthResponse
        {
            Success = true,
            Message = "External login successful",
            UserId = user.Id,
            OrganizationId = userMembership?.OrganizationId,
            Email = user.Email,
            IsGuest = user.IsGuest,
            OrganizationName = userMembership?.Organization?.Name
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
