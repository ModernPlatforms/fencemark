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

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
