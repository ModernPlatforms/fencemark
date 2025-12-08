using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace fencemark.ApiService.Data;

/// <summary>
/// Factory for creating ApplicationDbContext instances at design time (for migrations)
/// </summary>
public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        
        // Use SQLite for design-time operations
        optionsBuilder.UseSqlite("Data Source=fencemark.db");

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
