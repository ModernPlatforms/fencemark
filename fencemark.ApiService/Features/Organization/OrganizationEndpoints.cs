using fencemark.ApiService.Infrastructure;
using fencemark.ApiService.Middleware;
using fencemark.ApiService.Services;

namespace fencemark.ApiService.Features.Organization;

public static class OrganizationEndpoints
{
    public static IEndpointRouteBuilder MapOrganizationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/organizations")
            .WithTags("Organization");

        group.MapPost("/create", CreateOrganization)
            .RequireAuthorization()
            .WithName("CreateOrganization");

        group.MapPut("/{organizationId}", UpdateOrganization)
            .RequireAuthorization(AuthorizationPolicies.AdminOnly)
            .WithName("UpdateOrganization");

        group.MapGet("/{organizationId}/members", GetMembers)
            .RequireAuthorization()
            .WithName("GetOrganizationMembers");

        group.MapPost("/{organizationId}/invite", InviteUser)
            .RequireAuthorization(AuthorizationPolicies.AdminOnly)
            .WithName("InviteUser");

        group.MapPost("/accept-invitation", AcceptInvitation)
            .WithName("AcceptInvitation");

        group.MapPut("/{organizationId}/members/role", UpdateRole)
            .RequireAuthorization(AuthorizationPolicies.AdminOnly)
            .WithName("UpdateMemberRole");

        group.MapDelete("/{organizationId}/members/{userId}", RemoveMember)
            .RequireAuthorization(AuthorizationPolicies.AdminOnly)
            .WithName("RemoveMember");

        group.MapPost("/seed-sample-data", SeedSampleData)
            .RequireAuthorization()
            .WithName("SeedSampleData");

        return app;
    }

    private static async Task<IResult> CreateOrganization(
        CreateOrganizationRequest request,
        IOrganizationService orgService,
        ICurrentUserService currentUser,
        ILogger<Program> logger,
        CancellationToken ct)
    {
        logger.LogInformation("[CreateOrganization] IsAuthenticated: {IsAuth}, UserId: {UserId}, Email: {Email}", 
            currentUser.IsAuthenticated, currentUser.UserId, currentUser.Email);

        if (!currentUser.IsAuthenticated || string.IsNullOrEmpty(currentUser.UserId))
        {
            logger.LogWarning("[CreateOrganization] Unauthorized - IsAuthenticated: {IsAuth}, UserId: {UserId}", 
                currentUser.IsAuthenticated, currentUser.UserId);
            return Results.Unauthorized();
        }

        var result = await orgService.CreateOrganizationAsync(currentUser.UserId, request, ct);
        logger.LogInformation("[CreateOrganization] Result: Success={Success}, Message={Message}", 
            result.Success, result.Message);
        return result.Success ? Results.Ok(result) : Results.BadRequest(result);
    }

    private static async Task<IResult> UpdateOrganization(
        string organizationId,
        UpdateOrganizationRequest request,
        IOrganizationService orgService,
        ICurrentUserService currentUser,
        CancellationToken ct)
    {
        if (currentUser.OrganizationId != organizationId)
        {
            return Results.Forbid();
        }

        var result = await orgService.UpdateOrganizationAsync(organizationId, request, ct);
        return result.Success ? Results.Ok(result) : Results.BadRequest(result);
    }

    private static async Task<IResult> GetMembers(
        string organizationId,
        IOrganizationService orgService,
        ICurrentUserService currentUser,
        CancellationToken ct)
    {
        if (currentUser.OrganizationId != organizationId)
        {
            return Results.Forbid();
        }

        var members = await orgService.GetMembersAsync(organizationId, ct);
        return Results.Ok(members);
    }

    private static async Task<IResult> InviteUser(
        string organizationId,
        InviteUserRequest request,
        IOrganizationService orgService,
        ICurrentUserService currentUser,
        CancellationToken ct)
    {
        if (currentUser.OrganizationId != organizationId)
        {
            return Results.Forbid();
        }

        var result = await orgService.InviteUserAsync(organizationId, request, ct);
        return result.Success ? Results.Ok(result) : Results.BadRequest(result);
    }

    private static async Task<IResult> AcceptInvitation(
        AcceptInvitationRequest request,
        IOrganizationService orgService,
        CancellationToken ct)
    {
        var result = await orgService.AcceptInvitationAsync(request, ct);
        return result.Success ? Results.Ok(result) : Results.BadRequest(result);
    }

    private static async Task<IResult> UpdateRole(
        string organizationId,
        UpdateRoleRequest request,
        IOrganizationService orgService,
        ICurrentUserService currentUser,
        CancellationToken ct)
    {
        if (currentUser.OrganizationId != organizationId)
        {
            return Results.Forbid();
        }

        var success = await orgService.UpdateRoleAsync(organizationId, request, ct);
        return success ? Results.Ok(new { success = true }) : Results.BadRequest(new { success = false, message = "Failed to update role" });
    }

    private static async Task<IResult> RemoveMember(
        string organizationId,
        string userId,
        IOrganizationService orgService,
        ICurrentUserService currentUser,
        CancellationToken ct)
    {
        if (currentUser.OrganizationId != organizationId)
        {
            return Results.Forbid();
        }

        var success = await orgService.RemoveMemberAsync(organizationId, userId, ct);
        return success ? Results.Ok(new { success = true }) : Results.BadRequest(new { success = false, message = "Failed to remove member" });
    }

    private static async Task<IResult> SeedSampleData(
        ISeedDataService seedService,
        ICurrentUserService currentUser,
        CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated || string.IsNullOrEmpty(currentUser.OrganizationId))
        {
            return Results.Unauthorized();
        }

        // Allow seeding even if data exists - users may want to add more sample data
        // or re-seed after deleting some entities
        await seedService.SeedSampleDataAsync(currentUser.OrganizationId);
        return Results.Ok(new { success = true, message = "Sample data seeded successfully" });
    }
}
