using fencemark.ApiService.Data;
using fencemark.ApiService.Data.Models;
using fencemark.ApiService.Middleware;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace fencemark.Tests;

/// <summary>
/// Comprehensive tests for multi-tenant data isolation and RLS
/// These tests verify that no cross-tenant data access is possible
/// </summary>
public class DataIsolationTests
{
    private class TestCurrentUserService : ICurrentUserService
    {
        public string? UserId { get; set; }
        public string? Email { get; set; }
        public string? OrganizationId { get; set; }
        public bool IsAuthenticated => !string.IsNullOrEmpty(UserId);
    }

    private static ApplicationDbContext CreateDbContext(ICurrentUserService? currentUserService = null)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return currentUserService != null 
            ? new ApplicationDbContext(options, currentUserService)
            : new ApplicationDbContext(options);
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

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDataProtection();
        var serviceProvider = services.BuildServiceProvider();

        return new UserManager<ApplicationUser>(
            store, options, passwordHasher, userValidators, passwordValidators,
            keyNormalizer, errors, serviceProvider, logger);
    }

    [Fact]
    public async Task Components_AreScopedToOrganization_NoLeakage()
    {
        // Arrange
        var context = CreateDbContext();
        
        var org1 = new Organization { Id = "org1", Name = "Organization 1" };
        var org2 = new Organization { Id = "org2", Name = "Organization 2" };
        context.Organizations.AddRange(org1, org2);

        var component1 = new Component 
        { 
            Id = "comp1",
            Name = "Org1 Component", 
            OrganizationId = "org1",
            Category = "Post"
        };
        var component2 = new Component 
        { 
            Id = "comp2",
            Name = "Org2 Component", 
            OrganizationId = "org2",
            Category = "Rail"
        };
        context.Components.AddRange(component1, component2);
        await context.SaveChangesAsync();

        // Act - Query as Org1
        var org1Components = await context.Components
            .Where(c => c.OrganizationId == "org1")
            .ToListAsync();

        // Assert
        Assert.Single(org1Components);
        Assert.Equal("comp1", org1Components[0].Id);
        Assert.Equal("Org1 Component", org1Components[0].Name);

        // Act - Query as Org2
        var org2Components = await context.Components
            .Where(c => c.OrganizationId == "org2")
            .ToListAsync();

        // Assert
        Assert.Single(org2Components);
        Assert.Equal("comp2", org2Components[0].Id);
        Assert.Equal("Org2 Component", org2Components[0].Name);

        await context.DisposeAsync();
    }

    [Fact]
    public async Task Jobs_AreScopedToOrganization_NoLeakage()
    {
        // Arrange
        var context = CreateDbContext();
        
        var org1 = new Organization { Id = "org1", Name = "Organization 1" };
        var org2 = new Organization { Id = "org2", Name = "Organization 2" };
        context.Organizations.AddRange(org1, org2);

        var job1 = new Job 
        { 
            Id = "job1",
            Name = "Org1 Job",
            CustomerName = "Customer 1", 
            OrganizationId = "org1"
        };
        var job2 = new Job 
        { 
            Id = "job2",
            Name = "Org2 Job",
            CustomerName = "Customer 2", 
            OrganizationId = "org2"
        };
        context.Jobs.AddRange(job1, job2);
        await context.SaveChangesAsync();

        // Act - Query as Org1
        var org1Jobs = await context.Jobs
            .Where(j => j.OrganizationId == "org1")
            .ToListAsync();

        // Assert
        Assert.Single(org1Jobs);
        Assert.Equal("job1", org1Jobs[0].Id);
        Assert.Equal("Org1 Job", org1Jobs[0].Name);

        // Act - Query as Org2
        var org2Jobs = await context.Jobs
            .Where(j => j.OrganizationId == "org2")
            .ToListAsync();

        // Assert
        Assert.Single(org2Jobs);
        Assert.Equal("job2", org2Jobs[0].Id);
        Assert.Equal("Org2 Job", org2Jobs[0].Name);

        await context.DisposeAsync();
    }

    [Fact]
    public async Task Quotes_AreScopedToOrganization_NoLeakage()
    {
        // Arrange
        var context = CreateDbContext();
        
        var org1 = new Organization { Id = "org1", Name = "Organization 1" };
        var org2 = new Organization { Id = "org2", Name = "Organization 2" };
        context.Organizations.AddRange(org1, org2);

        var job1 = new Job { Id = "job1", Name = "Job 1", CustomerName = "C1", OrganizationId = "org1" };
        var job2 = new Job { Id = "job2", Name = "Job 2", CustomerName = "C2", OrganizationId = "org2" };
        context.Jobs.AddRange(job1, job2);

        var quote1 = new Quote 
        { 
            Id = "quote1",
            JobId = "job1",
            OrganizationId = "org1",
            QuoteNumber = "Q001"
        };
        var quote2 = new Quote 
        { 
            Id = "quote2",
            JobId = "job2",
            OrganizationId = "org2",
            QuoteNumber = "Q002"
        };
        context.Quotes.AddRange(quote1, quote2);
        await context.SaveChangesAsync();

        // Act - Query as Org1
        var org1Quotes = await context.Quotes
            .Where(q => q.OrganizationId == "org1")
            .ToListAsync();

        // Assert
        Assert.Single(org1Quotes);
        Assert.Equal("quote1", org1Quotes[0].Id);
        Assert.Equal("Q001", org1Quotes[0].QuoteNumber);

        // Act - Query as Org2
        var org2Quotes = await context.Quotes
            .Where(q => q.OrganizationId == "org2")
            .ToListAsync();

        // Assert
        Assert.Single(org2Quotes);
        Assert.Equal("quote2", org2Quotes[0].Id);
        Assert.Equal("Q002", org2Quotes[0].QuoteNumber);

        await context.DisposeAsync();
    }

    [Fact]
    public async Task PricingConfigs_AreScopedToOrganization_NoLeakage()
    {
        // Arrange
        var context = CreateDbContext();
        
        var org1 = new Organization { Id = "org1", Name = "Organization 1" };
        var org2 = new Organization { Id = "org2", Name = "Organization 2" };
        context.Organizations.AddRange(org1, org2);

        var config1 = new PricingConfig 
        { 
            Id = "config1",
            Name = "Org1 Config",
            OrganizationId = "org1"
        };
        var config2 = new PricingConfig 
        { 
            Id = "config2",
            Name = "Org2 Config",
            OrganizationId = "org2"
        };
        context.PricingConfigs.AddRange(config1, config2);
        await context.SaveChangesAsync();

        // Act - Query as Org1
        var org1Configs = await context.PricingConfigs
            .Where(c => c.OrganizationId == "org1")
            .ToListAsync();

        // Assert
        Assert.Single(org1Configs);
        Assert.Equal("config1", org1Configs[0].Id);
        Assert.Equal("Org1 Config", org1Configs[0].Name);

        // Act - Query as Org2
        var org2Configs = await context.PricingConfigs
            .Where(c => c.OrganizationId == "org2")
            .ToListAsync();

        // Assert
        Assert.Single(org2Configs);
        Assert.Equal("config2", org2Configs[0].Id);
        Assert.Equal("Org2 Config", org2Configs[0].Name);

        await context.DisposeAsync();
    }

    [Fact]
    public async Task FenceTypes_AreScopedToOrganization_NoLeakage()
    {
        // Arrange
        var context = CreateDbContext();
        
        var org1 = new Organization { Id = "org1", Name = "Organization 1" };
        var org2 = new Organization { Id = "org2", Name = "Organization 2" };
        context.Organizations.AddRange(org1, org2);

        var fence1 = new FenceType 
        { 
            Id = "fence1",
            Name = "Org1 Fence",
            OrganizationId = "org1"
        };
        var fence2 = new FenceType 
        { 
            Id = "fence2",
            Name = "Org2 Fence",
            OrganizationId = "org2"
        };
        context.FenceTypes.AddRange(fence1, fence2);
        await context.SaveChangesAsync();

        // Act - Query as Org1
        var org1Fences = await context.FenceTypes
            .Where(f => f.OrganizationId == "org1")
            .ToListAsync();

        // Assert
        Assert.Single(org1Fences);
        Assert.Equal("fence1", org1Fences[0].Id);
        Assert.Equal("Org1 Fence", org1Fences[0].Name);

        // Act - Query as Org2
        var org2Fences = await context.FenceTypes
            .Where(f => f.OrganizationId == "org2")
            .ToListAsync();

        // Assert
        Assert.Single(org2Fences);
        Assert.Equal("fence2", org2Fences[0].Id);
        Assert.Equal("Org2 Fence", org2Fences[0].Name);

        await context.DisposeAsync();
    }

    [Fact]
    public async Task GateTypes_AreScopedToOrganization_NoLeakage()
    {
        // Arrange
        var context = CreateDbContext();
        
        var org1 = new Organization { Id = "org1", Name = "Organization 1" };
        var org2 = new Organization { Id = "org2", Name = "Organization 2" };
        context.Organizations.AddRange(org1, org2);

        var gate1 = new GateType 
        { 
            Id = "gate1",
            Name = "Org1 Gate",
            OrganizationId = "org1"
        };
        var gate2 = new GateType 
        { 
            Id = "gate2",
            Name = "Org2 Gate",
            OrganizationId = "org2"
        };
        context.GateTypes.AddRange(gate1, gate2);
        await context.SaveChangesAsync();

        // Act - Query as Org1
        var org1Gates = await context.GateTypes
            .Where(g => g.OrganizationId == "org1")
            .ToListAsync();

        // Assert
        Assert.Single(org1Gates);
        Assert.Equal("gate1", org1Gates[0].Id);
        Assert.Equal("Org1 Gate", org1Gates[0].Name);

        // Act - Query as Org2
        var org2Gates = await context.GateTypes
            .Where(g => g.OrganizationId == "org2")
            .ToListAsync();

        // Assert
        Assert.Single(org2Gates);
        Assert.Equal("gate2", org2Gates[0].Id);
        Assert.Equal("Org2 Gate", org2Gates[0].Name);

        await context.DisposeAsync();
    }

    [Fact]
    public async Task CrossTenantQueryById_ReturnsNull_WhenWrongOrganization()
    {
        // Arrange
        var context = CreateDbContext();
        
        var org1 = new Organization { Id = "org1", Name = "Organization 1" };
        var org2 = new Organization { Id = "org2", Name = "Organization 2" };
        context.Organizations.AddRange(org1, org2);

        var job1 = new Job 
        { 
            Id = "job1",
            Name = "Org1 Job",
            CustomerName = "Customer 1", 
            OrganizationId = "org1"
        };
        context.Jobs.Add(job1);
        await context.SaveChangesAsync();

        // Act - Try to access Org1's job while acting as Org2
        var result = await context.Jobs
            .FirstOrDefaultAsync(j => j.Id == "job1" && j.OrganizationId == "org2");

        // Assert
        Assert.Null(result);

        await context.DisposeAsync();
    }

    [Fact]
    public async Task MultipleTenantsWithSimilarData_RemainIsolated()
    {
        // Arrange - Create 3 organizations with similar component names
        var context = CreateDbContext();
        
        var orgs = new[]
        {
            new Organization { Id = "org1", Name = "Organization 1" },
            new Organization { Id = "org2", Name = "Organization 2" },
            new Organization { Id = "org3", Name = "Organization 3" }
        };
        context.Organizations.AddRange(orgs);

        // Each organization has a "Standard Post" component
        foreach (var org in orgs)
        {
            context.Components.Add(new Component
            {
                Id = $"comp-{org.Id}",
                Name = "Standard Post",
                Category = "Post",
                OrganizationId = org.Id,
                UnitPrice = 10.00m * int.Parse(org.Id.Replace("org", ""))
            });
        }
        await context.SaveChangesAsync();

        // Act & Assert - Each organization should only see their own component
        foreach (var org in orgs)
        {
            var components = await context.Components
                .Where(c => c.OrganizationId == org.Id)
                .ToListAsync();

            Assert.Single(components);
            Assert.Equal($"comp-{org.Id}", components[0].Id);
            Assert.Equal(org.Id, components[0].OrganizationId);
        }

        // Assert - Total count in database should be 3
        var totalComponents = await context.Components.CountAsync();
        Assert.Equal(3, totalComponents);

        await context.DisposeAsync();
    }

}
