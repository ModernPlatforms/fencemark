using fencemark.ApiService.Data;
using fencemark.ApiService.Data.Models;
using fencemark.ApiService.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace fencemark.ApiService.Services;

/// <summary>
/// Service for managing organizations and memberships
/// </summary>
public interface IOrganizationService
{
    Task<CreateOrganizationResponse> CreateOrganizationAsync(string userId, CreateOrganizationRequest request, CancellationToken cancellationToken = default);
    Task<UpdateOrganizationResponse> UpdateOrganizationAsync(string organizationId, UpdateOrganizationRequest request, CancellationToken cancellationToken = default);
    Task<IEnumerable<OrganizationMemberResponse>> GetMembersAsync(string organizationId, CancellationToken cancellationToken = default);
    Task<InviteUserResponse> InviteUserAsync(string organizationId, InviteUserRequest request, CancellationToken cancellationToken = default);
    Task<AuthResponse> AcceptInvitationAsync(AcceptInvitationRequest request, CancellationToken cancellationToken = default);
    Task<bool> UpdateRoleAsync(string organizationId, UpdateRoleRequest request, CancellationToken cancellationToken = default);
    Task<bool> RemoveMemberAsync(string organizationId, string userId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Implementation of organization service
/// </summary>
public class OrganizationService(
    ApplicationDbContext context,
    UserManager<ApplicationUser> userManager,
    ISeedDataService seedDataService) : IOrganizationService
{
    private const int MinimumOrganizationNameLength = 2;

    public async Task<CreateOrganizationResponse> CreateOrganizationAsync(
        string userId,
        CreateOrganizationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentNullException.ThrowIfNull(request);

        // Check if user exists
        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return new CreateOrganizationResponse
            {
                Success = false,
                Message = "User not found"
            };
        }

        // Check if user already has an organization
        var existingMembership = await context.OrganizationMembers
            .FirstOrDefaultAsync(m => m.UserId == userId, cancellationToken);

        if (existingMembership is not null)
        {
            return new CreateOrganizationResponse
            {
                Success = false,
                Message = "User already belongs to an organization"
            };
        }

        // Validate organization name
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Length < MinimumOrganizationNameLength)
        {
            return new CreateOrganizationResponse
            {
                Success = false,
                Message = $"Organization name must be at least {MinimumOrganizationNameLength} characters"
            };
        }

        // Check if organization name already exists (case-insensitive)
        var existingOrg = await context.Organizations
            .FirstOrDefaultAsync(o => o.Name.ToLower() == request.Name.ToLower(), cancellationToken);

        if (existingOrg is not null)
        {
            return new CreateOrganizationResponse
            {
                Success = false,
                Message = "An organization with this name already exists"
            };
        }

        // Create the organization
        var organization = new Organization
        {
            Name = request.Name,
            CreatedAt = DateTime.UtcNow
        };
        context.Organizations.Add(organization);
        await context.SaveChangesAsync(cancellationToken);

        // Add user as owner of the organization
        var membership = new OrganizationMember
        {
            UserId = userId,
            OrganizationId = organization.Id,
            Role = Role.Owner,
            JoinedAt = DateTime.UtcNow,
            IsAccepted = true
        };
        context.OrganizationMembers.Add(membership);
        await context.SaveChangesAsync(cancellationToken);

        // Add organization ID as a claim
        await userManager.AddClaimAsync(user, new System.Security.Claims.Claim(Infrastructure.CustomClaimTypes.OrganizationId, organization.Id));

        // Seed sample data for the new organization
        await seedDataService.SeedSampleDataAsync(organization.Id);

        return new CreateOrganizationResponse
        {
            Success = true,
            Message = "Organization created successfully",
            OrganizationId = organization.Id,
            OrganizationName = organization.Name
        };
    }

    public async Task<UpdateOrganizationResponse> UpdateOrganizationAsync(
        string organizationId,
        UpdateOrganizationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(organizationId);
        ArgumentNullException.ThrowIfNull(request);

        // Validate organization name
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Length < MinimumOrganizationNameLength)
        {
            return new UpdateOrganizationResponse
            {
                Success = false,
                Message = $"Organization name must be at least {MinimumOrganizationNameLength} characters"
            };
        }

        // Get the organization
        var organization = await context.Organizations
            .FirstOrDefaultAsync(o => o.Id == organizationId, cancellationToken);

        if (organization is null)
        {
            return new UpdateOrganizationResponse
            {
                Success = false,
                Message = "Organization not found"
            };
        }

        // Check if another organization already has this name
        var existingOrg = await context.Organizations
            .FirstOrDefaultAsync(o => o.Name == request.Name && o.Id != organizationId, cancellationToken);

        if (existingOrg is not null)
        {
            return new UpdateOrganizationResponse
            {
                Success = false,
                Message = "An organization with this name already exists"
            };
        }

        // Update the organization
        organization.Name = request.Name;
        await context.SaveChangesAsync(cancellationToken);

        return new UpdateOrganizationResponse
        {
            Success = true,
            Message = "Organization updated successfully"
        };
    }

    public async Task<IEnumerable<OrganizationMemberResponse>> GetMembersAsync(
        string organizationId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(organizationId);

        var members = await context.OrganizationMembers
            .Include(m => m.User)
            .Where(m => m.OrganizationId == organizationId)
            .OrderBy(m => m.JoinedAt)
            .Select(m => new OrganizationMemberResponse
            {
                UserId = m.UserId,
                Email = m.User.Email!,
                Role = m.Role.ToString(),
                JoinedAt = m.JoinedAt,
                IsGuest = m.User.IsGuest
            })
            .ToListAsync(cancellationToken);

        return members;
    }

    public async Task<InviteUserResponse> InviteUserAsync(
        string organizationId,
        InviteUserRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(organizationId);
        ArgumentNullException.ThrowIfNull(request);

        // Check if organization exists
        var organization = await context.Organizations
            .FindAsync([organizationId], cancellationToken);

        if (organization is null)
        {
            return new InviteUserResponse
            {
                Success = false,
                Message = "Organization not found"
            };
        }

        // Check if user already exists
        var existingUser = await userManager.FindByEmailAsync(request.Email);
        if (existingUser is not null)
        {
            // Check if already a member
            var existingMembership = await context.OrganizationMembers
                .AnyAsync(m => m.UserId == existingUser.Id && m.OrganizationId == organizationId,
                    cancellationToken);

            if (existingMembership)
            {
                return new InviteUserResponse
                {
                    Success = false,
                    Message = "User is already a member of this organization"
                };
            }
        }

        // Parse role
        if (!Enum.TryParse<Role>(request.Role, ignoreCase: true, out var role))
        {
            return new InviteUserResponse
            {
                Success = false,
                Message = "Invalid role specified"
            };
        }

        // Prevent creating another owner
        if (role == Role.Owner)
        {
            return new InviteUserResponse
            {
                Success = false,
                Message = "Cannot invite another owner. Only one owner per organization is allowed."
            };
        }

        // Generate invitation token
        var invitationToken = SecureTokenGenerator.Generate();

        if (existingUser is not null)
        {
            // User exists, create pending membership
            var membership = new OrganizationMember
            {
                UserId = existingUser.Id,
                OrganizationId = organizationId,
                Role = role,
                InvitedAt = DateTime.UtcNow,
                InvitationToken = invitationToken,
                IsAccepted = false
            };
            context.OrganizationMembers.Add(membership);
        }
        else
        {
            // Create placeholder user with invitation
            var newUser = new ApplicationUser
            {
                UserName = request.Email,
                Email = request.Email,
                IsGuest = true,
                IsEmailVerified = false,
                EmailConfirmed = false,
                CreatedAt = DateTime.UtcNow
            };

            var result = await userManager.CreateAsync(newUser);
            if (!result.Succeeded)
            {
                return new InviteUserResponse
                {
                    Success = false,
                    Message = "Failed to create user invitation"
                };
            }

            var membership = new OrganizationMember
            {
                UserId = newUser.Id,
                OrganizationId = organizationId,
                Role = role,
                InvitedAt = DateTime.UtcNow,
                InvitationToken = invitationToken,
                IsAccepted = false
            };
            context.OrganizationMembers.Add(membership);
        }

        await context.SaveChangesAsync(cancellationToken);

        return new InviteUserResponse
        {
            Success = true,
            Message = "Invitation sent successfully",
            InvitationToken = invitationToken
        };
    }

    public async Task<AuthResponse> AcceptInvitationAsync(
        AcceptInvitationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var membership = await context.OrganizationMembers
            .Include(m => m.User)
            .Include(m => m.Organization)
            .FirstOrDefaultAsync(m => m.InvitationToken == request.Token, cancellationToken);

        if (membership is null)
        {
            return new AuthResponse
            {
                Success = false,
                Message = "Invalid invitation token"
            };
        }

        if (membership.IsAccepted)
        {
            return new AuthResponse
            {
                Success = false,
                Message = "Invitation already accepted"
            };
        }

        // Set password if user doesn't have one
        var user = membership.User;
        if (!await userManager.HasPasswordAsync(user))
        {
            var token = await userManager.GeneratePasswordResetTokenAsync(user);
            await userManager.ResetPasswordAsync(user, token, request.Password);
        }

        // Accept the invitation
        membership.IsAccepted = true;
        membership.JoinedAt = DateTime.UtcNow;
        membership.InvitationToken = null;

        // Update user status
        user.IsEmailVerified = true;
        user.IsGuest = false;
        user.EmailConfirmed = true;

        await context.SaveChangesAsync(cancellationToken);

        // Add organization ID as a claim
        await userManager.AddClaimAsync(user, new System.Security.Claims.Claim("OrganizationId", membership.OrganizationId));

        return new AuthResponse
        {
            Success = true,
            Message = "Invitation accepted successfully",
            UserId = user.Id,
            OrganizationId = membership.OrganizationId,
            Email = user.Email,
            IsGuest = false
        };
    }

    public async Task<bool> UpdateRoleAsync(
        string organizationId,
        UpdateRoleRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(organizationId);
        ArgumentNullException.ThrowIfNull(request);

        var membership = await context.OrganizationMembers
            .FirstOrDefaultAsync(
                m => m.UserId == request.UserId && m.OrganizationId == organizationId,
                cancellationToken);

        if (membership is null)
        {
            return false;
        }

        // Prevent changing owner role
        if (membership.Role == Role.Owner)
        {
            return false;
        }

        if (!Enum.TryParse<Role>(request.Role, ignoreCase: true, out var role))
        {
            return false;
        }

        // Prevent creating another owner
        if (role == Role.Owner)
        {
            return false;
        }

        membership.Role = role;
        await context.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<bool> RemoveMemberAsync(
        string organizationId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(organizationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        var membership = await context.OrganizationMembers
            .FirstOrDefaultAsync(
                m => m.UserId == userId && m.OrganizationId == organizationId,
                cancellationToken);

        if (membership is null)
        {
            return false;
        }

        // Prevent removing owner
        if (membership.Role == Role.Owner)
        {
            return false;
        }

        context.OrganizationMembers.Remove(membership);
        await context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
