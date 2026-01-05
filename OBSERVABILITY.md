# Observability and Tracing Configuration

This document explains the dual observability setup for Fencemark, which uses both **Aspire Dashboard** (for local/dev) and **Azure Application Insights** (for production monitoring).

## Overview

The application uses **OpenTelemetry** as the unified telemetry collection standard, exporting to two destinations:

1. **Aspire Dashboard** - Real-time local observability during development
2. **Azure Application Insights** - Production monitoring, analytics, and alerting

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                     Your Application                         │
│  ┌──────────────┐              ┌──────────────┐            │
│  │  Web Frontend │              │  API Service  │            │
│  │  (Blazor)     │──HTTP────────│  (ASP.NET)   │            │
│  └──────────────┘              └──────────────┘            │
│         │                              │                     │
│         │  OpenTelemetry              │                     │
│         ├──────────────┬───────────────┤                     │
│         │              │               │                     │
└─────────┼──────────────┼───────────────┼─────────────────────┘
          │              │               │
          ▼              ▼               ▼
  ┌──────────────┐  ┌─────────┐  ┌──────────────┐
  │   Aspire     │  │  Azure  │  │  SQL Server  │
  │  Dashboard   │  │ AppIns  │  │  (traced)    │
  │  (OTLP)      │  │         │  │              │
  └──────────────┘  └─────────┘  └──────────────┘
```

## Telemetry Data Collected

### Traces (Distributed Tracing)
- ✅ HTTP requests (incoming and outgoing)
- ✅ SQL queries and stored procedures
- ✅ Database connection details
- ✅ Request duration and dependencies
- ✅ End-to-end transaction traces across services

### Metrics
- ✅ ASP.NET Core request metrics
- ✅ HTTP client metrics
- ✅ Runtime metrics (GC, CPU, memory)
- ✅ Custom application metrics

### Logs
- ✅ Structured logging with scopes
- ✅ Formatted messages
- ✅ Exception details

## SQL Server Tracing

SQL Server queries are automatically instrumented using `OpenTelemetry.Instrumentation.SqlClient`. This captures:

- Query text (parameterized)
- Execution duration
- Connection information
- Stored procedure names
- Exceptions and errors

**Configuration:**
```csharp
.AddSqlClientInstrumentation(options =>
{
    options.SetDbStatementForText = true;          // Capture SQL text
    options.SetDbStatementForStoredProcedure = true; // Capture SP names
    options.RecordException = true;                // Capture errors
});
```

## Viewing Telemetry

### During Development (Aspire Dashboard)

1. **Access the Dashboard:**
   ```
   https://<aspiredash-container>.azurecontainerapps.io
   ```

2. **View Traces:**
   - Navigate to "Traces" tab
   - See end-to-end request flows
   - Drill down into SQL queries

3. **View Metrics:**
   - Check "Metrics" tab for real-time performance data

4. **View Logs:**
   - "Structured Logs" tab shows all application logs

### In Production (Application Insights)

1. **Access Application Insights:**
   - Open Azure Portal
   - Navigate to your Application Insights resource
   - Resource Group: `rg-fencemark-<environment>`

2. **View Application Map:**
   - Shows service dependencies
   - Visualizes request flows
   - Identifies performance bottlenecks

3. **View Traces (Transaction Search):**
   - Go to "Transaction search"
   - Filter by operation name, result code, or duration
   - Drill into individual requests to see SQL queries

4. **View SQL Dependencies:**
   - Navigate to "Performance" → "Dependencies"
   - Filter by dependency type: "SQL"
   - See slow queries and failure rates

5. **Create Alerts:**
   - Set up alerts for:
     - Slow SQL queries (> 1s)
     - High error rates
     - Memory/CPU usage

## Configuration

### Environment Variables (Already Set in Bicep)

Both containers receive:

```bicep
{
  name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
  value: applicationInsights.outputs.connectionString
}
{
  name: 'OTEL_EXPORTER_OTLP_ENDPOINT'
  value: 'http://aspiredash:18889'
}
{
  name: 'OTEL_SERVICE_NAME'
  value: 'apiservice' // or 'webfrontend'
}
```

### ServiceDefaults Configuration

The `fencemark.ServiceDefaults` project configures OpenTelemetry:

**Packages:**
- `Azure.Monitor.OpenTelemetry.AspNetCore` - Application Insights exporter
- `OpenTelemetry.Instrumentation.SqlClient` - SQL tracing
- `OpenTelemetry.Instrumentation.AspNetCore` - HTTP tracing
- `OpenTelemetry.Instrumentation.Http` - HttpClient tracing
- `OpenTelemetry.Instrumentation.Runtime` - Runtime metrics

## End-to-End Trace Example

When a user requests a quote:

```
1. [Web Frontend] HTTP GET /quotes/123
   ├─ Duration: 245ms
   │
   ├─ 2. [HTTP Client] GET http://apiservice/api/quotes/123
   │  ├─ Duration: 230ms
   │  │
   │  ├─ 3. [API Service] Process request
   │  │  ├─ Duration: 215ms
   │  │  │
   │  │  ├─ 4. [SQL] SELECT * FROM Quotes WHERE Id = @p0
   │  │  │  ├─ Duration: 12ms
   │  │  │  ├─ Database: fencemark
   │  │  │  └─ Server: sql-xxx.database.windows.net
   │  │  │
   │  │  └─ 5. [SQL] SELECT * FROM LineItems WHERE QuoteId = @p0
   │  │     ├─ Duration: 8ms
   │  │     └─ Returns: 5 rows
   │  │
   │  └─ Returns: 200 OK
   │
   └─ Renders: Quote details page
```

This entire trace is visible in:
- **Aspire Dashboard**: Real-time during development
- **Application Insights**: For production analysis

## Query Examples

### Application Insights (Kusto Query Language)

**Find slow SQL queries:**
```kusto
dependencies
| where type == "SQL"
| where duration > 1000  // > 1 second
| project timestamp, name, duration, resultCode
| order by duration desc
```

**Trace a specific request:**
```kusto
requests
| where name == "GET /quotes/[id]"
| project operation_Id
| join dependencies on operation_Id
| order by timestamp asc
```

**Monitor error rates:**
```kusto
requests
| summarize 
    Total = count(),
    Failed = countif(success == false)
    by bin(timestamp, 5m)
| extend ErrorRate = (Failed * 100.0) / Total
```

## Performance Considerations

### Impact on Application
- **Minimal overhead**: OpenTelemetry uses sampling and batching
- **Async export**: Telemetry doesn't block requests
- **Configurable sampling**: Can adjust sampling rates if needed

### Cost
- **Aspire Dashboard**: No cost (included in Container Apps)
- **Application Insights**: 
  - First 5 GB/month: Free
  - Additional data: ~$2.30/GB
  - Typical cost for small app: $5-20/month

## Troubleshooting

### No data in Application Insights

1. **Check connection string:**
   ```bash
   az containerapp show --name ca-apiservice-xxx --resource-group rg-fencemark-dev \
     --query "properties.configuration.secrets" -o table
   ```

2. **Verify logs:**
   ```bash
   az containerapp logs show --name ca-apiservice-xxx --resource-group rg-fencemark-dev \
     --follow
   ```

3. **Look for initialization errors:**
   Search logs for "Application Insights" or "OpenTelemetry"

### SQL traces not appearing

1. **Verify package installed:**
   ```bash
   dotnet list package | grep SqlClient
   ```

2. **Check connection string format:**
   Must use `Microsoft.Data.SqlClient` (not `System.Data.SqlClient`)

3. **Enable detailed logging:**
   Add to appsettings.json:
   ```json
   {
     "Logging": {
       "LogLevel": {
         "OpenTelemetry": "Debug"
       }
     }
   }
   ```

## Best Practices

1. **Use semantic conventions:** Follow OpenTelemetry semantic conventions for attribute names
2. **Add custom spans:** Create spans for important business operations
3. **Filter sensitive data:** Don't log passwords or PII in SQL traces
4. **Set sampling rates:** Use sampling in high-traffic scenarios
5. **Create dashboards:** Build custom Application Insights dashboards for your team
6. **Set up alerts:** Proactive monitoring for errors and performance degradation

## Next Steps

1. ✅ OpenTelemetry configured
2. ✅ Application Insights integrated
3. ✅ SQL tracing enabled
4. 📋 **TODO**: Create custom Application Insights dashboard
5. 📋 **TODO**: Set up alerts for critical scenarios
6. 📋 **TODO**: Configure log retention policies
7. 📋 **TODO**: Add custom application metrics

## Resources

- [OpenTelemetry Documentation](https://opentelemetry.io/docs/)
- [Application Insights Documentation](https://docs.microsoft.com/en-us/azure/azure-monitor/app/app-insights-overview)
- [.NET Aspire Telemetry](https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/telemetry)
- [SQL Instrumentation](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/tree/main/src/OpenTelemetry.Instrumentation.SqlClient)
