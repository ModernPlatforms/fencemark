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

        var cs = Environment.GetEnvironmentVariable("ConnectionStrings__fencemark")
                 ?? "Server=localhost;Database=fencemark;Trusted_Connection=True;TrustServerCertificate=True;";

        optionsBuilder.UseSqlServer(cs);

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
