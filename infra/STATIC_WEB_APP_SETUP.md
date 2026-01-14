# Azure Storage Static Website Setup for Blazor WebAssembly

This document describes the Azure Storage static website infrastructure for hosting the Fencemark Blazor WebAssembly client application, with an optional Azure CDN layer.

## Overview

The static website infrastructure provisions:

- **Storage Account (Static Website)**: Hosts the Blazor WebAssembly build output in the `$web` container.
- **Optional Azure CDN**: Fronts the storage endpoint to improve global performance and enable custom domain scenarios.
- **Deployment Outputs**: Storage and CDN endpoints exposed as template outputs.

## Infrastructure Components

### 1. Storage Account Static Website
- **Naming Convention**: `st{resourceToken}` (Azure storage naming conventions)
- **SKU**: Configurable via `staticSiteStorageSku`
- **Location**: Australia East (follows main infrastructure)
- **Static Website**: Enabled with `index.html` as the index and 404 document

### 2. Azure CDN (Optional)
- **Profile**: `cdnp-{storageAccountName}`
- **Endpoint**: `static-site`
- **Origin**: Storage static website endpoint
- **Custom Domain + HTTPS**: Supports CDN-managed certificates when `bindCustomDomainCertificate = true`

## Deployment

### Prerequisites
1. Azure subscription with appropriate permissions
2. Existing API Container App infrastructure deployed
3. Central App Configuration store deployed
4. Azure Entra External ID configured

### Deployment Steps

1. **Deploy the infrastructure** (one-time):
   ```bash
   cd /home/runner/work/fencemark/fencemark/infra
   
   # For dev environment
   az deployment sub create \
     --location australiaeast \
     --template-file main.bicep \
     --parameters dev.bicepparam
   
   # For staging environment
   az deployment sub create \
     --location australiaeast \
     --template-file main.bicep \
     --parameters staging.bicepparam
   
   # For prod environment
   az deployment sub create \
     --location australiaeast \
     --template-file main.bicep \
     --parameters prod.bicepparam
   ```

2. **Publish the Blazor WebAssembly app**:
   ```bash
   dotnet publish -c Release -o ./publish
   ```

3. **Upload to the static website**:
   ```bash
   az storage blob upload-batch \
     --account-name <storageAccountName> \
     --destination '$web' \
     --source ./publish/wwwroot \
     --overwrite
   ```

4. **(Optional) Enable CDN**:
   - Set `enableStaticSiteCdn = true` in the environment `.bicepparam` file.
   - Redeploy the infrastructure to create the CDN profile and endpoint.
   - To enable HTTPS on the CDN custom domain, set `bindCustomDomainCertificate = true` once DNS is validated.

### Deployment Outputs

The deployment provides the following outputs:
- `staticSiteStorageAccountName`: Name of the storage account hosting the static site
- `staticSiteUrl`: Static website URL
- `staticSiteCdnHostname`: CDN endpoint hostname (when CDN is enabled)

## Configuration

### Static Website Routing

Blazor WebAssembly requires SPA fallback routing. Ensure your `staticwebapp.config.json` (or equivalent server configuration) is set to rewrite unknown routes to `/index.html`.

### Custom Domains

Custom domains are typically attached via Azure CDN (recommended) or Azure Front Door:
1. Create a CDN custom domain in Azure Portal.
2. Point DNS to the CDN endpoint hostname.
3. Validate and enable HTTPS.

This template can provision the CDN custom domain and enable CDN-managed HTTPS when:
- `enableStaticSiteCdn = true`
- `customDomain` is set (e.g., `dev.fencemark.com.au`)
- `bindCustomDomainCertificate = true`

## Monitoring & Diagnostics

- **Storage Metrics**: Available in Azure Portal under the storage account.
- **CDN Metrics**: Available under the CDN profile and endpoint.

## Cost Management

- **Storage Account**: Pay-as-you-go for capacity and bandwidth.
- **CDN**: Optional; additional bandwidth and request charges.

## Troubleshooting

### Common Issues

1. **Static site returns 404 for SPA routes**:
   - Ensure `index.html` is configured as the 404 document.
   - Verify SPA fallback configuration.

2. **CDN shows stale content**:
   - Purge the CDN endpoint after uploads.

3. **Upload fails**:
   - Confirm storage account permissions and network access.

## References

- [Azure Storage Static Website Hosting](https://learn.microsoft.com/azure/storage/blobs/storage-blob-static-website)
- [Azure CDN Documentation](https://learn.microsoft.com/azure/cdn/)
- [Blazor WebAssembly Deployment](https://learn.microsoft.com/aspnet/core/blazor/host-and-deploy/webassembly)
