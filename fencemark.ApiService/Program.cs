using fencemark.ApiService.Data;
using fencemark.ApiService.Data.Models;
using fencemark.ApiService.Features.Auth;
using fencemark.ApiService.Features.Components;
using fencemark.ApiService.Features.Fences;
using fencemark.ApiService.Features.Gates;
using fencemark.ApiService.Features.Jobs;
using fencemark.ApiService.Features.Organization;
using fencemark.ApiService.Middleware;
using fencemark.ApiService.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddProblemDetails();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Configure database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "Data Source=fencemark.db";
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));

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

// Configure authentication cookie
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.ExpireTimeSpan = TimeSpan.FromDays(30);
    options.SlidingExpiration = true;
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

// Add application services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IOrganizationService, OrganizationService>();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IPricingService, PricingService>();
builder.Services.AddScoped<IQuoteExportService, QuoteExportService>();
builder.Services.AddHttpContextAccessor();

// Add authorization
builder.Services.AddAuthorization();

var app = builder.Build();

// Apply database migrations on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.MigrateAsync();
}

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthentication();
app.UseAuthorization();

// Health endpoint for container liveness/readiness probes
app.MapGet("/health", () => Results.Ok("OK"));

// Map feature endpoints
app.MapAuthEndpoints();
app.MapOrganizationEndpoints();
app.MapFenceEndpoints();
app.MapGateEndpoints();
app.MapComponentEndpoints();
app.MapJobEndpoints();

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

app.MapDefaultEndpoints();

// Pricing Configuration endpoints
app.MapGet("/api/pricing-configs", async (ApplicationDbContext db, ICurrentUserService currentUser, CancellationToken ct) =>
{
    if (!currentUser.IsAuthenticated)
        return Results.Unauthorized();

    var configs = await db.PricingConfigs
        .Include(pc => pc.HeightTiers)
        .Where(pc => pc.OrganizationId == currentUser.OrganizationId)
        .OrderByDescending(pc => pc.IsDefault)
        .ThenBy(pc => pc.Name)
        .ToListAsync(ct);
    return Results.Ok(configs);
})
.RequireAuthorization()
.WithName("GetPricingConfigs")
.WithTags("Pricing");

app.MapGet("/api/pricing-configs/{id}", async (string id, ApplicationDbContext db, ICurrentUserService currentUser, CancellationToken ct) =>
{
    if (!currentUser.IsAuthenticated)
        return Results.Unauthorized();

    var config = await db.PricingConfigs
        .Include(pc => pc.HeightTiers)
        .FirstOrDefaultAsync(pc => pc.Id == id && pc.OrganizationId == currentUser.OrganizationId, ct);
    
    return config != null ? Results.Ok(config) : Results.NotFound();
})
.RequireAuthorization()
.WithName("GetPricingConfigById")
.WithTags("Pricing");

app.MapPost("/api/pricing-configs", async (PricingConfig request, ApplicationDbContext db, ICurrentUserService currentUser, CancellationToken ct) =>
{
    if (!currentUser.IsAuthenticated)
        return Results.Unauthorized();

    request.OrganizationId = currentUser.OrganizationId ?? string.Empty;
    request.Id = Guid.NewGuid().ToString();
    request.CreatedAt = DateTime.UtcNow;
    request.UpdatedAt = DateTime.UtcNow;

    // If this is set as default, unset other defaults
    if (request.IsDefault)
    {
        var existingDefaults = await db.PricingConfigs
            .Where(pc => pc.OrganizationId == currentUser.OrganizationId && pc.IsDefault)
            .ToListAsync(ct);
        foreach (var config in existingDefaults)
        {
            config.IsDefault = false;
        }
    }

    db.PricingConfigs.Add(request);
    await db.SaveChangesAsync(ct);
    return Results.Created($"/api/pricing-configs/{request.Id}", request);
})
.RequireAuthorization()
.WithName("CreatePricingConfig")
.WithTags("Pricing");

app.MapPut("/api/pricing-configs/{id}", async (string id, PricingConfig request, ApplicationDbContext db, ICurrentUserService currentUser, CancellationToken ct) =>
{
    if (!currentUser.IsAuthenticated)
        return Results.Unauthorized();

    var config = await db.PricingConfigs.FirstOrDefaultAsync(pc => pc.Id == id && pc.OrganizationId == currentUser.OrganizationId, ct);
    if (config == null)
        return Results.NotFound();

    // If this is set as default, unset other defaults
    if (request.IsDefault && !config.IsDefault)
    {
        var existingDefaults = await db.PricingConfigs
            .Where(pc => pc.OrganizationId == currentUser.OrganizationId && pc.IsDefault && pc.Id != id)
            .ToListAsync(ct);
        foreach (var otherConfig in existingDefaults)
        {
            otherConfig.IsDefault = false;
        }
    }

    config.Name = request.Name;
    config.Description = request.Description;
    config.LaborRatePerHour = request.LaborRatePerHour;
    config.HoursPerLinearMeter = request.HoursPerLinearMeter;
    config.ContingencyPercentage = request.ContingencyPercentage;
    config.ProfitMarginPercentage = request.ProfitMarginPercentage;
    config.IsDefault = request.IsDefault;
    config.UpdatedAt = DateTime.UtcNow;

    await db.SaveChangesAsync(ct);
    return Results.Ok(config);
})
.RequireAuthorization()
.WithName("UpdatePricingConfig")
.WithTags("Pricing");

app.MapDelete("/api/pricing-configs/{id}", async (string id, ApplicationDbContext db, ICurrentUserService currentUser, CancellationToken ct) =>
{
    if (!currentUser.IsAuthenticated)
        return Results.Unauthorized();

    var config = await db.PricingConfigs.FirstOrDefaultAsync(pc => pc.Id == id && pc.OrganizationId == currentUser.OrganizationId, ct);
    if (config == null)
        return Results.NotFound();

    db.PricingConfigs.Remove(config);
    await db.SaveChangesAsync(ct);
    return Results.Ok(new { success = true });
})
.RequireAuthorization()
.WithName("DeletePricingConfig")
.WithTags("Pricing");

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
