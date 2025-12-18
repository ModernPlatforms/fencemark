using fencemark.ApiService.Data;
using Microsoft.EntityFrameworkCore;

namespace fencemark.ApiService.Infrastructure;

/// <summary>
/// Applies EF Core migrations when the API service starts.
/// Includes retries so it works even when the SQL container is still warming up under Aspire.
/// </summary>
public sealed class DatabaseMigrationHostedService : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DatabaseMigrationHostedService> _logger;

    private const int MaxAttempts = 10;
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(5);
    
    public static bool MigrationsCompleted { get; private set; }

    public DatabaseMigrationHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<DatabaseMigrationHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var cs = db.Database.GetDbConnection().ConnectionString;

                _logger.LogInformation("[ApiService] Applying EF Core migrations (attempt {Attempt}/{MaxAttempts}) using connection: {Connection}", attempt, MaxAttempts, cs);

                await db.Database.MigrateAsync(cancellationToken);



                _logger.LogInformation("[ApiService] EF Core migrations completed successfully.");
                MigrationsCompleted = true;
                return;
            }
            catch (Exception ex) when (attempt < MaxAttempts && !cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning(ex, "[ApiService] EF Core migrations failed on attempt {Attempt}/{MaxAttempts}. Retrying in {DelaySeconds}s...", attempt, MaxAttempts, RetryDelay.TotalSeconds);
                await Task.Delay(RetryDelay, cancellationToken);
            }
        }

        throw new InvalidOperationException("Unable to apply EF Core migrations after multiple attempts.");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
