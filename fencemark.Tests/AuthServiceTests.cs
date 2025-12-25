using fencemark.ApiService.Data;
using fencemark.ApiService.Data.Models;
using fencemark.ApiService.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace fencemark.Tests;

public class AuthServiceTests
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

        // Create service collection for token providers
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDataProtection();
        var serviceProvider = services.BuildServiceProvider();

        var userManager = new UserManager<ApplicationUser>(
            store, options, passwordHasher, userValidators, passwordValidators,
            keyNormalizer, errors, serviceProvider, logger);

        // Add default token providers
        var dataProtectionProvider = serviceProvider.GetRequiredService<Microsoft.AspNetCore.DataProtection.IDataProtectionProvider>();
        var tokenProvider = new DataProtectorTokenProvider<ApplicationUser>(
            dataProtectionProvider,
            Options.Create(new DataProtectionTokenProviderOptions()),
            new Logger<DataProtectorTokenProvider<ApplicationUser>>(new LoggerFactory()));

        userManager.RegisterTokenProvider(TokenOptions.DefaultProvider, tokenProvider);
        userManager.RegisterTokenProvider(TokenOptions.DefaultEmailProvider, tokenProvider);
        userManager.RegisterTokenProvider(TokenOptions.DefaultPhoneProvider, tokenProvider);

        return userManager;
    }

    private static SignInManager<ApplicationUser> CreateSignInManager(
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext context)
    {
        var contextAccessor = new Microsoft.AspNetCore.Http.HttpContextAccessor();
        var claimsFactory = new Microsoft.AspNetCore.Identity.UserClaimsPrincipalFactory<ApplicationUser>(
            userManager,
            Options.Create(new IdentityOptions()));
        var options = Options.Create(new IdentityOptions());
        var logger = new Logger<SignInManager<ApplicationUser>>(new LoggerFactory());

        return new SignInManager<ApplicationUser>(
            userManager, contextAccessor, claimsFactory, options, logger, null!, null);
    }

    private static ISeedDataService CreateMockSeedDataService()
    {
        // Return a mock implementation that does nothing
        return new MockSeedDataService();
    }

    private class MockSeedDataService : ISeedDataService
    {
        public Task SeedSampleDataAsync(string organizationId) => Task.CompletedTask;
        public Task<bool> HasSampleDataAsync(string organizationId) => Task.FromResult(false);
    }

    [Fact]
    public async Task WhenNewUserRegistersOrganizationIsAutomaticallyCreated()
    {
        // Arrange
        await using var context = CreateDbContext();
        var userManager = CreateUserManager(context);
        var signInManager = CreateSignInManager(userManager, context);
        var seedDataService = CreateMockSeedDataService();
        var authService = new AuthService(userManager, signInManager, context, seedDataService);

        var request = new RegisterRequest
        {
            Email = "test@example.com",
            Password = "Password123!",
            OrganizationName = "Test Company"
        };

        // Act
        var response = await authService.RegisterAsync(request);

        // Assert
        Assert.True(response.Success);
        Assert.NotNull(response.UserId);
        Assert.NotNull(response.OrganizationId);
        Assert.Equal("test@example.com", response.Email);
        Assert.True(response.IsGuest);

        var organization = await context.Organizations.FindAsync(response.OrganizationId);
        Assert.NotNull(organization);
        Assert.Equal("Test Company", organization.Name);
    }

    [Fact]
    public async Task WhenNewUserRegistersTheyAreSetAsOwnerOfOrganization()
    {
        // Arrange
        await using var context = CreateDbContext();
        var userManager = CreateUserManager(context);
        var signInManager = CreateSignInManager(userManager, context);        var seedDataService = CreateMockSeedDataService();
        var authService = new AuthService(userManager, signInManager, context, seedDataService);

        var request = new RegisterRequest
        {
            Email = "owner@example.com",
            Password = "SecurePass123!",
            OrganizationName = "Owner Company"
        };

        // Act
        var response = await authService.RegisterAsync(request);

        // Assert
        Assert.True(response.Success);

        var membership = await context.OrganizationMembers
            .FirstOrDefaultAsync(m => m.UserId == response.UserId && m.OrganizationId == response.OrganizationId);

        Assert.NotNull(membership);
        Assert.Equal(Role.Owner, membership.Role);
        Assert.True(membership.IsAccepted);
    }

    [Fact]
    public async Task WhenNewUserRegistersTheyAreMarkedAsGuest()
    {
        // Arrange
        await using var context = CreateDbContext();
        var userManager = CreateUserManager(context);
        var signInManager = CreateSignInManager(userManager, context);        var seedDataService = CreateMockSeedDataService();
        var authService = new AuthService(userManager, signInManager, context, seedDataService);

        var request = new RegisterRequest
        {
            Email = "guest@example.com",
            Password = "TestPass123!",
            OrganizationName = "Guest Company"
        };

        // Act
        var response = await authService.RegisterAsync(request);

        // Assert
        Assert.True(response.Success);
        Assert.True(response.IsGuest);

        var user = await userManager.FindByIdAsync(response.UserId!);
        Assert.NotNull(user);
        Assert.True(user.IsGuest);
        Assert.False(user.IsEmailVerified);
    }

    [Fact]
    public async Task WhenUserEmailIsVerifiedGuestStatusIsRemoved()
    {
        // Arrange
        await using var context = CreateDbContext();
        var userManager = CreateUserManager(context);
        var signInManager = CreateSignInManager(userManager, context);        var seedDataService = CreateMockSeedDataService();
        var authService = new AuthService(userManager, signInManager, context, seedDataService);

        var request = new RegisterRequest
        {
            Email = "verify@example.com",
            Password = "VerifyPass123!",
            OrganizationName = "Verify Company"
        };

        var registerResponse = await authService.RegisterAsync(request);
        var user = await userManager.FindByIdAsync(registerResponse.UserId!);
        var token = await userManager.GenerateEmailConfirmationTokenAsync(user!);

        // Act
        var verified = await authService.VerifyEmailAsync(registerResponse.UserId!, token);

        // Assert
        Assert.True(verified);

        var updatedUser = await userManager.FindByIdAsync(registerResponse.UserId!);
        Assert.NotNull(updatedUser);
        Assert.False(updatedUser.IsGuest);
        Assert.True(updatedUser.IsEmailVerified);
    }

    [Fact]
    public async Task WhenDuplicateEmailRegistersRegistrationFails()
    {
        // Arrange
        await using var context = CreateDbContext();
        var userManager = CreateUserManager(context);
        var signInManager = CreateSignInManager(userManager, context);        var seedDataService = CreateMockSeedDataService();
        var authService = new AuthService(userManager, signInManager, context, seedDataService);

        var request = new RegisterRequest
        {
            Email = "duplicate@example.com",
            Password = "Password123!",
            OrganizationName = "First Company"
        };

        await authService.RegisterAsync(request);

        var duplicateRequest = new RegisterRequest
        {
            Email = "duplicate@example.com",
            Password = "Password456!",
            OrganizationName = "Second Company"
        };

        // Act
        var response = await authService.RegisterAsync(duplicateRequest);

        // Assert
        Assert.False(response.Success);
        Assert.Contains("already exists", response.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task WhenInvalidPasswordProvidedRegistrationFails()
    {
        // Arrange
        await using var context = CreateDbContext();
        var userManager = CreateUserManager(context);
        var signInManager = CreateSignInManager(userManager, context);        var seedDataService = CreateMockSeedDataService();
        var authService = new AuthService(userManager, signInManager, context, seedDataService);

        var request = new RegisterRequest
        {
            Email = "weak@example.com",
            Password = "weak", // Too weak password
            OrganizationName = "Test Company"
        };

        // Act
        var response = await authService.RegisterAsync(request);

        // Assert
        Assert.False(response.Success);
        Assert.NotNull(response.Message);
    }

    [Fact]
    public async Task WhenExistingUserWithoutOrganizationLogsInViaExternalProviderOrganizationIsCreated()
    {
        // Arrange
        await using var context = CreateDbContext();
        var userManager = CreateUserManager(context);
        var signInManager = CreateSignInManager(userManager, context);
        var seedDataService = CreateMockSeedDataService();
        var authService = new AuthService(userManager, signInManager, context, seedDataService);

        // Create a user without an organization (simulating deleted organization membership)
        var user = new ApplicationUser
        {
            UserName = "orphan@example.com",
            Email = "orphan@example.com",
            IsGuest = true,
            IsEmailVerified = false,
            CreatedAt = DateTime.UtcNow
        };
        await userManager.CreateAsync(user, "Password123!");

        // Verify user has no organization membership
        var membershipBefore = await context.OrganizationMembers
            .FirstOrDefaultAsync(m => m.UserId == user.Id);
        Assert.Null(membershipBefore);

        var request = new ExternalLoginRequest
        {
            Email = "orphan@example.com",
            ExternalId = "ext-123",
            Provider = "AzureAD",
            OrganizationName = "Auto Created Org"
        };

        // Act
        var response = await authService.ExternalLoginAsync(request);

        // Assert
        Assert.True(response.Success);
        Assert.NotNull(response.OrganizationId);
        Assert.Equal("Auto Created Org", response.OrganizationName);

        // Verify organization was created
        var organization = await context.Organizations.FindAsync(response.OrganizationId);
        Assert.NotNull(organization);
        Assert.Equal("Auto Created Org", organization.Name);

        // Verify user is now a member with Owner role
        var membership = await context.OrganizationMembers
            .FirstOrDefaultAsync(m => m.UserId == user.Id && m.OrganizationId == response.OrganizationId);
        Assert.NotNull(membership);
        Assert.Equal(Role.Owner, membership.Role);
        Assert.True(membership.IsAccepted);
    }

    [Fact]
    public async Task WhenNewUserLogsInViaExternalProviderOrganizationIsCreated()
    {
        // Arrange
        await using var context = CreateDbContext();
        var userManager = CreateUserManager(context);
        var signInManager = CreateSignInManager(userManager, context);
        var seedDataService = CreateMockSeedDataService();
        var authService = new AuthService(userManager, signInManager, context, seedDataService);

        var request = new ExternalLoginRequest
        {
            Email = "newuser@example.com",
            ExternalId = "ext-456",
            Provider = "AzureAD",
            OrganizationName = "New User Org"
        };

        // Act
        var response = await authService.ExternalLoginAsync(request);

        // Assert
        Assert.True(response.Success);
        Assert.NotNull(response.UserId);
        Assert.NotNull(response.OrganizationId);
        Assert.Equal("New User Org", response.OrganizationName);

        // Verify organization was created
        var organization = await context.Organizations.FindAsync(response.OrganizationId);
        Assert.NotNull(organization);
        Assert.Equal("New User Org", organization.Name);

        // Verify user was created and is a member
        var user = await userManager.FindByIdAsync(response.UserId!);
        Assert.NotNull(user);
        Assert.Equal("newuser@example.com", user.Email);
        Assert.False(user.IsGuest);
        Assert.True(user.IsEmailVerified);

        var membership = await context.OrganizationMembers
            .FirstOrDefaultAsync(m => m.UserId == user.Id && m.OrganizationId == response.OrganizationId);
        Assert.NotNull(membership);
        Assert.Equal(Role.Owner, membership.Role);
    }

    [Fact]
    public async Task WhenExistingUserWithOrganizationLogsInViaExternalProviderExistingOrganizationIsUsed()
    {
        // Arrange
        await using var context = CreateDbContext();
        var userManager = CreateUserManager(context);
        var signInManager = CreateSignInManager(userManager, context);
        var seedDataService = CreateMockSeedDataService();
        var authService = new AuthService(userManager, signInManager, context, seedDataService);

        // Create a user with an organization
        var user = new ApplicationUser
        {
            UserName = "existing@example.com",
            Email = "existing@example.com",
            IsGuest = false,
            IsEmailVerified = true,
            CreatedAt = DateTime.UtcNow
        };
        await userManager.CreateAsync(user, "Password123!");

        var organization = new Organization
        {
            Name = "Existing Org",
            CreatedAt = DateTime.UtcNow
        };
        context.Organizations.Add(organization);

        var membership = new OrganizationMember
        {
            UserId = user.Id,
            OrganizationId = organization.Id,
            Role = Role.Owner,
            JoinedAt = DateTime.UtcNow,
            IsAccepted = true
        };
        context.OrganizationMembers.Add(membership);
        await context.SaveChangesAsync();

        var request = new ExternalLoginRequest
        {
            Email = "existing@example.com",
            ExternalId = "ext-789",
            Provider = "AzureAD",
            OrganizationName = "Ignored New Org Name"
        };

        // Act
        var response = await authService.ExternalLoginAsync(request);

        // Assert
        Assert.True(response.Success);
        Assert.Equal(organization.Id, response.OrganizationId);
        Assert.Equal("Existing Org", response.OrganizationName);

        // Verify no new organization was created
        var orgCount = await context.Organizations.CountAsync();
        Assert.Equal(1, orgCount);
    }
}

