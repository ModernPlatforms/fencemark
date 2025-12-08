using fencemark.ApiService.Data;
using fencemark.ApiService.Data.Models;
using fencemark.ApiService.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace fencemark.Tests;

public class OrganizationServiceTests
{
    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static UserManager<ApplicationUser> CreateUserManager(ApplicationDbContext context)
    {
        var store = new Microsoft.AspNetCore.Identity.EntityFrameworkCore.UserStore<ApplicationUser>(context);
        var options = Options.Create(new IdentityOptions());
        var passwordHasher = new PasswordHasher<ApplicationUser>();
        var userValidators = new List<IUserValidator<ApplicationUser>> { new UserValidator<ApplicationUser>() };
        var passwordValidators = new List<IPasswordValidator<ApplicationUser>> { new PasswordValidator<ApplicationUser>() };
        var keyNormalizer = new UpperInvariantLookupNormalizer();
        var errors = new IdentityErrorDescriber();
        var logger = new Logger<UserManager<ApplicationUser>>(new LoggerFactory());

        return new UserManager<ApplicationUser>(
            store, options, passwordHasher, userValidators, passwordValidators,
            keyNormalizer, errors, null!, logger);
    }

    private async Task<(Organization org, ApplicationUser owner, ApplicationDbContext context, OrganizationService service)> SetupTestOrganizationAsync()
    {
        var context = CreateDbContext();
        var userManager = CreateUserManager(context);
        var service = new OrganizationService(context, userManager);

        var owner = new ApplicationUser
        {
            UserName = "owner@test.com",
            Email = "owner@test.com",
            IsGuest = false
        };
        await userManager.CreateAsync(owner, "Owner123!");

        var org = new Organization { Name = "Test Org" };
        context.Organizations.Add(org);

        var membership = new OrganizationMember
        {
            UserId = owner.Id,
            OrganizationId = org.Id,
            Role = Role.Owner,
            IsAccepted = true
        };
        context.OrganizationMembers.Add(membership);
        await context.SaveChangesAsync();

        return (org, owner, context, service);
    }

    [Fact]
    public async Task WhenAdminInvitesUserInvitationIsCreated()
    {
        // Arrange
        var (org, owner, context, service) = await SetupTestOrganizationAsync();

        var request = new InviteUserRequest
        {
            Email = "newmember@test.com",
            Role = "Member"
        };

        // Act
        var response = await service.InviteUserAsync(org.Id, request);

        // Assert
        Assert.True(response.Success);
        Assert.NotNull(response.InvitationToken);

        var membership = await context.OrganizationMembers
            .FirstOrDefaultAsync(m => m.OrganizationId == org.Id && m.InvitationToken == response.InvitationToken);

        Assert.NotNull(membership);
        Assert.False(membership.IsAccepted);
        Assert.Equal(Role.Member, membership.Role);

        await context.DisposeAsync();
    }

    [Fact]
    public async Task WhenInvitedUserCanBeAssignedDifferentRoles()
    {
        // Arrange
        var (org, owner, context, service) = await SetupTestOrganizationAsync();

        // Act & Assert - Test each role
        var roles = new[] { "Admin", "Member", "Billing", "ReadOnly" };
        foreach (var role in roles)
        {
            var request = new InviteUserRequest
            {
                Email = $"{role.ToLower()}@test.com",
                Role = role
            };

            var response = await service.InviteUserAsync(org.Id, request);
            Assert.True(response.Success);

            var membership = await context.OrganizationMembers
                .FirstOrDefaultAsync(m => m.InvitationToken == response.InvitationToken);

            Assert.NotNull(membership);
            Assert.Equal(Enum.Parse<Role>(role), membership.Role);
        }

        await context.DisposeAsync();
    }

    [Fact]
    public async Task WhenInvitingAsOwnerItShouldFail()
    {
        // Arrange
        var (org, owner, context, service) = await SetupTestOrganizationAsync();

        var request = new InviteUserRequest
        {
            Email = "anotherwner@test.com",
            Role = "Owner"
        };

        // Act
        var response = await service.InviteUserAsync(org.Id, request);

        // Assert
        Assert.False(response.Success);
        Assert.Contains("owner", response.Message, StringComparison.OrdinalIgnoreCase);

        await context.DisposeAsync();
    }

    [Fact]
    public async Task WhenAllDataIsScopedToOrganizationNoDataLeaks()
    {
        // Arrange
        var context = CreateDbContext();
        var userManager = CreateUserManager(context);
        var service = new OrganizationService(context, userManager);

        // Create two separate organizations
        var org1 = new Organization { Name = "Org 1" };
        var org2 = new Organization { Name = "Org 2" };
        context.Organizations.AddRange(org1, org2);

        var user1 = new ApplicationUser { UserName = "user1@org1.com", Email = "user1@org1.com" };
        var user2 = new ApplicationUser { UserName = "user2@org2.com", Email = "user2@org2.com" };
        await userManager.CreateAsync(user1, "Pass123!");
        await userManager.CreateAsync(user2, "Pass123!");

        context.OrganizationMembers.Add(new OrganizationMember
        {
            UserId = user1.Id,
            OrganizationId = org1.Id,
            Role = Role.Owner,
            IsAccepted = true
        });

        context.OrganizationMembers.Add(new OrganizationMember
        {
            UserId = user2.Id,
            OrganizationId = org2.Id,
            Role = Role.Owner,
            IsAccepted = true
        });

        await context.SaveChangesAsync();

        // Act
        var org1Members = await service.GetMembersAsync(org1.Id);
        var org2Members = await service.GetMembersAsync(org2.Id);

        // Assert
        Assert.Single(org1Members);
        Assert.Single(org2Members);
        Assert.Equal("user1@org1.com", org1Members.First().Email);
        Assert.Equal("user2@org2.com", org2Members.First().Email);

        await context.DisposeAsync();
    }

    [Fact]
    public async Task WhenUpdatingUserRoleItSucceeds()
    {
        // Arrange
        var (org, owner, context, service) = await SetupTestOrganizationAsync();

        var member = new ApplicationUser { UserName = "member@test.com", Email = "member@test.com" };
        await CreateUserManager(context).CreateAsync(member, "Pass123!");

        var membership = new OrganizationMember
        {
            UserId = member.Id,
            OrganizationId = org.Id,
            Role = Role.Member,
            IsAccepted = true
        };
        context.OrganizationMembers.Add(membership);
        await context.SaveChangesAsync();

        var updateRequest = new UpdateRoleRequest
        {
            UserId = member.Id,
            Role = "Admin"
        };

        // Act
        var success = await service.UpdateRoleAsync(org.Id, updateRequest);

        // Assert
        Assert.True(success);

        var updatedMembership = await context.OrganizationMembers
            .FirstOrDefaultAsync(m => m.UserId == member.Id);

        Assert.NotNull(updatedMembership);
        Assert.Equal(Role.Admin, updatedMembership.Role);

        await context.DisposeAsync();
    }

    [Fact]
    public async Task WhenChangingOwnerRoleItFails()
    {
        // Arrange
        var (org, owner, context, service) = await SetupTestOrganizationAsync();

        var updateRequest = new UpdateRoleRequest
        {
            UserId = owner.Id,
            Role = "Member"
        };

        // Act
        var success = await service.UpdateRoleAsync(org.Id, updateRequest);

        // Assert
        Assert.False(success);

        var membership = await context.OrganizationMembers
            .FirstOrDefaultAsync(m => m.UserId == owner.Id);

        Assert.NotNull(membership);
        Assert.Equal(Role.Owner, membership.Role);

        await context.DisposeAsync();
    }

    [Fact]
    public async Task WhenRemovingMemberItSucceeds()
    {
        // Arrange
        var (org, owner, context, service) = await SetupTestOrganizationAsync();

        var member = new ApplicationUser { UserName = "removeme@test.com", Email = "removeme@test.com" };
        await CreateUserManager(context).CreateAsync(member, "Pass123!");

        var membership = new OrganizationMember
        {
            UserId = member.Id,
            OrganizationId = org.Id,
            Role = Role.Member,
            IsAccepted = true
        };
        context.OrganizationMembers.Add(membership);
        await context.SaveChangesAsync();

        // Act
        var success = await service.RemoveMemberAsync(org.Id, member.Id);

        // Assert
        Assert.True(success);

        var removedMembership = await context.OrganizationMembers
            .FirstOrDefaultAsync(m => m.UserId == member.Id);

        Assert.Null(removedMembership);

        await context.DisposeAsync();
    }

    [Fact]
    public async Task WhenRemovingOwnerItFails()
    {
        // Arrange
        var (org, owner, context, service) = await SetupTestOrganizationAsync();

        // Act
        var success = await service.RemoveMemberAsync(org.Id, owner.Id);

        // Assert
        Assert.False(success);

        var membership = await context.OrganizationMembers
            .FirstOrDefaultAsync(m => m.UserId == owner.Id);

        Assert.NotNull(membership);

        await context.DisposeAsync();
    }
}
