using System;
using System.Security.Claims;
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
using fencemark.ApiService.Features.Quotes;
using fencemark.ApiService.Features.TaxRegions;
using fencemark.ApiService.Features.Mapping;
using fencemark.ApiService.Infrastructure;
using fencemark.ApiService.Middleware;
using fencemark.ApiService.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.DataProtection.AzureKeyVault;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Azure.Identity;

var builder = WebApplication.CreateBuilder(args);

// ============================================================================
// Azure App Configuration Setup
// ============================================================================
// If AppConfig__Endpoint is set (by Bicep/Container App), use Azure App Configuration
// as a configuration source. This allows centralized configuration management
// with environment-specific labels (dev, staging, prod).
var appConfigEndpoint = builder.Configuration["AppConfig__Endpoint"];
if (!string.IsNullOrEmpty(appConfigEndpoint))
{
    var environmentLabel = builder.Configuration["AppConfig__Label"] ?? builder.Environment.EnvironmentName.ToLowerInvariant();

    builder.Configuration.AddAzureAppConfiguration(options =>
    {
        options
            .Connect(new Uri(appConfigEndpoint), new DefaultAzureCredential())
            // Load all keys with the environment label
            .Select(KeyFilter.Any, environmentLabel)
            // Also load keys with no label (shared config)
            .Select(KeyFilter.Any, LabelFilter.Null)
            // Configure Key Vault references to use managed identity
            .ConfigureKeyVault(kv =>
            {
                kv.SetCredential(new DefaultAzureCredential());
            });
    });

    Console.WriteLine($"[ApiService] Connected to Azure App Configuration: {appConfigEndpoint} (label: {environmentLabel})");
}

// Check logging level early - only log startup messages if verbose logging is enabled
var earlyLoggingLevel = builder.Configuration.GetValue<string>("Logging:LoggingLevel") ?? "Verbose";
if (earlyLoggingLevel.Equals("Verbose", StringComparison.OrdinalIgnoreCase))
{
    // CRITICAL DEBUG: Force log output to verify code is running
    var tempLogger = LoggerFactory.Create(b => b.AddConsole()).CreateLogger("Startup");
    tempLogger.LogCritical("============================================");
    tempLogger.LogCritical("[ApiService] STARTING API SERVICE - BUILD v{Ticks}", DateTime.Now.Ticks);
    tempLogger.LogCritical("============================================");
}

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddProblemDetails();

// Configure JSON serialization to handle circular references
// Required because FenceType/GateType have bidirectional navigation properties
// via FenceComponent/GateComponent join tables
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
});

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

// Get logging level from configuration
var loggingLevel = LoggingHelper.GetLoggingLevel(builder.Configuration);
var isVerboseLogging = LoggingHelper.IsVerboseLoggingEnabled(loggingLevel);

if (isVerboseLogging)
{
    Console.WriteLine("============================================");
    Console.WriteLine("[ApiService] STARTING API SERVICE - BUILD v" + DateTime.Now.Ticks);
    Console.WriteLine("============================================");
    Console.WriteLine($"[ApiService] Environment: {builder.Environment.EnvironmentName}");
    Console.WriteLine($"[ApiService] Logging Level: {loggingLevel}");
    Console.WriteLine($"[ApiService] Using DefaultConnection: {LoggingHelper.MaskConnectionString(connectionString)}");
}


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
            
            // Don't set options.Audience - use ValidAudiences in TokenValidationParameters instead
            // This allows the token to have either format: "api://clientId" or just "clientId"
            options.RequireHttpsMetadata = true;
            
            Console.WriteLine($"[ApiService] Using Authority: {options.Authority}");
            Console.WriteLine($"[ApiService] Accepted Audiences: api://{audience}, {audience}");
            
            // Enable detailed PII logging for debugging
            Microsoft.IdentityModel.Logging.IdentityModelEventSource.ShowPII = true;
            
            options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuers = new[]
                {
                    // Custom domain format (e.g., https://devfencemark.ciamlogin.com/{tenantId}/v2.0)
                    $"{instance.TrimEnd('/')}/{tenantId}/v2.0",
                    // Tenant GUID subdomain format - Azure Entra External ID uses this in tokens
                    // (e.g., https://{tenantId}.ciamlogin.com/{tenantId}/v2.0)
                    $"https://{tenantId}.ciamlogin.com/{tenantId}/v2.0",
                    // Standard Azure AD issuers (fallback)
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
                    var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();

                    // Azure AD v1.0 tokens use ClaimTypes.Name for the email
                    var email = context.Principal?.FindFirst(ClaimTypes.Name)?.Value
                               ?? context.Principal?.FindFirst(ClaimTypes.Email)?.Value
                               ?? context.Principal?.FindFirst("preferred_username")?.Value
                               ?? context.Principal?.FindFirst("email")?.Value;

                    // Get the Azure AD object ID (oid) or subject (sub) claim
                    // Use ClaimTypes constants which handle both short and long claim type URIs
                    var externalId = context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                                   ?? context.Principal?.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value
                                   ?? context.Principal?.FindFirst("oid")?.Value
                                   ?? context.Principal?.FindFirst("sub")?.Value;

                    // Get user's name from Azure AD claims
                    var givenName = context.Principal?.FindFirst(ClaimTypes.GivenName)?.Value
                                  ?? context.Principal?.FindFirst("given_name")?.Value;
                    var familyName = context.Principal?.FindFirst(ClaimTypes.Surname)?.Value
                                   ?? context.Principal?.FindFirst("family_name")?.Value;
                    // Fallback to "name" claim if individual name parts not available
                    var displayName = context.Principal?.FindFirst("name")?.Value;

                    logger.LogInformation("[ApiService] Token validated for user: {Email}, ExternalId: {ExternalId}, Name: {GivenName} {FamilyName}", email, externalId, givenName, familyName);
                    
                    if (!string.IsNullOrEmpty(email) && !string.IsNullOrEmpty(externalId))
                    {
                        // Look up the user's organization membership
                        var dbContext = context.HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>();
                        var userManager = context.HttpContext.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
                        var seedDataService = context.HttpContext.RequestServices.GetRequiredService<ISeedDataService>();
                        
                        // Try to find user by external ID first, then by email
                        var user = await userManager.Users
                            .FirstOrDefaultAsync(u => u.ExternalId == externalId && u.ExternalProvider == "AzureAD");
                        
                        if (user == null)
                        {
                            // Try to find by email
                            user = await userManager.FindByEmailAsync(email);
                            
                            if (user != null)
                            {
                                // Existing user, link external identity and update name if available
                                user.ExternalId = externalId;
                                user.ExternalProvider = "AzureAD";
                                user.IsEmailVerified = true; // External provider verified the email
                                user.IsGuest = false;

                                // Update name from Azure AD claims if not already set
                                if (string.IsNullOrWhiteSpace(user.FirstName) && !string.IsNullOrWhiteSpace(givenName))
                                {
                                    user.FirstName = givenName;
                                }
                                if (string.IsNullOrWhiteSpace(user.LastName) && !string.IsNullOrWhiteSpace(familyName))
                                {
                                    user.LastName = familyName;
                                }
                                // If no first/last name but we have a display name, try to parse it
                                if (string.IsNullOrWhiteSpace(user.FirstName) && string.IsNullOrWhiteSpace(user.LastName) && !string.IsNullOrWhiteSpace(displayName))
                                {
                                    var nameParts = displayName.Split(' ', 2);
                                    user.FirstName = nameParts[0];
                                    user.LastName = nameParts.Length > 1 ? nameParts[1] : null;
                                }

                                await userManager.UpdateAsync(user);
                                logger.LogInformation("[ApiService] Linked existing user {Email} to external identity {ExternalId}", email, externalId);
                            }
                            else
                            {
                                // Create new user with name from Azure AD claims
                                var firstName = givenName;
                                var lastName = familyName;

                                // If no first/last name but we have a display name, try to parse it
                                if (string.IsNullOrWhiteSpace(firstName) && string.IsNullOrWhiteSpace(lastName) && !string.IsNullOrWhiteSpace(displayName))
                                {
                                    var nameParts = displayName.Split(' ', 2);
                                    firstName = nameParts[0];
                                    lastName = nameParts.Length > 1 ? nameParts[1] : null;
                                }

                                user = new ApplicationUser
                                {
                                    UserName = email,
                                    Email = email,
                                    FirstName = firstName,
                                    LastName = lastName,
                                    ExternalId = externalId,
                                    ExternalProvider = "AzureAD",
                                    IsEmailVerified = true, // External provider verified the email
                                    IsGuest = false,
                                    CreatedAt = DateTime.UtcNow
                                };

                                var result = await userManager.CreateAsync(user);
                                if (!result.Succeeded)
                                {
                                    logger.LogError("[ApiService] Failed to create user {Email}: {Errors}", email, string.Join(", ", result.Errors.Select(e => e.Description)));
                                    // Don't add claims if user creation failed
                                    // This will result in authentication failure downstream
                                    return;
                                }
                                logger.LogInformation("[ApiService] Created new user {Email} with external identity {ExternalId}", email, externalId);
                            }
                        }
                        
                        // Ensure the user has an organization membership
                        var membership = await dbContext.OrganizationMembers
                            .Include(m => m.Organization)
                            .FirstOrDefaultAsync(m => m.UserId == user.Id);
                        
                        if (membership != null)
                        {
                            // Check if organization needs seed data (may have been deleted)
                            if (!await seedDataService.HasSampleDataAsync(membership.OrganizationId))
                            {
                                await seedDataService.SeedSampleDataAsync(membership.OrganizationId);
                                logger.LogInformation("[ApiService] Re-seeded sample data for organization {OrgId}", membership.OrganizationId);
                            }
                        }
                        else
                        {
                            // User has no organization - they need to go through onboarding
                            logger.LogInformation("[ApiService] User {Email} has no organization - onboarding required", email);
                        }
                        
                        // Add custom claims to the principal
                        var claims = new List<Claim>
                        {
                            // Add ApplicationUserId claim to map JWT user to ASP.NET Identity user
                            new Claim(CustomClaimTypes.ApplicationUserId, user.Id)
                        };
                        
                        if (membership != null)
                        {
                            // Add OrganizationId and Role claims to the principal.
                            // Role uses the standard ClaimTypes.Role so ASP.NET Core's
                            // role-based authorization (RequireRole/IsInRole) works without
                            // extra configuration.
                            claims.Add(new Claim(CustomClaimTypes.OrganizationId, membership.OrganizationId));
                            claims.Add(new Claim(ClaimTypes.Role, membership.Role.ToString()));
                            logger.LogInformation("[ApiService] Added ApplicationUserId, OrganizationId, and Role claims: UserId={UserId}, OrgId={OrgId}, Role={Role}", user.Id, membership.OrganizationId, membership.Role);
                        }
                        else
                        {
                            logger.LogInformation("[ApiService] Added ApplicationUserId claim: {UserId}", user.Id);
                            logger.LogWarning("[ApiService] User {Email} has no organization membership", email);
                        }
                        
                        var appIdentity = new ClaimsIdentity(claims);
                        context.Principal?.AddIdentity(appIdentity);
                    }
                    else
                    {
                        logger.LogWarning("[ApiService] Token validated but missing email or external ID claims");
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

// Add cadastral service with configuration
builder.Services.Configure<CadastralOptions>(builder.Configuration.GetSection(CadastralOptions.SectionName));
builder.Services.AddHttpClient(); // For cadastral API calls
builder.Services.AddScoped<ICadastralService, CadastralService>();

// Add Azure Maps token service for server-side token acquisition
builder.Services.AddMemoryCache();
builder.Services.Configure<AzureMapsOptions>(builder.Configuration.GetSection(AzureMapsOptions.SectionName));
builder.Services.AddSingleton<IAzureMapsTokenService, AzureMapsTokenService>();

// Add authorization
builder.Services.AddAuthorization(options => options.AddFencemarkPolicies());

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
app.MapQuoteEndpoints();
app.MapTaxRegionEndpoints();
app.MapDiscountEndpoints();
app.MapParcelEndpoints();
app.MapDrawingEndpoints();
app.MapFenceSegmentEndpoints();
app.MapGatePositionEndpoints();
app.MapMappingEndpoints();

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

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
