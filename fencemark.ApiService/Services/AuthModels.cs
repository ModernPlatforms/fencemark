namespace fencemark.ApiService.Services;

/// <summary>
/// Request model for user registration
/// </summary>
public record RegisterRequest
{
    public required string Email { get; init; }
    public required string Password { get; init; }
    public required string OrganizationName { get; init; }
}

/// <summary>
/// Request model for user login
/// </summary>
public record LoginRequest
{
    public required string Email { get; init; }
    public required string Password { get; init; }
}

/// <summary>
/// Response model for authentication operations
/// </summary>
public record AuthResponse
{
    public bool Success { get; init; }
    public string? Message { get; init; }
    public string? UserId { get; init; }
    public string? OrganizationId { get; init; }
    public string? Email { get; init; }
    public bool IsGuest { get; init; }
}

/// <summary>
/// Request model for inviting a user to an organization
/// </summary>
public record InviteUserRequest
{
    public required string Email { get; init; }
    public required string Role { get; init; }
}

/// <summary>
/// Response model for user invitation
/// </summary>
public record InviteUserResponse
{
    public bool Success { get; init; }
    public string? Message { get; init; }
    public string? InvitationToken { get; init; }
}

/// <summary>
/// Request model for accepting an invitation
/// </summary>
public record AcceptInvitationRequest
{
    public required string Token { get; init; }
    public required string Password { get; init; }
}

/// <summary>
/// Response model for organization member information
/// </summary>
public record OrganizationMemberResponse
{
    public required string UserId { get; init; }
    public required string Email { get; init; }
    public required string Role { get; init; }
    public DateTime JoinedAt { get; init; }
    public bool IsGuest { get; init; }
}

/// <summary>
/// Request model for updating a user's role
/// </summary>
public record UpdateRoleRequest
{
    public required string UserId { get; init; }
    public required string Role { get; init; }
}

/// <summary>
/// Request model for external identity login (Entra External ID)
/// </summary>
public record ExternalLoginRequest
{
    public required string Email { get; init; }
    public required string ExternalId { get; init; }
    public required string Provider { get; init; }
    public string? GivenName { get; init; }
    public string? FamilyName { get; init; }
    public required string OrganizationName { get; init; }
}
