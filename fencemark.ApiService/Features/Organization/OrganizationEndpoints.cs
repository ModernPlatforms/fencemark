using fencemark.ApiService.Middleware;
using fencemark.ApiService.Services;

namespace fencemark.ApiService.Features.Organization;

public static class OrganizationEndpoints
{
    public static IEndpointRouteBuilder MapOrganizationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/organizations")
            .WithTags("Organization");

        group.MapGet("/{organizationId}/members", GetMembers)
            .RequireAuthorization()
            .WithName("GetOrganizationMembers");

        group.MapPost("/{organizationId}/invite", InviteUser)
            .RequireAuthorization()
            .WithName("InviteUser");

        group.MapPost("/accept-invitation", AcceptInvitation)
            .WithName("AcceptInvitation");

        group.MapPut("/{organizationId}/members/role", UpdateRole)
            .RequireAuthorization()
            .WithName("UpdateMemberRole");

        group.MapDelete("/{organizationId}/members/{userId}", RemoveMember)
            .RequireAuthorization()
            .WithName("RemoveMember");

        group.MapPost("/seed-sample-data", SeedSampleData)
            .RequireAuthorization()
            .WithName("SeedSampleData");

        return app;
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

        // Check if sample data already exists
        if (await seedService.HasSampleDataAsync(currentUser.OrganizationId))
        {
            return Results.BadRequest(new { success = false, message = "Sample data already exists for this organization" });
        }

        await seedService.SeedSampleDataAsync(currentUser.OrganizationId);
        return Results.Ok(new { success = true, message = "Sample data seeded successfully" });
    }
}
