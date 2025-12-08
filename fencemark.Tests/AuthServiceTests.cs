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
        options.Value.Tokens.EmailConfirmationTokenProvider = "Default";
        var passwordHasher = new PasswordHasher<ApplicationUser>();
        var userValidators = new List<IUserValidator<ApplicationUser>> { new UserValidator<ApplicationUser>() };
        var passwordValidators = new List<IPasswordValidator<ApplicationUser>> { new PasswordValidator<ApplicationUser>() };
        var keyNormalizer = new UpperInvariantLookupNormalizer();
        var errors = new IdentityErrorDescriber();
        var logger = new Logger<UserManager<ApplicationUser>>(new LoggerFactory());

        var userManager = new UserManager<ApplicationUser>(
            store, options, passwordHasher, userValidators, passwordValidators,
            keyNormalizer, errors, null!, logger);

        // Register a real token provider for 'Default'
        userManager.RegisterTokenProvider("Default", new EmailTokenProvider<ApplicationUser>());

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

    [Fact]
    public async Task WhenNewUserRegistersOrganizationIsAutomaticallyCreated()
    {
        // Arrange
        await using var context = CreateDbContext();
        var userManager = CreateUserManager(context);
        var signInManager = CreateSignInManager(userManager, context);
        var authService = new AuthService(userManager, signInManager, context);

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
        var signInManager = CreateSignInManager(userManager, context);
        var authService = new AuthService(userManager, signInManager, context);

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
        var signInManager = CreateSignInManager(userManager, context);
        var authService = new AuthService(userManager, signInManager, context);

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
        var signInManager = CreateSignInManager(userManager, context);
        var authService = new AuthService(userManager, signInManager, context);

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
        var signInManager = CreateSignInManager(userManager, context);
        var authService = new AuthService(userManager, signInManager, context);

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
        var signInManager = CreateSignInManager(userManager, context);
        var authService = new AuthService(userManager, signInManager, context);

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
}
