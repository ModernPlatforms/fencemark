using System;
using fencemark.ApiService.Data;
using fencemark.ApiService.Data.Models;
using fencemark.ApiService.Features.Auth;
using fencemark.ApiService.Features.Components;
using fencemark.ApiService.Features.Discounts;
using fencemark.ApiService.Features.Drawings;
using fencemark.ApiService.Features.Fences;
using fencemark.ApiService.Features.FenceSegments;
using fencemark.ApiService.Features.Gates;
using fencemark.ApiService.Features.GatePositions;
using fencemark.ApiService.Features.Jobs;
using fencemark.ApiService.Features.Organization;
using fencemark.ApiService.Features.Parcels;
using fencemark.ApiService.Features.Pricing;
using fencemark.ApiService.Features.TaxRegions;
using fencemark.ApiService.Infrastructure;
using fencemark.ApiService.Middleware;
using fencemark.ApiService.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.DataProtection.AzureKeyVault;

var builder = WebApplication.CreateBuilder(args);

// CRITICAL DEBUG: Force log output to verify code is running
var tempLogger = LoggerFactory.Create(b => b.AddConsole()).CreateLogger("Startup");
tempLogger.LogCritical("============================================");
tempLogger.LogCritical("[ApiService] STARTING API SERVICE - BUILD v{Ticks}", DateTime.Now.Ticks);
tempLogger.LogCritical("============================================");

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddProblemDetails();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Configure database
// Connection string now always targets SQL Server. When running under Aspire
// AppHost, DefaultConnection is provided by the referenced SQL resource.
// When running the ApiService project directly (without AppHost), fall back
// to a local SQL Server/SQL Express instance for convenience.
var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? builder.Configuration.GetConnectionString("fencemark")   // use Aspire-injected connection
    ?? "Server=localhost;Database=fencemark;Trusted_Connection=True;TrustServerCertificate=True;";

if (!connectionString.Contains("Connection Timeout", StringComparison.OrdinalIgnoreCase))
{
    connectionString += ";Connection Timeout=5";
}

Console.WriteLine("============================================");
Console.WriteLine("[ApiService] STARTING API SERVICE - BUILD v" + DateTime.Now.Ticks);
Console.WriteLine("============================================");

Console.WriteLine($"[ApiService] Using DefaultConnection: {connectionString}");

// Always use SQL Server with TenantConnectionInterceptor (RLS via SESSION_CONTEXT)
builder.Services.AddScoped<TenantConnectionInterceptor>();

builder.Services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
{
    // In Azure, DefaultConnection should use Authentication=Active Directory Default
    // so the system-assigned managed identity is used. In local Aspire, the
    // AppHost provides DefaultConnection pointing at the Aspire SQL container.
    options.UseSqlServer(connectionString)
        .AddInterceptors(serviceProvider.GetRequiredService<TenantConnectionInterceptor>());
});

// Apply EF Core migrations on startup with retries
builder.Services.AddHostedService<DatabaseMigrationHostedService>();

// Add custom health check that waits for migrations
builder.Services.AddHealthChecks()
    .AddCheck("migrations", () => 
        DatabaseMigrationHostedService.MigrationsCompleted 
            ? HealthCheckResult.Healthy("Migrations completed") 
            : HealthCheckResult.Unhealthy("Migrations still running"));

// Configure Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Password settings
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 8;

    // User settings
    options.User.RequireUniqueEmail = true;

    // Sign in settings
    options.SignIn.RequireConfirmedEmail = false; // Start simple, can be enabled later
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Configure Data Protection to use Azure Key Vault for persistent key storage
// This ensures encrypted data survives container restarts and works across instances
var dataProtectionBuilder = builder.Services.AddDataProtection()
    .SetApplicationName("fencemark");

// For local development, persist keys to a shared location
// This prevents cookies from breaking when the app restarts
if (builder.Environment.IsDevelopment())
{
    var keysPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "fencemark-keys");
    Directory.CreateDirectory(keysPath);
    dataProtectionBuilder.PersistKeysToFileSystem(new DirectoryInfo(keysPath));
    Console.WriteLine($"[ApiService] Data Protection keys persisted locally to: {keysPath}");
}

if (!string.IsNullOrEmpty(builder.Configuration["ASPNETCORE_DATAPROTECTION_KEYVAULT_URI"]))
{
    var keyVaultKeyIdentifier = builder.Configuration["ASPNETCORE_DATAPROTECTION_KEYVAULT_KEYIDENTIFIER"];
    
    if (!string.IsNullOrEmpty(keyVaultKeyIdentifier))
    {
        var credential = new Azure.Identity.DefaultAzureCredential();
        
        // Add retry logic to handle initial 401 from Key Vault during cold starts
        // This is normal with Managed Identity - first request gets 401, then token is acquired
        int maxRetries = 3;
        int retryDelayMs = 1000;
        Exception? lastException = null;
        
        for (int attempt = 0; attempt <= maxRetries; attempt++)
        {
            try
            {
                dataProtectionBuilder.ProtectKeysWithAzureKeyVault(new Uri(keyVaultKeyIdentifier), credential);
                Console.WriteLine($"[ApiService] Data Protection keys protected with Azure Key Vault: {keyVaultKeyIdentifier}");
                lastException = null;
                break;
            }
            catch (Exception ex) when (attempt < maxRetries)
            {
                lastException = ex;
                Console.WriteLine($"[ApiService] Attempt {attempt + 1}/{maxRetries + 1} - Key Vault auth in progress, retrying in {retryDelayMs}ms...");
                await Task.Delay(retryDelayMs);
                retryDelayMs *= 2; // Exponential backoff
            }
        }
        
        if (lastException != null)
        {
            Console.WriteLine($"[ApiService] Warning: Could not configure Key Vault for data protection after {maxRetries + 1} attempts: {lastException.Message}");
        }
    }
}

// CRITICAL: Override Identity's default authentication scheme
// Identity sets Cookie as default, but this API should use JWT Bearer
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    var authority = builder.Configuration["AzureAd:Authority"];
    var audience = builder.Configuration["AzureAd:ClientId"];
    
    Console.WriteLine($"[ApiService] Configuring JWT Bearer: Authority={authority}, Audience={audience}");
    
    if (!string.IsNullOrEmpty(authority) && !string.IsNullOrEmpty(audience))
    {
            // Use Entra External ID (CIAM) as the authority
            var tenantId = builder.Configuration["AzureAd:TenantId"];
            var instance = builder.Configuration["AzureAd:Instance"] ?? "https://devfencemark.ciamlogin.com/";
            
            // For Entra External ID, use the CIAM authority
            options.Authority = $"{instance.TrimEnd('/')}/{tenantId}/v2.0";
            
            // Audience is the API's Application ID URI
            options.Audience = $"api://{audience}";
            options.RequireHttpsMetadata = true;
            
            Console.WriteLine($"[ApiService] Using Authority: {options.Authority}");
            Console.WriteLine($"[ApiService] Using Audience: {options.Audience}");
            
            // Enable detailed PII logging for debugging
            Microsoft.IdentityModel.Logging.IdentityModelEventSource.ShowPII = true;
            
            options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuers = new[] 
                { 
                    $"{instance.TrimEnd('/')}/{tenantId}/v2.0",
                    $"https://login.microsoftonline.com/{tenantId}/v2.0",
                    $"https://sts.windows.net/{tenantId}/"
                },
                ValidateAudience = true,
                ValidAudiences = new[] { $"api://{audience}", audience },
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true
            };
            
            // CRITICAL: Disable automatic challenge redirect for APIs
            // JWT Bearer should return 401/403 status codes, NOT redirect to login pages
            options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var token = context.Request.Headers["Authorization"].FirstOrDefault();
                    Console.WriteLine($"[ApiService] OnMessageReceived - Token present: {!string.IsNullOrEmpty(token)}, Token length: {token?.Length ?? 0}");
                    return Task.CompletedTask;
                },
                OnAuthenticationFailed = context =>
                {
                    Console.WriteLine($"[ApiService] Authentication failed: {context.Exception.GetType().Name}: {context.Exception.Message}");
                    if (context.Exception.InnerException != null)
                    {
                        Console.WriteLine($"[ApiService] Inner exception: {context.Exception.InnerException.Message}");
                    }
                    return Task.CompletedTask;
                },
                OnTokenValidated = async context =>
                {
                    // Azure AD v1.0 tokens use ClaimTypes.Name for the email
                    var email = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value
                               ?? context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
                               ?? context.Principal?.FindFirst("preferred_username")?.Value
                               ?? context.Principal?.FindFirst("email")?.Value;
                               
                    Console.WriteLine($"[ApiService] Token validated for user: {email}");
                    
                    if (!string.IsNullOrEmpty(email))
                    {
                        // Look up the user's organization membership
                        var dbContext = context.HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>();
                        var userManager = context.HttpContext.RequestServices.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<ApplicationUser>>();
                        
                        var user = await userManager.FindByEmailAsync(email);
                        if (user != null)
                        {
                            var membership = await dbContext.OrganizationMembers
                                .FirstOrDefaultAsync(m => m.UserId == user.Id);
                            
                            if (membership != null)
                            {
                                // Add OrganizationId claim to the principal
                                var claims = new List<System.Security.Claims.Claim>
                                {
                                    new System.Security.Claims.Claim("OrganizationId", membership.OrganizationId)
                                };
                                var appIdentity = new System.Security.Claims.ClaimsIdentity(claims);
                                context.Principal?.AddIdentity(appIdentity);
                                
                                Console.WriteLine($"[ApiService] Added OrganizationId claim: {membership.OrganizationId}");
                            }
                            else
                            {
                                Console.WriteLine($"[ApiService] Warning: User {email} has no organization membership");
                            }
                        }
                        else
                        {
                            Console.WriteLine($"[ApiService] Warning: User {email} not found in database");
                        }
                    }
                }
            };
        }
        else
        {
            Console.WriteLine("[ApiService] Warning: AzureAd configuration not found. JWT Bearer auth will not work.");
        }
    });

// CRITICAL: Disable cookie authentication redirects for API
// This prevents Identity from redirecting to /Account/Login on 401
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Events.OnRedirectToLogin = context =>
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = context =>
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    };
});

// Configure CORS for Blazor WASM client
var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
if (builder.Environment.IsDevelopment())
{
    Console.WriteLine($"[ApiService] CORS configured for {corsOrigins.Length} origins: {string.Join(", ", corsOrigins)}");
}
else
{
    Console.WriteLine($"[ApiService] CORS configured for {corsOrigins.Length} origins");
}

builder.Services.AddCors(options =>
{
    options.AddPolicy("WasmClient", policy =>
    {
        policy.WithOrigins(corsOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials(); // Required for cookies/authentication
    });
});

// Add application services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IOrganizationService, OrganizationService>();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IPricingService, PricingService>();
builder.Services.AddScoped<IQuoteExportService, QuoteExportService>();
builder.Services.AddScoped<ISeedDataService, SeedDataService>();
builder.Services.AddHttpContextAccessor();

// Add authorization
builder.Services.AddAuthorization();

var app = builder.Build();

// Add request/response logging middleware
app.Use(async (context, next) =>
{
    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("[{Method}] {Path} - Starting request", context.Request.Method, context.Request.Path);
    
    // Log headers for debugging
    if (context.Request.Headers.Authorization.Count > 0)
    {
        logger.LogInformation("Authorization header present: {AuthHeader}", 
            context.Request.Headers.Authorization.ToString().Substring(0, Math.Min(50, context.Request.Headers.Authorization.ToString().Length)));
    }
    else
    {
        logger.LogWarning("No Authorization header found");
    }
    
    await next();
    
    logger.LogInformation("[{Method}] {Path} - Completed with status {StatusCode}", 
        context.Request.Method, context.Request.Path, context.Response.StatusCode);
});

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Enable CORS - must be before authentication/authorization
app.UseCors("WasmClient");

app.UseAuthentication();
app.UseAuthorization();

// Map default endpoints (including health checks) early
app.MapDefaultEndpoints();

// Map feature endpoints
app.MapAuthEndpoints();
app.MapOrganizationEndpoints();
app.MapFenceEndpoints();
app.MapGateEndpoints();
app.MapComponentEndpoints();
app.MapJobEndpoints();
app.MapPricingEndpoints();
app.MapTaxRegionEndpoints();
app.MapDiscountEndpoints();
app.MapParcelEndpoints();
app.MapDrawingEndpoints();
app.MapFenceSegmentEndpoints();
app.MapGatePositionEndpoints();

// Keep the original weather forecast endpoint for backward compatibility
string[] summaries = ["Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"];

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        )) 
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

// Quote endpoints
app.MapPost("/api/quotes/generate", async (GenerateQuoteRequest request, IPricingService pricingService, ICurrentUserService currentUser, CancellationToken ct) =>
{
    if (!currentUser.IsAuthenticated)
        return Results.Unauthorized();

    try
    {
        var quote = await pricingService.GenerateQuoteAsync(request.JobId, request.PricingConfigId, ct);
        return Results.Ok(quote);
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
})
.RequireAuthorization()
.WithName("GenerateQuote")
.WithTags("Quotes");

app.MapPost("/api/quotes/{id}/recalculate", async (string id, RecalculateQuoteRequest request, IPricingService pricingService, ApplicationDbContext db, ICurrentUserService currentUser, CancellationToken ct) =>
{
    if (!currentUser.IsAuthenticated)
        return Results.Unauthorized();

    // Verify quote belongs to user's organization
    var quote = await db.Quotes.FirstOrDefaultAsync(q => q.Id == id && q.OrganizationId == currentUser.OrganizationId, ct);
    if (quote == null)
        return Results.NotFound();

    try
    {
        var updatedQuote = await pricingService.RecalculateQuoteAsync(id, request.ChangeSummary, ct);
        return Results.Ok(updatedQuote);
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
})
.RequireAuthorization()
.WithName("RecalculateQuote")
.WithTags("Quotes");

app.MapGet("/api/quotes", async (ApplicationDbContext db, ICurrentUserService currentUser, CancellationToken ct) =>
{
    if (!currentUser.IsAuthenticated)
        return Results.Unauthorized();

    var quotes = await db.Quotes
        .Include(q => q.Job)
        .Where(q => q.OrganizationId == currentUser.OrganizationId)
        .OrderByDescending(q => q.CreatedAt)
        .ToListAsync(ct);
    return Results.Ok(quotes);
})
.RequireAuthorization()
.WithName("GetQuotes")
.WithTags("Quotes");

app.MapGet("/api/quotes/{id}", async (string id, ApplicationDbContext db, ICurrentUserService currentUser, CancellationToken ct) =>
{
    if (!currentUser.IsAuthenticated)
        return Results.Unauthorized();

    var quote = await db.Quotes
        .Include(q => q.Job)
        .Include(q => q.BillOfMaterials)
        .Include(q => q.Versions)
        .Include(q => q.PricingConfig)
        .FirstOrDefaultAsync(q => q.Id == id && q.OrganizationId == currentUser.OrganizationId, ct);
    
    return quote != null ? Results.Ok(quote) : Results.NotFound();
})
.RequireAuthorization()
.WithName("GetQuoteById")
.WithTags("Quotes");

app.MapPut("/api/quotes/{id}", async (string id, UpdateQuoteRequest request, ApplicationDbContext db, ICurrentUserService currentUser, CancellationToken ct) =>
{
    if (!currentUser.IsAuthenticated)
        return Results.Unauthorized();

    var quote = await db.Quotes.FirstOrDefaultAsync(q => q.Id == id && q.OrganizationId == currentUser.OrganizationId, ct);
    if (quote == null)
        return Results.NotFound();

    quote.Status = request.Status;
    quote.ValidUntil = request.ValidUntil;
    quote.Terms = request.Terms;
    quote.Notes = request.Notes;
    quote.TaxAmount = request.TaxAmount;
    quote.GrandTotal = quote.TotalAmount + request.TaxAmount;
    quote.UpdatedAt = DateTime.UtcNow;

    await db.SaveChangesAsync(ct);
    return Results.Ok(quote);
})
.RequireAuthorization()
.WithName("UpdateQuote")
.WithTags("Quotes");

app.MapDelete("/api/quotes/{id}", async (string id, ApplicationDbContext db, ICurrentUserService currentUser, CancellationToken ct) =>
{
    if (!currentUser.IsAuthenticated)
        return Results.Unauthorized();

    var quote = await db.Quotes.FirstOrDefaultAsync(q => q.Id == id && q.OrganizationId == currentUser.OrganizationId, ct);
    if (quote == null)
        return Results.NotFound();

    db.Quotes.Remove(quote);
    await db.SaveChangesAsync(ct);
    return Results.Ok(new { success = true });
})
.RequireAuthorization()
.WithName("DeleteQuote")
.WithTags("Quotes");

// BOM endpoints
app.MapGet("/api/jobs/{jobId}/bom", async (string jobId, IPricingService pricingService, ApplicationDbContext db, ICurrentUserService currentUser, CancellationToken ct) =>
{
    if (!currentUser.IsAuthenticated)
        return Results.Unauthorized();

    // Verify job belongs to user's organization
    var job = await db.Jobs.FirstOrDefaultAsync(j => j.Id == jobId && j.OrganizationId == currentUser.OrganizationId, ct);
    if (job == null)
        return Results.NotFound();

    try
    {
        var bom = await pricingService.CalculateBillOfMaterialsAsync(jobId, ct);
        return Results.Ok(bom);
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
})
.RequireAuthorization()
.WithName("GetJobBillOfMaterials")
.WithTags("BOM");

// Export endpoints
app.MapGet("/api/quotes/{id}/export/html", async (string id, IQuoteExportService exportService, ApplicationDbContext db, ICurrentUserService currentUser, CancellationToken ct) =>
{
    if (!currentUser.IsAuthenticated)
        return Results.Unauthorized();

    // Verify quote belongs to user's organization
    var quote = await db.Quotes.FirstOrDefaultAsync(q => q.Id == id && q.OrganizationId == currentUser.OrganizationId, ct);
    if (quote == null)
        return Results.NotFound();

    try
    {
        var html = await exportService.ExportQuoteAsHtmlAsync(id, ct);
        return Results.Content(html, "text/html");
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
})
.RequireAuthorization()
.WithName("ExportQuoteAsHtml")
.WithTags("Export");

app.MapGet("/api/quotes/{id}/export/csv", async (string id, IQuoteExportService exportService, ApplicationDbContext db, ICurrentUserService currentUser, CancellationToken ct) =>
{
    if (!currentUser.IsAuthenticated)
        return Results.Unauthorized();

    // Verify quote belongs to user's organization
    var quote = await db.Quotes.FirstOrDefaultAsync(q => q.Id == id && q.OrganizationId == currentUser.OrganizationId, ct);
    if (quote == null)
        return Results.NotFound();

    try
    {
        var csv = await exportService.ExportBomAsCsvAsync(id, ct);
        return Results.Text(csv, "text/csv");
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
})
.RequireAuthorization()
.WithName("ExportBomAsCsv")
.WithTags("Export");

app.Run();

// Request/Response DTOs
record GenerateQuoteRequest(string JobId, string? PricingConfigId = null);
record RecalculateQuoteRequest(string? ChangeSummary = null);
record UpdateQuoteRequest(QuoteStatus Status, DateTime? ValidUntil, string? Terms, string? Notes, decimal TaxAmount);

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
