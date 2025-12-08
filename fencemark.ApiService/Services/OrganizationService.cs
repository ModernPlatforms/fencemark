using fencemark.ApiService.Data;
using fencemark.ApiService.Data.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace fencemark.ApiService.Services;

/// <summary>
/// Service for managing organizations and memberships
/// </summary>
public interface IOrganizationService
{
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
    UserManager<ApplicationUser> userManager) : IOrganizationService
{
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
        var invitationToken = Guid.NewGuid().ToString();

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
