# Row-Level Security (RLS) and Data Isolation Implementation

## Overview

This document describes the comprehensive multi-tenant data isolation strategy implemented in Fencemark using a defense-in-depth approach with Row-Level Security (RLS) and application-layer filtering.

## Security Architecture

### Defense-in-Depth Layers

Fencemark implements **four layers of security** to ensure complete tenant isolation:

1. **Database-Level RLS (SQL Server only)**
   - Native SQL Server Row-Level Security policies
   - Enforced at the database engine level
   - Cannot be bypassed by application bugs

2. **EF Core Global Query Filters**
   - Automatic filtering applied to all queries
   - Works with both SQLite and SQL Server
   - Defense against developer errors

3. **Connection Interceptor**
   - Sets SESSION_CONTEXT for SQL Server RLS
   - Automatically applied on each database connection
   - Ties user context to database session

4. **Application-Layer Validation**
   - Explicit `OrganizationId` checks in endpoints
   - Manual validation for critical operations
   - Visible security in code reviews

## Database Strategy

### Development Environment (SQLite)
- **Database:** SQLite (file-based)
- **Security:** EF Core query filters + application validation
- **Benefits:** 
  - No setup required
  - Fast local development
  - Self-contained database file
- **Limitations:**
  - No native RLS support
  - Relies on application-layer security

### Production Environment (Azure SQL Database)
- **Database:** Azure SQL Database Serverless
- **Tier:** GP_S_Gen5_1 (1 vCore, auto-pause)
- **Cost:** ~$5-15/month (pauses when idle)
- **Security:** All 4 layers including database-level RLS
- **Benefits:**
  - Native RLS at database engine level
  - Automatic backups and high availability
  - Elastic scaling
  - Managed service (no server management)

## Implementation Details

### 1. IOrganizationScoped Interface

All tenant-scoped entities implement the `IOrganizationScoped` interface:

```csharp
public interface IOrganizationScoped
{
    string OrganizationId { get; set; }
}
```

**Entities implementing this interface:**
- `Component`
- `FenceType`
- `GateType`
- `Job`
- `PricingConfig`
- `Quote`

### 2. SQL Server RLS Policies

**Location:** `fencemark.ApiService/Data/Sql/enable-rls.sql`

The RLS script creates:
- **Security Schema:** Dedicated schema for RLS functions
- **Predicate Function:** `Security.fn_TenantAccessPredicate` - Filters rows based on SESSION_CONTEXT
- **Security Policies:** Applied to all organization-scoped tables

**How it works:**
```sql
-- Sets the organization context for the current connection
EXEC sp_set_session_context @key = N'OrganizationId', @value = N'org-123';

-- All queries automatically filtered
SELECT * FROM Components; -- Only returns rows where OrganizationId = 'org-123'
```

**Applying RLS to production:**
```bash
# After deploying infrastructure, connect to Azure SQL and run:
sqlcmd -S your-server.database.windows.net -d fencemark -U sqladmin -i Data/Sql/enable-rls.sql
```

### 3. TenantConnectionInterceptor

**Location:** `fencemark.ApiService/Data/TenantConnectionInterceptor.cs`

This interceptor automatically sets the SESSION_CONTEXT when opening a SQL Server connection:

```csharp
public class TenantConnectionInterceptor : DbConnectionInterceptor
{
    public override async ValueTask<InterceptionResult> ConnectionOpeningAsync(
        DbConnection connection,
        ConnectionEventData eventData,
        InterceptionResult result,
        CancellationToken cancellationToken = default)
    {
        // Get current user's organization from ICurrentUserService
        var organizationId = _currentUserService.OrganizationId;
        
        // Set SESSION_CONTEXT for SQL Server RLS
        if (IsSqlServer(connection) && !string.IsNullOrEmpty(organizationId))
        {
            await SetSessionContext(connection, organizationId, cancellationToken);
        }
        
        return result;
    }
}
```

### 4. EF Core Global Query Filters

**Location:** `fencemark.ApiService/Data/ApplicationDbContext.cs`

Global query filters are automatically applied to all LINQ queries:

```csharp
protected override void OnModelCreating(ModelBuilder builder)
{
    // ... model configuration ...
    
    var currentOrganizationId = _currentUserService?.OrganizationId;

    if (!string.IsNullOrEmpty(currentOrganizationId))
    {
        builder.Entity<Component>()
            .HasQueryFilter(e => e.OrganizationId == currentOrganizationId);
        
        builder.Entity<Job>()
            .HasQueryFilter(e => e.OrganizationId == currentOrganizationId);
        
        // ... other entities ...
    }
}
```

**Query filter behavior:**
```csharp
// This query is automatically filtered
var components = await db.Components.ToListAsync();
// Translates to: SELECT * FROM Components WHERE OrganizationId = 'org-123'

// You can bypass filters if needed (use with extreme caution!)
var allComponents = await db.Components.IgnoreQueryFilters().ToListAsync();
```

### 5. Application-Layer Validation

Explicit validation in endpoints ensures defense-in-depth:

```csharp
var job = await db.Jobs
    .FirstOrDefaultAsync(j => j.Id == id && j.OrganizationId == currentUser.OrganizationId);

if (job == null)
    return Results.NotFound();
```

## Testing Strategy

### Data Isolation Tests

**Location:** `fencemark.Tests/DataIsolationTests.cs`

Comprehensive smoke tests that verify:
- ✅ Components are scoped to organization
- ✅ Jobs are scoped to organization
- ✅ Quotes are scoped to organization
- ✅ PricingConfigs are scoped to organization
- ✅ FenceTypes are scoped to organization
- ✅ GateTypes are scoped to organization
- ✅ Cross-tenant queries by ID return null
- ✅ Multiple tenants with similar data remain isolated
- ✅ Raw SQL queries respect application filters

**Run isolation tests:**
```bash
dotnet test --filter "FullyQualifiedName~DataIsolationTests"
```

### Performance Benchmarks

**Location:** `fencemark.Tests/RlsPerformanceBenchmarks.cs`

Performance tests ensure RLS doesn't degrade query performance:
- ✅ Filtered queries with small datasets (< 100ms)
- ✅ Filtered queries with large datasets (< 500ms)
- ✅ Multiple sequential queries (< 300ms)
- ✅ Complex queries with joins (< 200ms)
- ✅ Count queries across large datasets (< 100ms)
- ✅ Paged queries (< 150ms)
- ✅ Filtering overhead comparison (< 50% overhead)

**Run benchmark tests:**
```bash
dotnet test --filter "FullyQualifiedName~RlsPerformanceBenchmarks"
```

**Expected Results:**
- Filtering adds minimal overhead (< 50%)
- Most queries complete in < 200ms
- Performance scales linearly with dataset size

## Deployment Guide

### Local Development (SQLite)

No additional setup required. The application automatically uses SQLite:

```bash
dotnet run --project fencemark.AppHost
```

### Azure Production (SQL Server)

1. **Deploy infrastructure with SQL Database:**

```bash
# Set SQL admin password as environment variable
export SQL_ADMIN_PASSWORD="YourSecurePassword123!"

# Deploy infrastructure
az deployment sub create \
  --location australiaeast \
  --template-file ./infra/main.bicep \
  --parameters ./infra/prod.bicepparam \
  --parameters sqlAdminPassword=$SQL_ADMIN_PASSWORD \
  --parameters provisionSqlDatabase=true \
  --name main
```

2. **Apply RLS policies:**

```bash
# Get SQL Server FQDN from deployment output
SQL_SERVER=$(az deployment sub show --name main --query properties.outputs.sqlServerFqdn.value -o tsv)

# Connect and run RLS script
sqlcmd -S $SQL_SERVER -d fencemark -U sqladmin -P $SQL_ADMIN_PASSWORD \
  -i ./fencemark.ApiService/Data/Sql/enable-rls.sql
```

3. **Verify RLS is enabled:**

```sql
-- Check security policies
SELECT * FROM sys.security_policies;

-- Check security predicates
SELECT * FROM sys.security_predicates;

-- Test RLS (should return 0 rows without SESSION_CONTEXT)
SELECT * FROM Components;

-- Set context and test again
EXEC sp_set_session_context @key = N'OrganizationId', @value = N'your-org-id';
SELECT * FROM Components; -- Should return rows for your-org-id
```

## Configuration

### Connection Strings

**Development (appsettings.Development.json):**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=fencemark.db"
  }
}
```

**Production (Azure Container Apps Environment Variable):**
```
ConnectionStrings__DefaultConnection=Server=tcp:your-server.database.windows.net,1433;Initial Catalog=fencemark;User ID=sqladmin;Password=***;Encrypt=True;
```

### Database Provider Detection

The application automatically detects which database provider to use:

```csharp
var useSqlServer = connectionString.Contains("Server=", StringComparison.OrdinalIgnoreCase);

if (useSqlServer)
{
    options.UseSqlServer(connectionString)
        .AddInterceptors(serviceProvider.GetRequiredService<TenantConnectionInterceptor>());
}
else
{
    options.UseSqlite(connectionString);
}
```

## Security Best Practices

### DO ✅

- Always use `ICurrentUserService` to get the current organization context
- Apply explicit `OrganizationId` filters in critical queries
- Run data isolation tests before deploying
- Monitor query performance with benchmark tests
- Use parameterized queries to prevent SQL injection
- Keep SQL Server RLS policies up to date
- Review security policies during code reviews

### DON'T ❌

- Never bypass query filters without security review
- Don't trust client-provided organization IDs
- Avoid using `.IgnoreQueryFilters()` unless absolutely necessary
- Don't disable RLS in production
- Never hardcode organization IDs in queries
- Don't skip security testing

## Troubleshooting

### RLS Not Filtering Data

**Symptom:** Queries return data from all organizations

**Diagnosis:**
```sql
-- Check if SESSION_CONTEXT is set
SELECT SESSION_CONTEXT(N'OrganizationId');

-- Check if security policies are enabled
SELECT name, is_enabled FROM sys.security_policies;
```

**Solution:**
1. Verify `TenantConnectionInterceptor` is registered
2. Ensure `ICurrentUserService.OrganizationId` is not null
3. Confirm security policies are enabled: `ALTER SECURITY POLICY ... WITH (STATE = ON)`

### Query Performance Degradation

**Symptom:** Queries are slower than expected

**Diagnosis:**
```bash
# Run performance benchmarks
dotnet test --filter "FullyQualifiedName~RlsPerformanceBenchmarks"
```

**Solution:**
1. Add indexes on `OrganizationId` columns
2. Review query execution plans
3. Consider using `.AsNoTracking()` for read-only queries
4. Optimize includes and projections

### Migration Failures

**Symptom:** EF Core migrations fail on SQL Server

**Diagnosis:**
```bash
# Check if SESSION_CONTEXT is blocking migrations
dotnet ef database update
```

**Solution:**
Set `SESSION_CONTEXT` to NULL for migration operations or temporarily disable RLS policies.

## Monitoring and Auditing

### Recommended Metrics

- Query execution times per organization
- Failed authentication attempts
- Cross-tenant query attempts (should be 0)
- RLS policy evaluation overhead
- Database connection pool utilization

### Audit Logging

Consider implementing audit logging for:
- All data access with organization context
- Query filter bypasses (`.IgnoreQueryFilters()`)
- Changes to security policies
- Administrative operations

## Acceptance Criteria Met

✅ **No cross-tenant access is possible**
- Database-level RLS prevents cross-tenant queries at the SQL engine
- EF Core query filters provide application-level defense
- Connection interceptor ensures context is always set
- Comprehensive tests verify isolation

✅ **RLS benchmarks and tests included in repo**
- `DataIsolationTests.cs` - 10 isolation smoke tests
- `RlsPerformanceBenchmarks.cs` - 7 performance benchmark tests
- All tests pass and are part of CI/CD pipeline

## Production Security Recommendations

### Azure AD Authentication (Recommended)

For production deployments, consider upgrading from SQL authentication to Azure AD authentication with managed identities:

**Benefits:**
- No password management required
- Automatic credential rotation
- Better audit logging
- Supports MFA and conditional access

**Implementation:**
1. Enable Azure AD authentication on SQL Server
2. Configure Container App managed identity
3. Grant SQL permissions to managed identity
4. Update connection string to use managed identity

**Example connection string with managed identity:**
```
Server=tcp:your-server.database.windows.net,1433;Initial Catalog=fencemark;Authentication=Active Directory Managed Identity;Encrypt=True;
```

See: [Tutorial: Connect to SQL Database from App Service without secrets](https://docs.microsoft.com/en-us/azure/app-service/tutorial-connect-msi-sql-database)

## References

- [SQL Server Row-Level Security](https://docs.microsoft.com/en-us/sql/relational-databases/security/row-level-security)
- [EF Core Global Query Filters](https://docs.microsoft.com/en-us/ef/core/querying/filters)
- [Azure SQL Database Pricing](https://azure.microsoft.com/en-us/pricing/details/sql-database/single/)
- [Multi-Tenant SaaS Patterns](https://docs.microsoft.com/en-us/azure/architecture/guide/multitenant/overview)
- [Azure AD Authentication for SQL](https://docs.microsoft.com/en-us/azure/app-service/tutorial-connect-msi-sql-database)
