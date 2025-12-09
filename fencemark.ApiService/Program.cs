using fencemark.ApiService.Data;
using fencemark.ApiService.Data.Models;
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

// Authentication endpoints
app.MapPost("/api/auth/register", async (RegisterRequest request, IAuthService authService, CancellationToken ct) =>
{
    var result = await authService.RegisterAsync(request, ct);
    return result.Success ? Results.Ok(result) : Results.BadRequest(result);
})
.WithName("Register")
.WithTags("Authentication");

app.MapPost("/api/auth/login", async (LoginRequest request, IAuthService authService, CancellationToken ct) =>
{
    var result = await authService.LoginAsync(request, ct);
    return result.Success ? Results.Ok(result) : Results.Unauthorized();
})
.WithName("Login")
.WithTags("Authentication");

app.MapPost("/api/auth/logout", async (SignInManager<ApplicationUser> signInManager) =>
{
    await signInManager.SignOutAsync();
    return Results.Ok(new { success = true, message = "Logged out successfully" });
})
.RequireAuthorization()
.WithName("Logout")
.WithTags("Authentication");

app.MapGet("/api/auth/me", async (ICurrentUserService currentUser) =>
{
    if (!currentUser.IsAuthenticated)
    {
        return Results.Unauthorized();
    }

    return Results.Ok(new
    {
        userId = currentUser.UserId,
        email = currentUser.Email,
        organizationId = currentUser.OrganizationId
    });
})
.RequireAuthorization()
.WithName("GetCurrentUser")
.WithTags("Authentication");

// Organization endpoints
app.MapGet("/api/organizations/{organizationId}/members",
    async (string organizationId, IOrganizationService orgService, ICurrentUserService currentUser, CancellationToken ct) =>
{
    if (currentUser.OrganizationId != organizationId)
    {
        return Results.Forbid();
    }

    var members = await orgService.GetMembersAsync(organizationId, ct);
    return Results.Ok(members);
})
.RequireAuthorization()
.WithName("GetOrganizationMembers")
.WithTags("Organization");

app.MapPost("/api/organizations/{organizationId}/invite",
    async (string organizationId, InviteUserRequest request, IOrganizationService orgService, ICurrentUserService currentUser, CancellationToken ct) =>
{
    if (currentUser.OrganizationId != organizationId)
    {
        return Results.Forbid();
    }

    var result = await orgService.InviteUserAsync(organizationId, request, ct);
    return result.Success ? Results.Ok(result) : Results.BadRequest(result);
})
.RequireAuthorization()
.WithName("InviteUser")
.WithTags("Organization");

app.MapPost("/api/organizations/accept-invitation",
    async (AcceptInvitationRequest request, IOrganizationService orgService, CancellationToken ct) =>
{
    var result = await orgService.AcceptInvitationAsync(request, ct);
    return result.Success ? Results.Ok(result) : Results.BadRequest(result);
})
.WithName("AcceptInvitation")
.WithTags("Organization");

app.MapPut("/api/organizations/{organizationId}/members/role",
    async (string organizationId, UpdateRoleRequest request, IOrganizationService orgService, ICurrentUserService currentUser, CancellationToken ct) =>
{
    if (currentUser.OrganizationId != organizationId)
    {
        return Results.Forbid();
    }

    var success = await orgService.UpdateRoleAsync(organizationId, request, ct);
    return success ? Results.Ok(new { success = true }) : Results.BadRequest(new { success = false, message = "Failed to update role" });
})
.RequireAuthorization()
.WithName("UpdateMemberRole")
.WithTags("Organization");

app.MapDelete("/api/organizations/{organizationId}/members/{userId}",
    async (string organizationId, string userId, IOrganizationService orgService, ICurrentUserService currentUser, CancellationToken ct) =>
{
    if (currentUser.OrganizationId != organizationId)
    {
        return Results.Forbid();
    }

    var success = await orgService.RemoveMemberAsync(organizationId, userId, ct);
    return success ? Results.Ok(new { success = true }) : Results.BadRequest(new { success = false, message = "Failed to remove member" });
})
.RequireAuthorization()
.WithName("RemoveMember")
.WithTags("Organization");

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

// Fence Type endpoints
app.MapGet("/api/fences", async (ApplicationDbContext db, ICurrentUserService currentUser, CancellationToken ct) =>
{
    if (!currentUser.IsAuthenticated)
        return Results.Unauthorized();

    var fences = await db.FenceTypes
        .Where(f => f.OrganizationId == currentUser.OrganizationId)
        .OrderBy(f => f.Name)
        .ToListAsync(ct);
    return Results.Ok(fences);
})
.RequireAuthorization()
.WithName("GetFenceTypes")
.WithTags("Fences");

app.MapGet("/api/fences/{id}", async (string id, ApplicationDbContext db, ICurrentUserService currentUser, CancellationToken ct) =>
{
    if (!currentUser.IsAuthenticated)
        return Results.Unauthorized();

    var fence = await db.FenceTypes
        .Include(f => f.Components)
            .ThenInclude(fc => fc.Component)
        .FirstOrDefaultAsync(f => f.Id == id && f.OrganizationId == currentUser.OrganizationId, ct);
    
    return fence != null ? Results.Ok(fence) : Results.NotFound();
})
.RequireAuthorization()
.WithName("GetFenceTypeById")
.WithTags("Fences");

app.MapPost("/api/fences", async (FenceType request, ApplicationDbContext db, ICurrentUserService currentUser, CancellationToken ct) =>
{
    if (!currentUser.IsAuthenticated)
        return Results.Unauthorized();

    request.OrganizationId = currentUser.OrganizationId ?? string.Empty;
    request.Id = Guid.NewGuid().ToString();
    request.CreatedAt = DateTime.UtcNow;
    request.UpdatedAt = DateTime.UtcNow;

    db.FenceTypes.Add(request);
    await db.SaveChangesAsync(ct);
    return Results.Created($"/api/fences/{request.Id}", request);
})
.RequireAuthorization()
.WithName("CreateFenceType")
.WithTags("Fences");

app.MapPut("/api/fences/{id}", async (string id, FenceType request, ApplicationDbContext db, ICurrentUserService currentUser, CancellationToken ct) =>
{
    if (!currentUser.IsAuthenticated)
        return Results.Unauthorized();

    var fence = await db.FenceTypes.FirstOrDefaultAsync(f => f.Id == id && f.OrganizationId == currentUser.OrganizationId, ct);
    if (fence == null)
        return Results.NotFound();

    fence.Name = request.Name;
    fence.Description = request.Description;
    fence.HeightInFeet = request.HeightInFeet;
    fence.Material = request.Material;
    fence.Style = request.Style;
    fence.PricePerLinearFoot = request.PricePerLinearFoot;
    fence.UpdatedAt = DateTime.UtcNow;

    await db.SaveChangesAsync(ct);
    return Results.Ok(fence);
})
.RequireAuthorization()
.WithName("UpdateFenceType")
.WithTags("Fences");

app.MapDelete("/api/fences/{id}", async (string id, ApplicationDbContext db, ICurrentUserService currentUser, CancellationToken ct) =>
{
    if (!currentUser.IsAuthenticated)
        return Results.Unauthorized();

    var fence = await db.FenceTypes.FirstOrDefaultAsync(f => f.Id == id && f.OrganizationId == currentUser.OrganizationId, ct);
    if (fence == null)
        return Results.NotFound();

    db.FenceTypes.Remove(fence);
    await db.SaveChangesAsync(ct);
    return Results.Ok(new { success = true });
})
.RequireAuthorization()
.WithName("DeleteFenceType")
.WithTags("Fences");

// Gate Type endpoints
app.MapGet("/api/gates", async (ApplicationDbContext db, ICurrentUserService currentUser, CancellationToken ct) =>
{
    if (!currentUser.IsAuthenticated)
        return Results.Unauthorized();

    var gates = await db.GateTypes
        .Where(g => g.OrganizationId == currentUser.OrganizationId)
        .OrderBy(g => g.Name)
        .ToListAsync(ct);
    return Results.Ok(gates);
})
.RequireAuthorization()
.WithName("GetGateTypes")
.WithTags("Gates");

app.MapGet("/api/gates/{id}", async (string id, ApplicationDbContext db, ICurrentUserService currentUser, CancellationToken ct) =>
{
    if (!currentUser.IsAuthenticated)
        return Results.Unauthorized();

    var gate = await db.GateTypes
        .Include(g => g.Components)
            .ThenInclude(gc => gc.Component)
        .FirstOrDefaultAsync(g => g.Id == id && g.OrganizationId == currentUser.OrganizationId, ct);
    
    return gate != null ? Results.Ok(gate) : Results.NotFound();
})
.RequireAuthorization()
.WithName("GetGateTypeById")
.WithTags("Gates");

app.MapPost("/api/gates", async (GateType request, ApplicationDbContext db, ICurrentUserService currentUser, CancellationToken ct) =>
{
    if (!currentUser.IsAuthenticated)
        return Results.Unauthorized();

    request.OrganizationId = currentUser.OrganizationId ?? string.Empty;
    request.Id = Guid.NewGuid().ToString();
    request.CreatedAt = DateTime.UtcNow;
    request.UpdatedAt = DateTime.UtcNow;

    db.GateTypes.Add(request);
    await db.SaveChangesAsync(ct);
    return Results.Created($"/api/gates/{request.Id}", request);
})
.RequireAuthorization()
.WithName("CreateGateType")
.WithTags("Gates");

app.MapPut("/api/gates/{id}", async (string id, GateType request, ApplicationDbContext db, ICurrentUserService currentUser, CancellationToken ct) =>
{
    if (!currentUser.IsAuthenticated)
        return Results.Unauthorized();

    var gate = await db.GateTypes.FirstOrDefaultAsync(g => g.Id == id && g.OrganizationId == currentUser.OrganizationId, ct);
    if (gate == null)
        return Results.NotFound();

    gate.Name = request.Name;
    gate.Description = request.Description;
    gate.WidthInFeet = request.WidthInFeet;
    gate.HeightInFeet = request.HeightInFeet;
    gate.Material = request.Material;
    gate.Style = request.Style;
    gate.BasePrice = request.BasePrice;
    gate.UpdatedAt = DateTime.UtcNow;

    await db.SaveChangesAsync(ct);
    return Results.Ok(gate);
})
.RequireAuthorization()
.WithName("UpdateGateType")
.WithTags("Gates");

app.MapDelete("/api/gates/{id}", async (string id, ApplicationDbContext db, ICurrentUserService currentUser, CancellationToken ct) =>
{
    if (!currentUser.IsAuthenticated)
        return Results.Unauthorized();

    var gate = await db.GateTypes.FirstOrDefaultAsync(g => g.Id == id && g.OrganizationId == currentUser.OrganizationId, ct);
    if (gate == null)
        return Results.NotFound();

    db.GateTypes.Remove(gate);
    await db.SaveChangesAsync(ct);
    return Results.Ok(new { success = true });
})
.RequireAuthorization()
.WithName("DeleteGateType")
.WithTags("Gates");

// Component endpoints
app.MapGet("/api/components", async (ApplicationDbContext db, ICurrentUserService currentUser, CancellationToken ct) =>
{
    if (!currentUser.IsAuthenticated)
        return Results.Unauthorized();

    var components = await db.Components
        .Where(c => c.OrganizationId == currentUser.OrganizationId)
        .OrderBy(c => c.Category)
        .ThenBy(c => c.Name)
        .ToListAsync(ct);
    return Results.Ok(components);
})
.RequireAuthorization()
.WithName("GetComponents")
.WithTags("Components");

app.MapGet("/api/components/{id}", async (string id, ApplicationDbContext db, ICurrentUserService currentUser, CancellationToken ct) =>
{
    if (!currentUser.IsAuthenticated)
        return Results.Unauthorized();

    var component = await db.Components
        .FirstOrDefaultAsync(c => c.Id == id && c.OrganizationId == currentUser.OrganizationId, ct);
    
    return component != null ? Results.Ok(component) : Results.NotFound();
})
.RequireAuthorization()
.WithName("GetComponentById")
.WithTags("Components");

app.MapPost("/api/components", async (Component request, ApplicationDbContext db, ICurrentUserService currentUser, CancellationToken ct) =>
{
    if (!currentUser.IsAuthenticated)
        return Results.Unauthorized();

    request.OrganizationId = currentUser.OrganizationId ?? string.Empty;
    request.Id = Guid.NewGuid().ToString();
    request.CreatedAt = DateTime.UtcNow;
    request.UpdatedAt = DateTime.UtcNow;

    db.Components.Add(request);
    await db.SaveChangesAsync(ct);
    return Results.Created($"/api/components/{request.Id}", request);
})
.RequireAuthorization()
.WithName("CreateComponent")
.WithTags("Components");

app.MapPut("/api/components/{id}", async (string id, Component request, ApplicationDbContext db, ICurrentUserService currentUser, CancellationToken ct) =>
{
    if (!currentUser.IsAuthenticated)
        return Results.Unauthorized();

    var component = await db.Components.FirstOrDefaultAsync(c => c.Id == id && c.OrganizationId == currentUser.OrganizationId, ct);
    if (component == null)
        return Results.NotFound();

    component.Name = request.Name;
    component.Description = request.Description;
    component.Sku = request.Sku;
    component.Category = request.Category;
    component.UnitOfMeasure = request.UnitOfMeasure;
    component.UnitPrice = request.UnitPrice;
    component.Material = request.Material;
    component.Dimensions = request.Dimensions;
    component.UpdatedAt = DateTime.UtcNow;

    await db.SaveChangesAsync(ct);
    return Results.Ok(component);
})
.RequireAuthorization()
.WithName("UpdateComponent")
.WithTags("Components");

app.MapDelete("/api/components/{id}", async (string id, ApplicationDbContext db, ICurrentUserService currentUser, CancellationToken ct) =>
{
    if (!currentUser.IsAuthenticated)
        return Results.Unauthorized();

    var component = await db.Components.FirstOrDefaultAsync(c => c.Id == id && c.OrganizationId == currentUser.OrganizationId, ct);
    if (component == null)
        return Results.NotFound();

    db.Components.Remove(component);
    await db.SaveChangesAsync(ct);
    return Results.Ok(new { success = true });
})
.RequireAuthorization()
.WithName("DeleteComponent")
.WithTags("Components");

// Job endpoints
app.MapGet("/api/jobs", async (ApplicationDbContext db, ICurrentUserService currentUser, CancellationToken ct) =>
{
    if (!currentUser.IsAuthenticated)
        return Results.Unauthorized();

    var jobs = await db.Jobs
        .Where(j => j.OrganizationId == currentUser.OrganizationId)
        .OrderByDescending(j => j.CreatedAt)
        .ToListAsync(ct);
    return Results.Ok(jobs);
})
.RequireAuthorization()
.WithName("GetJobs")
.WithTags("Jobs");

app.MapGet("/api/jobs/{id}", async (string id, ApplicationDbContext db, ICurrentUserService currentUser, CancellationToken ct) =>
{
    if (!currentUser.IsAuthenticated)
        return Results.Unauthorized();

    var job = await db.Jobs
        .Include(j => j.LineItems)
            .ThenInclude(li => li.FenceType)
        .Include(j => j.LineItems)
            .ThenInclude(li => li.GateType)
        .FirstOrDefaultAsync(j => j.Id == id && j.OrganizationId == currentUser.OrganizationId, ct);
    
    return job != null ? Results.Ok(job) : Results.NotFound();
})
.RequireAuthorization()
.WithName("GetJobById")
.WithTags("Jobs");

app.MapPost("/api/jobs", async (Job request, ApplicationDbContext db, ICurrentUserService currentUser, CancellationToken ct) =>
{
    if (!currentUser.IsAuthenticated)
        return Results.Unauthorized();

    request.OrganizationId = currentUser.OrganizationId ?? string.Empty;
    request.Id = Guid.NewGuid().ToString();
    request.CreatedAt = DateTime.UtcNow;
    request.UpdatedAt = DateTime.UtcNow;

    db.Jobs.Add(request);
    await db.SaveChangesAsync(ct);
    return Results.Created($"/api/jobs/{request.Id}", request);
})
.RequireAuthorization()
.WithName("CreateJob")
.WithTags("Jobs");

app.MapPut("/api/jobs/{id}", async (string id, Job request, ApplicationDbContext db, ICurrentUserService currentUser, CancellationToken ct) =>
{
    if (!currentUser.IsAuthenticated)
        return Results.Unauthorized();

    var job = await db.Jobs.FirstOrDefaultAsync(j => j.Id == id && j.OrganizationId == currentUser.OrganizationId, ct);
    if (job == null)
        return Results.NotFound();

    job.Name = request.Name;
    job.CustomerName = request.CustomerName;
    job.CustomerEmail = request.CustomerEmail;
    job.CustomerPhone = request.CustomerPhone;
    job.InstallationAddress = request.InstallationAddress;
    job.Status = request.Status;
    job.TotalLinearFeet = request.TotalLinearFeet;
    job.LaborCost = request.LaborCost;
    job.MaterialsCost = request.MaterialsCost;
    job.TotalCost = request.TotalCost;
    job.Notes = request.Notes;
    job.EstimatedStartDate = request.EstimatedStartDate;
    job.EstimatedCompletionDate = request.EstimatedCompletionDate;
    job.UpdatedAt = DateTime.UtcNow;

    await db.SaveChangesAsync(ct);
    return Results.Ok(job);
})
.RequireAuthorization()
.WithName("UpdateJob")
.WithTags("Jobs");

app.MapDelete("/api/jobs/{id}", async (string id, ApplicationDbContext db, ICurrentUserService currentUser, CancellationToken ct) =>
{
    if (!currentUser.IsAuthenticated)
        return Results.Unauthorized();

    var job = await db.Jobs.FirstOrDefaultAsync(j => j.Id == id && j.OrganizationId == currentUser.OrganizationId, ct);
    if (job == null)
        return Results.NotFound();

    db.Jobs.Remove(job);
    await db.SaveChangesAsync(ct);
    return Results.Ok(new { success = true });
})
.RequireAuthorization()
.WithName("DeleteJob")
.WithTags("Jobs");

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
