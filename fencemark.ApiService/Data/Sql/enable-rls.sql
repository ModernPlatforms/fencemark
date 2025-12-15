-- ============================================================================
-- Row-Level Security (RLS) Setup for Multi-Tenant Data Isolation
-- ============================================================================
-- This script enables Row-Level Security on all organization-scoped tables
-- to ensure complete data isolation between tenants at the database level.
-- ============================================================================

-- Create a schema for RLS functions if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'Security')
BEGIN
    EXEC('CREATE SCHEMA Security');
END
GO

-- ============================================================================
-- RLS Predicate Function
-- ============================================================================
-- This function determines if a row should be accessible based on the current
-- user's organization context. It uses SESSION_CONTEXT to get the OrganizationId.

CREATE OR ALTER FUNCTION Security.fn_TenantAccessPredicate(@OrganizationId NVARCHAR(450))
RETURNS TABLE
WITH SCHEMABINDING
AS
RETURN
    SELECT 1 AS fn_TenantAccessPredicate_result
    WHERE
        @OrganizationId = CAST(SESSION_CONTEXT(N'OrganizationId') AS NVARCHAR(450))
        OR CAST(SESSION_CONTEXT(N'OrganizationId') AS NVARCHAR(450)) IS NULL; -- Allow NULL for system operations
GO

-- ============================================================================
-- Security Policies for Organization-Scoped Tables
-- ============================================================================

-- Components Table
IF EXISTS (SELECT * FROM sys.security_policies WHERE name = 'ComponentsSecurityPolicy')
    DROP SECURITY POLICY Security.ComponentsSecurityPolicy;
GO

CREATE SECURITY POLICY Security.ComponentsSecurityPolicy
    ADD FILTER PREDICATE Security.fn_TenantAccessPredicate(OrganizationId) ON dbo.Components,
    ADD BLOCK PREDICATE Security.fn_TenantAccessPredicate(OrganizationId) ON dbo.Components AFTER INSERT,
    ADD BLOCK PREDICATE Security.fn_TenantAccessPredicate(OrganizationId) ON dbo.Components AFTER UPDATE
WITH (STATE = ON);
GO

-- FenceTypes Table
IF EXISTS (SELECT * FROM sys.security_policies WHERE name = 'FenceTypesSecurityPolicy')
    DROP SECURITY POLICY Security.FenceTypesSecurityPolicy;
GO

CREATE SECURITY POLICY Security.FenceTypesSecurityPolicy
    ADD FILTER PREDICATE Security.fn_TenantAccessPredicate(OrganizationId) ON dbo.FenceTypes,
    ADD BLOCK PREDICATE Security.fn_TenantAccessPredicate(OrganizationId) ON dbo.FenceTypes AFTER INSERT,
    ADD BLOCK PREDICATE Security.fn_TenantAccessPredicate(OrganizationId) ON dbo.FenceTypes AFTER UPDATE
WITH (STATE = ON);
GO

-- GateTypes Table
IF EXISTS (SELECT * FROM sys.security_policies WHERE name = 'GateTypesSecurityPolicy')
    DROP SECURITY POLICY Security.GateTypesSecurityPolicy;
GO

CREATE SECURITY POLICY Security.GateTypesSecurityPolicy
    ADD FILTER PREDICATE Security.fn_TenantAccessPredicate(OrganizationId) ON dbo.GateTypes,
    ADD BLOCK PREDICATE Security.fn_TenantAccessPredicate(OrganizationId) ON dbo.GateTypes AFTER INSERT,
    ADD BLOCK PREDICATE Security.fn_TenantAccessPredicate(OrganizationId) ON dbo.GateTypes AFTER UPDATE
WITH (STATE = ON);
GO

-- Jobs Table
IF EXISTS (SELECT * FROM sys.security_policies WHERE name = 'JobsSecurityPolicy')
    DROP SECURITY POLICY Security.JobsSecurityPolicy;
GO

CREATE SECURITY POLICY Security.JobsSecurityPolicy
    ADD FILTER PREDICATE Security.fn_TenantAccessPredicate(OrganizationId) ON dbo.Jobs,
    ADD BLOCK PREDICATE Security.fn_TenantAccessPredicate(OrganizationId) ON dbo.Jobs AFTER INSERT,
    ADD BLOCK PREDICATE Security.fn_TenantAccessPredicate(OrganizationId) ON dbo.Jobs AFTER UPDATE
WITH (STATE = ON);
GO

-- PricingConfigs Table
IF EXISTS (SELECT * FROM sys.security_policies WHERE name = 'PricingConfigsSecurityPolicy')
    DROP SECURITY POLICY Security.PricingConfigsSecurityPolicy;
GO

CREATE SECURITY POLICY Security.PricingConfigsSecurityPolicy
    ADD FILTER PREDICATE Security.fn_TenantAccessPredicate(OrganizationId) ON dbo.PricingConfigs,
    ADD BLOCK PREDICATE Security.fn_TenantAccessPredicate(OrganizationId) ON dbo.PricingConfigs AFTER INSERT,
    ADD BLOCK PREDICATE Security.fn_TenantAccessPredicate(OrganizationId) ON dbo.PricingConfigs AFTER UPDATE
WITH (STATE = ON);
GO

-- Quotes Table
IF EXISTS (SELECT * FROM sys.security_policies WHERE name = 'QuotesSecurityPolicy')
    DROP SECURITY POLICY Security.QuotesSecurityPolicy;
GO

CREATE SECURITY POLICY Security.QuotesSecurityPolicy
    ADD FILTER PREDICATE Security.fn_TenantAccessPredicate(OrganizationId) ON dbo.Quotes,
    ADD BLOCK PREDICATE Security.fn_TenantAccessPredicate(OrganizationId) ON dbo.Quotes AFTER INSERT,
    ADD BLOCK PREDICATE Security.fn_TenantAccessPredicate(OrganizationId) ON dbo.Quotes AFTER UPDATE
WITH (STATE = ON);
GO

PRINT 'Row-Level Security policies have been successfully created.';
PRINT 'Use EXEC sp_set_session_context @key = N''OrganizationId'', @value = N''<org-id>'' to set the tenant context.';
GO
