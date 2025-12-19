using fencemark.ApiService.Middleware;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Data.Common;

namespace fencemark.ApiService.Data;

/// <summary>
/// EF Core interceptor that sets the SESSION_CONTEXT for Row-Level Security in SQL Server
/// </summary>
public class TenantConnectionInterceptor : DbConnectionInterceptor
{
    private readonly ICurrentUserService _currentUserService;

    public TenantConnectionInterceptor(ICurrentUserService currentUserService)
    {
        _currentUserService = currentUserService;
    }

    public override async Task ConnectionOpenedAsync(
        DbConnection connection,
        ConnectionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        await base.ConnectionOpenedAsync(connection, eventData, cancellationToken);

        // Set the SESSION_CONTEXT for SQL Server RLS
        var organizationId = _currentUserService.OrganizationId;
        
        // Check if this is a SQL Server connection by examining the connection type
        var isSqlServer = connection.GetType().FullName?.Contains("SqlConnection", StringComparison.OrdinalIgnoreCase) ?? false;
        
        if (isSqlServer && !string.IsNullOrEmpty(organizationId))
        {
            using var command = connection.CreateCommand();
            command.CommandText = "EXEC sp_set_session_context @key = N'OrganizationId', @value = @organizationId;";
            var parameter = command.CreateParameter();
            parameter.ParameterName = "@organizationId";
            parameter.Value = organizationId;
            command.Parameters.Add(parameter);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    public override void ConnectionOpened(
        DbConnection connection,
        ConnectionEndEventData eventData)
    {
        base.ConnectionOpened(connection, eventData);

        // Set the SESSION_CONTEXT for SQL Server RLS (sync version)
        var organizationId = _currentUserService.OrganizationId;
        
        // Check if this is a SQL Server connection by examining the connection type
        var isSqlServer = connection.GetType().FullName?.Contains("SqlConnection", StringComparison.OrdinalIgnoreCase) ?? false;
        
        if (isSqlServer && !string.IsNullOrEmpty(organizationId))
        {
            using var command = connection.CreateCommand();
            command.CommandText = "EXEC sp_set_session_context @key = N'OrganizationId', @value = @organizationId;";
            var parameter = command.CreateParameter();
            parameter.ParameterName = "@organizationId";
            parameter.Value = organizationId;
            command.Parameters.Add(parameter);
            command.ExecuteNonQuery();
        }
    }
}
