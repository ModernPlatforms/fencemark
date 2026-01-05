# DNS Zone Setup for fencemark.com.au

This document describes how to set up the DNS zone for fencemark.com.au and configure automatic subdomain routing for each environment.

## Overview

- **Base Domain**: fencemark.com.au
- **Dev Environment**: dev.fencemark.com.au
- **Staging Environment**: staging.fencemark.com.au
- **Production Environment**: fencemark.com.au (apex domain)

The DNS zone is deployed together with the central App Configuration in the central resource group (`rg-fencemark-central-config`) and shared across all environments. Each environment deployment automatically creates its DNS records and configures domain validation.

## Central Infrastructure Deployment

Deploy the central App Configuration and DNS zone together (one-time setup):

**Using PowerShell Script (Recommended):**
```powershell
.\infra\scripts\deploy-central-infrastructure.ps1
```

**Using Azure CLI:**
```bash
az deployment sub create \
  --location australiaeast \
  --template-file infra/central-appconfig.bicep \
  --parameters infra/central-appconfig.bicepparam
```

The deployment will output the Azure name servers that you need to configure at your domain registrar.

After deployment, you'll receive the Azure name servers. Configure these at your domain registrar:

```bash
# Get the name servers
az network dns zone show \
  --resource-group rg-fencemark-central-config \
  --name fencemark.com.au \
  --query "nameServers" \
  --output table
```

## Domain Registrar Configuration

At your domain registrar (e.g., GoDaddy, Namecheap, etc.), update the name servers to the Azure name servers provided above. This typically takes 24-48 hours to propagate.

Example name servers from Azure:
- ns1-01.azure-dns.com
- ns2-01.azure-dns.net
- ns3-01.azure-dns.org
- ns4-01.azure-dns.info

## Environment Deployments

Once the DNS zone is configured, each environment deployment will automatically:

1. **Create DNS Validation Record**: A TXT record (`asuid.<subdomain>`) for Container Apps domain validation
2. **Create CNAME Record**: Points the subdomain to the Container Apps environment
3. **Generate Managed Certificate**: SSL/TLS certificate for the custom domain
4. **Bind Domain**: Attach the custom domain to the web frontend

### Dev Environment

```bash
azd deploy --environment dev
```

This creates:
- TXT record: `asuid.dev.fencemark.com.au` → validation ID
- CNAME record: `dev.fencemark.com.au` → Container Apps FQDN
- Web frontend accessible at: https://dev.fencemark.com.au

### Staging Environment

```bash
azd deploy --environment staging
```

This creates:
- TXT record: `asuid.staging.fencemark.com.au` → validation ID
- CNAME record: `staging.fencemark.com.au` → Container Apps FQDN
- Web frontend accessible at: https://staging.fencemark.com.au

### Production Environment

```bash
azd deploy --environment prod
```

This creates:
- TXT record: `asuid.fencemark.com.au` → validation ID
- CNAME record: `@.fencemark.com.au` → Container Apps FQDN
- Web frontend accessible at: https://fencemark.com.au

## Customizing the Domain

If you want to override the automatic subdomain computation, you can set the `customDomain` parameter in your environment-specific `.bicepparam` file:

```bicep
param customDomain = 'app.fencemark.com.au'
```

## Verification

After deployment, verify the DNS records:

```bash
# Check DNS records
az network dns record-set list \
  --resource-group rg-fencemark-central-config \
  --zone-name fencemark.com.au \
  --output table

# Test DNS resolution
nslookup dev.fencemark.com.au
nslookup staging.fencemark.com.au
nslookup fencemark.com.au
```

## Troubleshooting

### Certificate not provisioning

If the managed certificate fails to provision:

1. Verify the TXT validation record exists:
   ```bash
   az network dns record-set txt show \
     --resource-group rg-fencemark-central-config \
     --zone-name fencemark.com.au \
     --name asuid.dev
   ```

2. Check Container Apps environment validation:
   ```bash
   az containerapp env show \
     --name <environment-name> \
     --resource-group <resource-group> \
     --query customDomainConfiguration
   ```

### DNS not resolving

- Ensure name servers are properly configured at your registrar
- Wait for DNS propagation (can take up to 48 hours)
- Use `dig` or `nslookup` to test DNS resolution

### Custom domain not binding

- Verify the CNAME record points to the correct Container Apps FQDN
- Check that the managed certificate is in "Succeeded" state
- Review Container Apps logs for binding errors

## Architecture

```
┌─────────────────────────────────────────────┐
│ fencemark.com.au (DNS Zone)                 │
│ Resource Group: rg-fencemark-central-config │
└─────────────────────────────────────────────┘
                    │
        ┌───────────┼───────────┐
        │           │           │
        ▼           ▼           ▼
┌─────────┐   ┌─────────┐   ┌─────────┐
│   dev   │   │ staging │   │  prod   │
│  (CNAME)│   │ (CNAME) │   │ (CNAME) │
└─────────┘   └─────────┘   └─────────┘
     │             │             │
     ▼             ▼             ▼
Container Apps  Container Apps  Container Apps
Environment     Environment     Environment
(dev)          (staging)       (prod)
```

## Security Considerations

- **Managed Certificates**: Azure automatically renews certificates before expiration
- **TLS 1.2+**: Enforced by Container Apps
- **Domain Validation**: Automatic validation via TXT records
- **RBAC**: DNS zone modifications require appropriate Azure permissions

## Cost

- **DNS Zone**: ~$0.50/month for the hosted zone
- **DNS Queries**: $0.40 per million queries (first 1 billion queries)
- **Managed Certificates**: Free with Container Apps

## Next Steps

1. Deploy the DNS zone using the commands above
2. Configure name servers at your domain registrar
3. Wait for DNS propagation
4. Deploy each environment (dev, staging, prod)
5. Verify custom domains are accessible
