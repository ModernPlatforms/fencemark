using fencemark.ApiService.Data;
using fencemark.ApiService.Data.Models;
using fencemark.ApiService.Middleware;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace fencemark.Tests;

/// <summary>
/// Performance benchmarks for RLS and data isolation
/// These tests ensure that the filtering mechanisms don't significantly impact performance
/// </summary>
public class RlsPerformanceBenchmarks
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

    private async Task<ApplicationDbContext> SeedLargeDataset(int organizationCount = 10, int itemsPerOrg = 100)
    {
        var context = CreateDbContext();

        // Create organizations
        var organizations = Enumerable.Range(1, organizationCount)
            .Select(i => new Organization { Id = $"org{i}", Name = $"Organization {i}" })
            .ToList();
        context.Organizations.AddRange(organizations);

        // Create components for each organization
        foreach (var org in organizations)
        {
            var components = Enumerable.Range(1, itemsPerOrg)
                .Select(i => new Component
                {
                    Id = $"{org.Id}-comp{i}",
                    Name = $"{org.Name} Component {i}",
                    Category = "Post",
                    OrganizationId = org.Id,
                    UnitPrice = 10.00m + i
                })
                .ToList();
            context.Components.AddRange(components);

            var jobs = Enumerable.Range(1, itemsPerOrg)
                .Select(i => new Job
                {
                    Id = $"{org.Id}-job{i}",
                    Name = $"{org.Name} Job {i}",
                    CustomerName = $"Customer {i}",
                    OrganizationId = org.Id
                })
                .ToList();
            context.Jobs.AddRange(jobs);
        }

        await context.SaveChangesAsync();
        return context;
    }

    [Fact]
    public async Task Benchmark_FilteredQuery_WithSmallDataset()
    {
        // Arrange
        var context = await SeedLargeDataset(organizationCount: 5, itemsPerOrg: 50);
        var targetOrgId = "org3";
        var stopwatch = new Stopwatch();

        // Act
        stopwatch.Start();
        var components = await context.Components
            .Where(c => c.OrganizationId == targetOrgId)
            .ToListAsync();
        stopwatch.Stop();

        // Assert
        Assert.Equal(50, components.Count);
        
        // Performance assertion - should complete in under 100ms for small dataset
        Assert.True(stopwatch.ElapsedMilliseconds < 100, 
            $"Query took {stopwatch.ElapsedMilliseconds}ms, expected < 100ms");

        await context.DisposeAsync();
    }

    [Fact]
    public async Task Benchmark_FilteredQuery_WithLargeDataset()
    {
        // Arrange
        var context = await SeedLargeDataset(organizationCount: 20, itemsPerOrg: 200);
        var targetOrgId = "org10";
        var stopwatch = new Stopwatch();

        // Act
        stopwatch.Start();
        var components = await context.Components
            .Where(c => c.OrganizationId == targetOrgId)
            .ToListAsync();
        stopwatch.Stop();

        // Assert
        Assert.Equal(200, components.Count);
        
        // Performance assertion - should complete in under 500ms even with larger dataset
        Assert.True(stopwatch.ElapsedMilliseconds < 500, 
            $"Query took {stopwatch.ElapsedMilliseconds}ms, expected < 500ms");

        await context.DisposeAsync();
    }

    [Fact]
    public async Task Benchmark_MultipleSequentialQueries()
    {
        // Arrange
        var context = await SeedLargeDataset(organizationCount: 10, itemsPerOrg: 100);
        var targetOrgId = "org5";
        var stopwatch = new Stopwatch();

        // Act - Simulate a typical request that queries multiple tables
        stopwatch.Start();
        
        var components = await context.Components
            .Where(c => c.OrganizationId == targetOrgId)
            .ToListAsync();
        
        var jobs = await context.Jobs
            .Where(j => j.OrganizationId == targetOrgId)
            .ToListAsync();
        
        var fenceTypes = await context.FenceTypes
            .Where(f => f.OrganizationId == targetOrgId)
            .ToListAsync();
        
        stopwatch.Stop();

        // Assert
        Assert.Equal(100, components.Count);
        Assert.Equal(100, jobs.Count);
        
        // Performance assertion - multiple queries should complete quickly
        Assert.True(stopwatch.ElapsedMilliseconds < 300, 
            $"Multiple queries took {stopwatch.ElapsedMilliseconds}ms, expected < 300ms");

        await context.DisposeAsync();
    }

    [Fact]
    public async Task Benchmark_ComplexQueryWithJoins()
    {
        // Arrange
        var context = await SeedLargeDataset(organizationCount: 5, itemsPerOrg: 50);
        
        // Add related data
        var targetOrgId = "org3";
        var jobs = await context.Jobs
            .Where(j => j.OrganizationId == targetOrgId)
            .Take(10)
            .ToListAsync();

        foreach (var job in jobs)
        {
            context.Quotes.Add(new Quote
            {
                Id = $"quote-{job.Id}",
                JobId = job.Id,
                OrganizationId = targetOrgId,
                QuoteNumber = $"Q-{job.Id}"
            });
        }
        await context.SaveChangesAsync();

        var stopwatch = new Stopwatch();

        // Act - Complex query with joins
        stopwatch.Start();
        var quotesWithJobs = await context.Quotes
            .Where(q => q.OrganizationId == targetOrgId)
            .Include(q => q.Job)
            .Include(q => q.BillOfMaterials)
            .ToListAsync();
        stopwatch.Stop();

        // Assert
        Assert.Equal(10, quotesWithJobs.Count);
        Assert.All(quotesWithJobs, q => Assert.NotNull(q.Job));
        
        // Performance assertion - complex queries should still be reasonably fast
        Assert.True(stopwatch.ElapsedMilliseconds < 200, 
            $"Complex query took {stopwatch.ElapsedMilliseconds}ms, expected < 200ms");

        await context.DisposeAsync();
    }

    [Fact]
    public async Task Benchmark_CountQuery_AcrossLargeDataset()
    {
        // Arrange
        var context = await SeedLargeDataset(organizationCount: 50, itemsPerOrg: 100);
        var targetOrgId = "org25";
        var stopwatch = new Stopwatch();

        // Act - Count queries should be optimized
        stopwatch.Start();
        var componentCount = await context.Components
            .CountAsync(c => c.OrganizationId == targetOrgId);
        var jobCount = await context.Jobs
            .CountAsync(j => j.OrganizationId == targetOrgId);
        stopwatch.Stop();

        // Assert
        Assert.Equal(100, componentCount);
        Assert.Equal(100, jobCount);
        
        // Performance assertion - count queries should be very fast
        Assert.True(stopwatch.ElapsedMilliseconds < 100, 
            $"Count queries took {stopwatch.ElapsedMilliseconds}ms, expected < 100ms");

        await context.DisposeAsync();
    }

    [Fact]
    public async Task Benchmark_PagedQuery()
    {
        // Arrange
        var context = await SeedLargeDataset(organizationCount: 10, itemsPerOrg: 500);
        var targetOrgId = "org5";
        var pageSize = 20;
        var stopwatch = new Stopwatch();

        // Act - Paged query (typical for list views)
        stopwatch.Start();
        var pagedResults = await context.Components
            .Where(c => c.OrganizationId == targetOrgId)
            .OrderBy(c => c.Name)
            .Skip(0)
            .Take(pageSize)
            .ToListAsync();
        stopwatch.Stop();

        // Assert
        Assert.Equal(pageSize, pagedResults.Count);
        
        // Performance assertion - paged queries should be fast
        Assert.True(stopwatch.ElapsedMilliseconds < 150, 
            $"Paged query took {stopwatch.ElapsedMilliseconds}ms, expected < 150ms");

        await context.DisposeAsync();
    }

    [Fact]
    public async Task PerformanceComparison_WithAndWithoutFiltering()
    {
        // This test compares query performance to ensure filtering overhead is minimal
        
        // Arrange
        var context = await SeedLargeDataset(organizationCount: 10, itemsPerOrg: 200);
        var targetOrgId = "org5";

        // Act 1 - Query without explicit filtering (gets all)
        var sw1 = Stopwatch.StartNew();
        var allComponents = await context.Components.ToListAsync();
        sw1.Stop();

        // Act 2 - Query with filtering
        var sw2 = Stopwatch.StartNew();
        var filteredComponents = await context.Components
            .Where(c => c.OrganizationId == targetOrgId)
            .ToListAsync();
        sw2.Stop();

        // Assert
        Assert.Equal(2000, allComponents.Count); // 10 orgs * 200 items
        Assert.Equal(200, filteredComponents.Count);
        
        // Filtered query should actually be FASTER due to reduced data transfer
        // But we'll be lenient and just ensure it's not significantly slower
        var overhead = (double)sw2.ElapsedMilliseconds / sw1.ElapsedMilliseconds;
        Assert.True(overhead < 1.5, 
            $"Filtering overhead is {overhead:P0}, expected < 50%");
    }
}
