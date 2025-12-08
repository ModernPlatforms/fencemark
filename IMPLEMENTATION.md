# B2B User Onboarding Implementation Guide

This document describes the complete B2B user onboarding system implemented in Fencemark.

## Overview

The system implements a multi-tenant B2B application where:
- Each user belongs to exactly one organization upon registration
- Organizations are automatically created when new users sign up
- The first user becomes the Owner of their organization
- Owners and Admins can invite additional users
- All data is completely isolated between organizations
- Users have guest status until their email is verified

## Architecture

### Database Models

#### ApplicationUser
Extends `IdentityUser` with additional fields:
- `IsEmailVerified`: Tracks email verification status
- `IsGuest`: Marks unverified users
- `CreatedAt`: Timestamp of account creation
- `OrganizationMemberships`: Navigation property to memberships

#### Organization
Represents a company or team account:
- `Id`: Unique identifier
- `Name`: Organization name
- `CreatedAt`: When the organization was created
- `Members`: Navigation property to all members

#### OrganizationMember
Links users to organizations with roles:
- `UserId` and `OrganizationId`: Foreign keys
- `Role`: Enum (Owner, Admin, Member, Billing, ReadOnly)
- `InvitedAt` and `JoinedAt`: Timestamps
- `InvitationToken`: For pending invitations
- `IsAccepted`: Whether invitation was accepted

### Services

#### AuthService
Handles authentication and user lifecycle:
- `RegisterAsync()`: Creates user + organization + owner membership
- `LoginAsync()`: Authenticates and returns user context
- `VerifyEmailAsync()`: Confirms email and removes guest status

#### OrganizationService
Manages organization membership:
- `GetMembersAsync()`: Lists all members of an organization
- `InviteUserAsync()`: Sends invitation with specific role
- `AcceptInvitationAsync()`: Processes invitation acceptance
- `UpdateRoleAsync()`: Changes a member's role
- `RemoveMemberAsync()`: Removes a user from organization

#### CurrentUserService
Provides access to authenticated user context:
- `UserId`: Current user's ID
- `Email`: Current user's email
- `OrganizationId`: User's organization ID
- `IsAuthenticated`: Whether user is logged in

### API Endpoints

#### Authentication
- `POST /api/auth/register` - Register new user with organization
- `POST /api/auth/login` - Authenticate user
- `POST /api/auth/logout` - Sign out user
- `GET /api/auth/me` - Get current user info

#### Organization Management
- `GET /api/organizations/{id}/members` - List organization members
- `POST /api/organizations/{id}/invite` - Invite new member
- `POST /api/organizations/accept-invitation` - Accept invitation
- `PUT /api/organizations/{id}/members/role` - Update member role
- `DELETE /api/organizations/{id}/members/{userId}` - Remove member

### Blazor UI Components

#### Register.razor
- Email, password, and organization name input
- Validates password complexity
- Creates user as organization owner
- Shows success/error messages

#### Login.razor
- Email and password authentication
- Redirects to organization dashboard on success
- Error handling for invalid credentials

#### Organization.razor
- Displays all organization members
- Shows member roles and guest status
- Invite member modal with role selection
- Change role modal for existing members
- Remove member functionality
- Complete data isolation enforcement

## Security Features

### Authentication
- ASP.NET Core Identity with secure password hashing
- Cookie-based authentication with HTTP-only, secure, and SameSite flags
- 30-day sliding expiration

### Authorization
- Role-based access control
- Organization ownership cannot be transferred or removed
- Only owners/admins can modify roles
- All endpoints validate organization membership

### Data Isolation
- `CurrentUserService` provides organization context
- All queries filter by organization ID
- Membership validation on every request
- No cross-organization data leakage

## Role Hierarchy

1. **Owner** - Full control, cannot be changed or removed, one per organization
2. **Admin** - Can manage members, assign roles (except Owner)
3. **Member** - Standard access to organization data
4. **Billing** - Access to billing and payment information
5. **ReadOnly** - View-only access to organization data

## User Flows

### New User Registration
1. User fills registration form (email, password, org name)
2. System creates ApplicationUser with `IsGuest = true`
3. System creates Organization
4. System creates OrganizationMember with Role.Owner
5. User is logged in automatically
6. Email verification token is generated (for future use)

### Email Verification
1. User clicks verification link (implementation ready)
2. System validates token
3. User's `IsEmailVerified` set to `true`
4. User's `IsGuest` set to `false`
5. User gains full access

### Inviting Users
1. Admin/Owner navigates to organization dashboard
2. Clicks "Invite Member"
3. Enters email and selects role
4. System creates invitation with token
5. Invitation email sent (implementation ready for email service)
6. Invited user receives link with token

### Accepting Invitation
1. User clicks invitation link
2. User sets password (if new) or logs in (if existing)
3. Membership marked as accepted
4. Guest status removed
5. User gains access to organization

## Testing Strategy

### Unit Tests
- AuthService: 7 tests covering registration, login, verification, errors
- OrganizationService: 8 tests covering invites, roles, data isolation, removal

### Test Coverage
✅ Organization auto-creation on signup
✅ Owner role assignment
✅ Guest status management
✅ Email verification flow
✅ User invitation with roles
✅ Role updates
✅ Member removal
✅ Data isolation between organizations
✅ Owner protection (cannot remove/change)
✅ Duplicate email handling
✅ Invalid password handling

### Integration Tests
The existing `WebTests.cs` can be extended to test full user flows through the Blazor UI.

## Database Migrations

Migrations are located in `fencemark.ApiService/Data/Migrations/`.

To create a new migration:
```bash
cd fencemark.ApiService
dotnet ef migrations add MigrationName
```

To update the database:
```bash
dotnet ef database update
```

The application automatically applies migrations on startup.

## Configuration

### Database
Default: SQLite (`Data Source=fencemark.db`)

To use SQL Server:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=Fencemark;Trusted_Connection=True;TrustServerCertificate=True"
  }
}
```

Update `Program.cs` to use `.UseSqlServer()` instead of `.UseSqlite()`.

### Identity Options
Configured in `Program.cs`:
- Password requirements (8 chars, upper, lower, digit)
- Unique email enforcement
- Email confirmation (currently disabled for simplicity)

## Future Enhancements

- [ ] Email service integration for verification and invitations
- [ ] Password reset functionality
- [ ] Two-factor authentication
- [ ] Organization settings page
- [ ] Audit logging for member changes
- [ ] Organization deletion
- [ ] Billing integration
- [ ] SSO/SAML support
- [ ] Advanced role permissions customization

## Production Considerations

1. **Database**: Migrate from SQLite to SQL Server or PostgreSQL
2. **Email**: Integrate SendGrid, AWS SES, or similar service
3. **Secrets**: Use Azure Key Vault, AWS Secrets Manager, or similar
4. **SSL**: Ensure HTTPS is enforced in production
5. **Rate Limiting**: Add rate limiting to auth endpoints
6. **Monitoring**: Enable Application Insights or similar APM
7. **Backup**: Implement database backup strategy
8. **GDPR**: Add user data export/deletion capabilities
