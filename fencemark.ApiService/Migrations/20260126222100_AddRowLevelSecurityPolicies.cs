using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace fencemark.ApiService.Migrations
{
    /// <inheritdoc />
    public partial class AddRowLevelSecurityPolicies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create the security predicate function that checks OrganizationId against SESSION_CONTEXT
            migrationBuilder.Sql(@"
                CREATE FUNCTION dbo.fn_SecurityPredicate(@OrganizationId nvarchar(450))
                RETURNS TABLE
                WITH SCHEMABINDING
                AS
                RETURN SELECT 1 AS fn_SecurityPredicate_result
                WHERE 
                    @OrganizationId = CAST(SESSION_CONTEXT(N'OrganizationId') AS nvarchar(450))
                    OR CAST(SESSION_CONTEXT(N'OrganizationId') AS nvarchar(450)) IS NULL
            ");

            // Create the security policy with filter predicates for all organization-scoped tables
            migrationBuilder.Sql(@"
                CREATE SECURITY POLICY dbo.TenantFilterPolicy
                ADD FILTER PREDICATE dbo.fn_SecurityPredicate(OrganizationId) ON dbo.Components,
                ADD FILTER PREDICATE dbo.fn_SecurityPredicate(OrganizationId) ON dbo.DiscountRules,
                ADD FILTER PREDICATE dbo.fn_SecurityPredicate(OrganizationId) ON dbo.Drawings,
                ADD FILTER PREDICATE dbo.fn_SecurityPredicate(OrganizationId) ON dbo.FenceSegments,
                ADD FILTER PREDICATE dbo.fn_SecurityPredicate(OrganizationId) ON dbo.FenceTypes,
                ADD FILTER PREDICATE dbo.fn_SecurityPredicate(OrganizationId) ON dbo.GatePositions,
                ADD FILTER PREDICATE dbo.fn_SecurityPredicate(OrganizationId) ON dbo.GateTypes,
                ADD FILTER PREDICATE dbo.fn_SecurityPredicate(OrganizationId) ON dbo.Jobs,
                ADD FILTER PREDICATE dbo.fn_SecurityPredicate(OrganizationId) ON dbo.Parcels,
                ADD FILTER PREDICATE dbo.fn_SecurityPredicate(OrganizationId) ON dbo.PricingConfigs,
                ADD FILTER PREDICATE dbo.fn_SecurityPredicate(OrganizationId) ON dbo.Quotes,
                ADD FILTER PREDICATE dbo.fn_SecurityPredicate(OrganizationId) ON dbo.TaxRegions
                WITH (STATE = ON);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop the security policy first
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.security_policies WHERE name = N'TenantFilterPolicy')
                    DROP SECURITY POLICY dbo.TenantFilterPolicy;
            ");

            // Drop the security predicate function
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.objects WHERE type = 'IF' AND name = N'fn_SecurityPredicate')
                    DROP FUNCTION dbo.fn_SecurityPredicate;
            ");
        }
    }
}
