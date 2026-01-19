using fencemark.ApiService.Data;
using fencemark.ApiService.Data.Models;
using fencemark.ApiService.Middleware;
using fencemark.ApiService.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace fencemark.ApiService.Features.Auth;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth")
            .WithTags("Authentication");

        group.MapPost("/register", Register)
            .WithName("Register");

        group.MapPost("/login", Login)
            .WithName("Login");

        group.MapPost("/external-login", ExternalLogin)
            .WithName("ExternalLogin");

        group.MapPost("/logout", Logout)
            .RequireAuthorization()
            .WithName("Logout");

        group.MapGet("/me", GetCurrentUser)
            .RequireAuthorization()
            .WithName("GetCurrentUser");

        group.MapPut("/me", UpdateCurrentUser)
            .RequireAuthorization()
            .WithName("UpdateCurrentUser");

        group.MapDelete("/account", DeleteAccount)
            .RequireAuthorization()
            .WithName("DeleteAccount");

        return app;
    }

    private static async Task<IResult> Register(
        RegisterRequest request,
        IAuthService authService,
        CancellationToken ct)
    {
        var result = await authService.RegisterAsync(request, ct);
        return result.Success ? Results.Ok(result) : Results.BadRequest(result);
    }

    private static async Task<IResult> Login(
        LoginRequest request,
        IAuthService authService,
        CancellationToken ct)
    {
        var result = await authService.LoginAsync(request, ct);
        return result.Success ? Results.Ok(result) : Results.Unauthorized();
    }

    private static async Task<IResult> ExternalLogin(
        ExternalLoginRequest request,
        IAuthService authService,
        CancellationToken ct)
    {
        var result = await authService.ExternalLoginAsync(request, ct);
        return result.Success ? Results.Ok(result) : Results.BadRequest(result);
    }

    private static async Task<IResult> Logout(SignInManager<ApplicationUser> signInManager)
    {
        await signInManager.SignOutAsync();
        return Results.Ok(new { success = true, message = "Logged out successfully" });
    }

    private static async Task<IResult> GetCurrentUser(
        ICurrentUserService currentUser,
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext db,
        CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated || string.IsNullOrEmpty(currentUser.UserId))
        {
            return Results.Unauthorized();
        }

        var user = await userManager.FindByIdAsync(currentUser.UserId);
        if (user == null)
        {
            return Results.NotFound();
        }

        var membership = await db.OrganizationMembers
            .Include(m => m.Organization)
            .FirstOrDefaultAsync(m => m.UserId == user.Id, ct);

        return Results.Ok(new
        {
            userId = user.Id,
            email = user.Email,
            userName = user.UserName,
            organizationId = membership?.OrganizationId,
            organizationName = membership?.Organization?.Name,
            isEmailVerified = user.IsEmailVerified,
            isGuest = user.IsGuest,
            createdAt = user.CreatedAt
        });
    }

    private static async Task<IResult> UpdateCurrentUser(
        UpdateUserRequest request,
        ICurrentUserService currentUser,
        UserManager<ApplicationUser> userManager,
        CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated || string.IsNullOrEmpty(currentUser.UserId))
        {
            return Results.Unauthorized();
        }

        var user = await userManager.FindByIdAsync(currentUser.UserId);
        if (user == null)
        {
            return Results.NotFound();
        }

        // Update email if changed
        if (!string.IsNullOrEmpty(request.Email) && request.Email != user.Email)
        {
            var emailResult = await userManager.SetEmailAsync(user, request.Email);
            if (!emailResult.Succeeded)
            {
                return Results.BadRequest(new
                {
                    success = false,
                    message = "Failed to update email",
                    errors = emailResult.Errors.Select(e => e.Description)
                });
            }
            
            // Also update username to match email
            await userManager.SetUserNameAsync(user, request.Email);
        }

        // Update password if provided
        if (!string.IsNullOrEmpty(request.CurrentPassword) && !string.IsNullOrEmpty(request.NewPassword))
        {
            var passwordResult = await userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
            if (!passwordResult.Succeeded)
            {
                return Results.BadRequest(new
                {
                    success = false,
                    message = "Failed to update password",
                    errors = passwordResult.Errors.Select(e => e.Description)
                });
            }
        }

        return Results.Ok(new
        {
            success = true,
            message = "Account updated successfully",
            userId = user.Id,
            email = user.Email
        });
    }

    private static async Task<IResult> DeleteAccount(
        ICurrentUserService currentUser,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated || string.IsNullOrEmpty(currentUser.UserId))
        {
            return Results.Unauthorized();
        }

        var user = await userManager.FindByIdAsync(currentUser.UserId);
        if (user == null)
        {
            return Results.NotFound();
        }

        // Sign out the user first
        await signInManager.SignOutAsync();

        // Delete the user account
        var result = await userManager.DeleteAsync(user);
        
        if (!result.Succeeded)
        {
            return Results.BadRequest(new { 
                success = false, 
                message = "Failed to delete account",
                errors = result.Errors.Select(e => e.Description)
            });
        }

        return Results.Ok(new { success = true, message = "Account deleted successfully" });
    }
}
