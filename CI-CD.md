# CI/CD Pipeline Documentation

## Overview

Fencemark uses GitHub Actions for continuous integration and continuous deployment (CI/CD) with Azure Bicep for infrastructure as code (IaC). This document explains the complete CI/CD pipeline, how it works, and how to use it effectively.

## Table of Contents

- [Pipeline Architecture](#pipeline-architecture)
- [Workflows](#workflows)
- [Triggers and When Workflows Run](#triggers-and-when-workflows-run)
- [Build and Test Process](#build-and-test-process)
- [Infrastructure as Code with Bicep](#infrastructure-as-code-with-bicep)
- [Deployment Process](#deployment-process)
- [Environment Management](#environment-management)
- [Using the Pipeline](#using-the-pipeline)
- [Secrets and Configuration](#secrets-and-configuration)
- [Monitoring and Troubleshooting](#monitoring-and-troubleshooting)
- [Best Practices](#best-practices)

## Pipeline Architecture

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           GitHub Actions CI/CD                              │
│                                                                             │
│  On Push/PR to main                         On Manual Trigger               │
│         │                                           │                       │
│         ▼                                           ▼                       │
│  ┌──────────────┐                          ┌──────────────┐                │
│  │ Build & Test │                          │    Deploy    │                │
│  │  Workflow    │                          │   Workflow   │                │
│  └──────┬───────┘                          └──────┬───────┘                │
│         │                                          │                        │
│         │ 1. Checkout code                         │ 1. Build & Test       │
│         │ 2. Setup .NET 10                         │ 2. Publish artifacts  │
│         │ 3. Restore dependencies                  │ 3. Deploy Bicep       │
│         │ 4. Build solution                        │ 4. Build Docker images│
│         │ 5. Run tests                             │ 5. Push to ACR        │
│         │ 6. Upload test results                   │ 6. Update Container   │
│         │ 7. Annotate failures                     │    Apps               │
│         │                                          │                        │
│         ▼                                          ▼                        │
│    Test Results                              Azure Container Apps          │
│    ✓ Unit Tests                              ┌──────────────┐              │
│    ✓ Integration Tests                       │     Dev      │              │
│    ✓ Test Reports                            │   Staging    │              │
│                                               │     Prod     │              │
│                                               └──────────────┘              │
│                                                                             │
│  On Pull Request                                                            │
│         │                                                                   │
│         ▼                                                                   │
│  ┌──────────────────┐                                                      │
│  │ Dependency Review │                                                      │
│  │    Workflow       │                                                      │
│  └───────────────────┘                                                      │
│         │                                                                   │
│         │ 1. Checkout code                                                  │
│         │ 2. Scan dependencies                                              │
│         │ 3. Check vulnerabilities                                          │
│         │ 4. Report issues                                                  │
│         │                                                                   │
│         ▼                                                                   │
│    Security Report                                                          │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Key Components

1. **Build & Test Workflow** - Validates code quality on every push and PR
2. **Deploy Workflow** - Deploys to Azure Container Apps using Bicep
3. **Dependency Review** - Scans for vulnerable dependencies on PRs
4. **Bicep Templates** - Infrastructure as code for reproducible environments

## Workflows

### 1. Build and Test (`.github/workflows/build.yml`)

**Purpose**: Ensures code quality by building and testing on every push and pull request.

**Key Steps**:
- Checkout code
- Setup .NET 10 SDK
- Restore NuGet dependencies
- Install Aspire workload
- Build solution in Release configuration
- Run all tests with xUnit reporting
- Upload test results as artifacts
- Annotate test failures in PR

**Outputs**:
- Build artifacts (temporary)
- Test results (.trx files)
- Test annotations in PR

### 2. Deploy to Azure (`.github/workflows/deploy.yml`)

**Purpose**: Deploys the application to Azure Container Apps with full infrastructure provisioning.

**Key Steps**:

#### Build Job
1. Build and test the solution
2. Publish API Service and Web Frontend
3. Upload artifacts for deployment

#### Deploy Jobs (Dev/Staging/Prod)
1. Authenticate with Azure using OIDC
2. Deploy Bicep infrastructure
3. Retrieve deployment outputs
4. Build and push Docker images to ACR
5. Update Container Apps with new images
6. Generate deployment summary

**Outputs**:
- Container images in Azure Container Registry
- Deployed Container Apps
- Deployment summary with URLs

### 3. Dependency Review (`.github/workflows/dependency-review.yml`)

**Purpose**: Scans pull requests for vulnerable or incompatible dependencies.

**Key Steps**:
- Checkout code
- Run GitHub dependency review action
- Report vulnerabilities and license issues

## Triggers and When Workflows Run

### Build and Test Workflow

```yaml
on:
  push:
    branches: [main]
  pull_request:
    branches: [main]
```

**Triggers**:
- ✅ Every push to `main` branch
- ✅ Every pull request targeting `main`
- ❌ Does NOT run on documentation-only changes
- ❌ Does NOT run on pushes to feature branches (only on PRs from them)

**Purpose**: Ensures all code changes are validated before merging.

### Deploy Workflow

```yaml
on:
  push:
    branches: [main]
    paths-ignore:
      - '**.md'
      - '.github/ISSUE_TEMPLATE/**'
      - '.github/PULL_REQUEST_TEMPLATE.md'
  workflow_dispatch:
    inputs:
      environment:
        type: choice
        options: [dev, staging, prod]
```

**Triggers**:
- ✅ Automatic deployment to **dev** on push to `main` (excluding docs)
- ✅ Manual deployment to **staging** via workflow_dispatch
- ✅ Manual deployment to **prod** via workflow_dispatch
- ❌ Does NOT auto-deploy staging or prod
- ❌ Ignores documentation-only changes

**Concurrency Control**:
```yaml
concurrency:
  group: deploy-${{ github.ref }}-${{ github.event.inputs.environment || 'dev' }}
  cancel-in-progress: false
```
- Prevents simultaneous deployments to the same environment
- Does NOT cancel in-progress deployments (safe for production)

### Dependency Review Workflow

```yaml
on:
  pull_request:
    branches: [main, master]
```

**Triggers**:
- ✅ Every pull request to `main` or `master`
- ❌ Does NOT run on pushes to `main`

## Build and Test Process

### Build Process

The build process uses .NET 10 and follows these steps:

```bash
# 1. Restore dependencies
dotnet restore

# 2. Install Aspire workload (for distributed app orchestration)
dotnet workload install aspire

# 3. Build in Release configuration
dotnet build --no-restore --configuration Release

# 4. Run tests
dotnet test --no-build --configuration Release
```

### Test Execution

Tests are executed using xUnit with the following configuration:

```bash
dotnet test \
  --no-build \
  --configuration Release \
  --report-xunit-trx \
  --results-directory TestResults
```

**Test Suite Coverage**:
- ✅ Unit tests for authentication services
- ✅ Unit tests for organization management
- ✅ Integration tests using Aspire's distributed testing
- ✅ Complete data segmentation validation
- ✅ Role-based access control tests

### Test Reporting

Test results are:
1. **Uploaded as artifacts** - Available for 90 days for analysis
2. **Annotated in PRs** - Failed tests appear as PR comments
3. **Reported in job summary** - Quick overview in GitHub Actions UI

```yaml
- name: Upload test results
  uses: actions/upload-artifact@v4
  with:
    name: test-results
    path: TestResults/*.trx

- name: Annotate test failures
  uses: dorny/test-reporter@v1
  with:
    name: dotnet-tests
    path: TestResults/*.trx
    reporter: dotnet-trx
```

## Infrastructure as Code with Bicep

### Bicep Template Structure

```
infra/
├── main.bicep                      # Main infrastructure template
├── dev.bicepparam                  # Dev environment parameters
├── staging.bicepparam              # Staging environment parameters
├── prod.bicepparam                 # Production environment parameters
├── entra-external-id.bicep        # Authentication infrastructure
├── keyvault-access.bicep          # Key Vault access policies
├── maps-account.bicep             # Azure Maps integration
├── modules/                        # Reusable Bicep modules
│   ├── container-app.bicep
│   ├── container-registry.bicep
│   └── log-analytics.bicep
├── abbreviations.json             # Resource naming conventions
└── README.md                      # Infrastructure documentation
```

### Azure Resources Deployed

The Bicep templates deploy a complete infrastructure stack:

| Resource | Purpose | SKU/Tier |
|----------|---------|----------|
| **Resource Group** | Logical container for all resources | N/A |
| **Log Analytics Workspace** | Centralized logging and monitoring | Pay-as-you-go |
| **Container Registry** | Store Docker images | Basic |
| **Container Apps Environment** | Managed runtime for containers | Consumption |
| **API Service Container App** | Backend API (internal ingress) | 0.25 CPU, 0.5Gi RAM |
| **Web Frontend Container App** | Blazor UI (external ingress) | 0.25 CPU, 0.5Gi RAM |
| **Azure Maps Account** | Location services | Gen2 / G2 |
| **Key Vault** (optional) | Secure secrets storage | Standard |
| **Managed Identity** (optional) | Azure AD authentication | N/A |

### Environment-Specific Configuration

Each environment has its own `.bicepparam` file with specific settings:

**Dev Environment** (`dev.bicepparam`):
- Minimal resources (0.25 CPU, 0.5Gi RAM)
- Scale to zero when idle
- 30-day log retention
- No custom domains

**Staging Environment** (`staging.bicepparam`):
- Moderate resources (0.5 CPU, 1Gi RAM)
- Minimum 1 replica
- 60-day log retention
- Optional custom domain

**Production Environment** (`prod.bicepparam`):
- Full resources (1 CPU, 2Gi RAM)
- Minimum 2 replicas (HA)
- 90-day log retention
- Custom domain required
- Additional monitoring

### Reproducible Deployments

Infrastructure is completely reproducible:

```bash
# Deploy to any environment
az deployment sub create \
  --location australiaeast \
  --template-file ./infra/main.bicep \
  --parameters ./infra/[env].bicepparam \
  --name main
```

All environment configuration is in source control, ensuring:
- ✅ Version controlled infrastructure
- ✅ Consistent deployments across environments
- ✅ Easy rollback to previous configurations
- ✅ Infrastructure changes reviewed via PRs
- ✅ Complete audit trail

## Deployment Process

### Deployment Flow

```
┌────────────────────────────────────────────────────────────────┐
│                    Deployment Pipeline                         │
│                                                                │
│  ┌──────────────┐                                             │
│  │ Build Stage  │                                             │
│  └──────┬───────┘                                             │
│         │                                                      │
│         ├─► Build Solution (Release)                          │
│         ├─► Run Tests                                         │
│         ├─► Publish API Service → artifact                    │
│         └─► Publish Web Frontend → artifact                   │
│                                                                │
│  ┌──────────────────┐                                         │
│  │ Infrastructure   │                                         │
│  │ Deployment       │                                         │
│  └──────┬───────────┘                                         │
│         │                                                      │
│         ├─► Authenticate with Azure (OIDC)                    │
│         ├─► Deploy Bicep template                             │
│         ├─► Create/Update Resource Group                      │
│         ├─► Create/Update Container Apps Environment          │
│         ├─► Create/Update Container Registry                  │
│         ├─► Create/Update Log Analytics                       │
│         ├─► Create/Update Container Apps                      │
│         └─► Retrieve deployment outputs                       │
│                                                                │
│  ┌──────────────────┐                                         │
│  │ Container Build  │                                         │
│  │ & Push           │                                         │
│  └──────┬───────────┘                                         │
│         │                                                      │
│         ├─► Download artifacts                                │
│         ├─► Login to ACR                                      │
│         ├─► Build API Service Docker image                    │
│         ├─► Build Web Frontend Docker image                   │
│         ├─► Push images to ACR                                │
│         └─► Tag with commit SHA                               │
│                                                                │
│  ┌──────────────────┐                                         │
│  │ Application      │                                         │
│  │ Deployment       │                                         │
│  └──────┬───────────┘                                         │
│         │                                                      │
│         ├─► Update API Service Container App                  │
│         ├─► Update Web Frontend Container App                 │
│         ├─► Health checks validate deployment                 │
│         └─► Generate deployment summary                       │
│                                                                │
└────────────────────────────────────────────────────────────────┘
```

### Authentication with Azure

The pipeline uses **OpenID Connect (OIDC)** for secure, credential-less authentication:

```yaml
- name: Log in to Azure
  uses: azure/login@v2
  with:
    client-id: ${{ vars.AZURE_CLIENT_ID }}
    tenant-id: ${{ vars.AZURE_TENANT_ID }}
    subscription-id: ${{ vars.AZURE_SUBSCRIPTION_ID }}
```

**Benefits of OIDC**:
- ✅ No long-lived credentials stored in GitHub
- ✅ Short-lived tokens with automatic rotation
- ✅ Azure AD managed identities
- ✅ Fine-grained access control

### Container Image Management

Images are tagged with the Git commit SHA for traceability:

```bash
IMAGE_TAG="<acr-login-server>/apiservice:${{ github.sha }}"
docker build -t $IMAGE_TAG -f fencemark.ApiService/Dockerfile ./publish/apiservice
docker push $IMAGE_TAG
```

**Image Naming Convention**:
- Format: `<registry>//<service>:<commit-sha>`
- Example: `crfencemark123.azurecr.io/apiservice:a1b2c3d`

**Benefits**:
- ✅ Exact version tracking
- ✅ Easy rollback to any commit
- ✅ Image immutability
- ✅ Security scanning by commit

### Deployment Outputs

Each deployment generates a comprehensive summary:

```markdown
## 🚀 Dev Deployment Complete

**Environment:** dev
**Region:** australiaeast
**Resource Group:** rg-fencemark-dev

### 🐳 Container Images
- **API Service:** `crfencemark123.azurecr.io/apiservice:a1b2c3d`
- **Web Frontend:** `crfencemark123.azurecr.io/webfrontend:a1b2c3d`

### 🔗 Application URL
https://ca-webfrontend-abc123.australiaeast.azurecontainerapps.io
```

## Environment Management

### Environments in GitHub

GitHub environments are configured with protection rules:

| Environment | Auto-Deploy | Manual Approval | Branch Restriction |
|-------------|-------------|-----------------|-------------------|
| **dev** | ✅ On push to main | ❌ No | N/A |
| **staging** | ❌ Manual only | ✅ Required | main |
| **prod** | ❌ Manual only | ✅ Required | main |

### Environment Variables and Secrets

Each environment has specific configuration:

#### Repository Variables (Shared)
- `AZURE_CLIENT_ID` - OIDC service principal client ID
- `AZURE_TENANT_ID` - Azure AD tenant ID
- `AZURE_SUBSCRIPTION_ID` - Target Azure subscription

#### Environment-Specific Variables
- `ENTRA_EXTERNAL_ID_TENANT_ID` - Authentication tenant
- `ENTRA_EXTERNAL_ID_CLIENT_ID` - Auth client ID
- `KEY_VAULT_URL` - Key Vault for secrets
- `CERTIFICATE_NAME` - Auth certificate name

### Environment Lifecycle

**Development (dev)**:
- Purpose: Continuous integration and testing
- Deployed automatically on every merge to main
- Latest code always deployed
- May be unstable

**Staging**:
- Purpose: Pre-production validation
- Manual deployment only
- Used for QA, UAT, and final testing
- Should mirror production configuration

**Production (prod)**:
- Purpose: Live user-facing application
- Manual deployment with approval
- Only stable, tested releases
- High availability configuration

## Using the Pipeline

### Running Build and Test

The build workflow runs automatically, but you can trigger it manually:

1. Navigate to **Actions** tab in GitHub
2. Select **Build and Test** workflow
3. Click **Run workflow**
4. Select branch
5. Click **Run workflow** button

### Deploying to Development

Development deploys automatically on merge to main:

```bash
# Create feature branch
git checkout -b feature/my-feature

# Make changes
git add .
git commit -m "Add new feature"
git push origin feature/my-feature

# Create PR to main
# After PR approval and merge → automatic deployment to dev
```

### Deploying to Staging

Staging requires manual deployment:

1. Navigate to **Actions** tab
2. Select **Deploy to Azure** workflow
3. Click **Run workflow**
4. Select `main` branch
5. Choose `staging` environment
6. Click **Run workflow**
7. Approve deployment when prompted

### Deploying to Production

Production requires manual deployment with approval:

1. Ensure staging is tested and stable
2. Navigate to **Actions** tab
3. Select **Deploy to Azure** workflow
4. Click **Run workflow**
5. Select `main` branch
6. Choose `prod` environment
7. Click **Run workflow**
8. Designated approvers will receive notification
9. Deployment proceeds after approval

### Manual Rollback

To roll back to a previous version:

```bash
# Option 1: Redeploy previous commit
git checkout <previous-commit-sha>
# Trigger manual deployment

# Option 2: Update Container App with previous image
az containerapp update \
  --name ca-webfrontend-<token> \
  --resource-group rg-fencemark-prod \
  --image <registry>/webfrontend:<previous-commit-sha>
```

## Secrets and Configuration

### Required Secrets Setup

#### 1. Azure Service Principal (OIDC)

Create a service principal with federated credentials:

```bash
# Create service principal
az ad sp create-for-rbac \
  --name "fencemark-github-actions" \
  --role contributor \
  --scopes /subscriptions/<subscription-id> \
  --sdk-auth

# Configure federated credentials for GitHub Actions
az ad app federated-credential create \
  --id <app-id> \
  --parameters '{
    "name": "github-actions",
    "issuer": "https://token.actions.githubusercontent.com",
    "subject": "repo:ModernPlatforms/fencemark:environment:prod",
    "audiences": ["api://AzureADTokenExchange"]
  }'
```

#### 2. Add Variables to GitHub

1. Go to **Settings** → **Secrets and variables** → **Actions**
2. Add variables:
   - `AZURE_CLIENT_ID`
   - `AZURE_TENANT_ID`
   - `AZURE_SUBSCRIPTION_ID`

#### 3. Configure Environment Secrets

For each environment (dev, staging, prod):

1. Go to **Settings** → **Environments**
2. Select environment
3. Add environment-specific variables
4. Configure protection rules

### Security Best Practices

- ✅ Use OIDC instead of service principal secrets
- ✅ Rotate credentials regularly
- ✅ Use environment secrets for sensitive data
- ✅ Enable required approvals for production
- ✅ Audit access logs regularly
- ✅ Use least privilege principle
- ✅ Store certificates in Key Vault
- ✅ Use managed identities in Azure

## Monitoring and Troubleshooting

### Viewing Workflow Runs

1. Navigate to **Actions** tab
2. Select workflow to view
3. Click on specific run
4. Expand jobs to see steps
5. Click on step to view logs

### Common Issues and Solutions

#### Build Failures

**Issue**: `dotnet restore` fails
```
Solution: Check NuGet package sources and authentication
```

**Issue**: Tests fail in CI but pass locally
```
Solution: Check for environment-specific dependencies or timing issues
```

#### Deployment Failures

**Issue**: Authentication fails to Azure
```
Solution: Verify OIDC configuration and federated credentials
```

**Issue**: Bicep deployment fails
```
Solution: Check parameter values and Azure resource quotas
```

**Issue**: Container App update fails
```
Solution: Check image exists in ACR and Container App has ACR pull permissions
```

#### Container App Issues

**Issue**: Application not responding after deployment
```
Solution: Check Container App logs:
az containerapp logs show \
  --name ca-webfrontend-<token> \
  --resource-group rg-fencemark-<env> \
  --follow
```

### Monitoring Tools

- **GitHub Actions**: Workflow execution logs
- **Azure Portal**: Resource health and metrics
- **Log Analytics**: Centralized logging
- **Application Insights**: APM and diagnostics (if configured)
- **Container App Logs**: Real-time application logs

### Getting Help

1. Check workflow logs in GitHub Actions
2. Check Container App logs in Azure Portal
3. Review Bicep deployment outputs
4. Check [DEPLOYMENT.md](./DEPLOYMENT.md) for specific issues
5. Check [infra/README.md](./infra/README.md) for infrastructure details

## Best Practices

### For Developers

1. **Test Locally First**: Run tests locally before pushing
   ```bash
   dotnet test
   ```

2. **Use Feature Branches**: Never commit directly to main
   ```bash
   git checkout -b feature/my-feature
   ```

3. **Write Good Commit Messages**: Clear, descriptive messages
   ```bash
   git commit -m "Add user authentication to API endpoint"
   ```

4. **Keep PRs Small**: Easier to review and test

5. **Update Tests**: Add tests for new features

### For DevOps Engineers

1. **Review Pipeline Regularly**: Check for outdated actions or dependencies

2. **Monitor Costs**: Review Azure spending and optimize resources

3. **Update Secrets**: Rotate credentials on schedule

4. **Test Disaster Recovery**: Periodically test rollback procedures

5. **Document Changes**: Update this documentation when pipeline changes

### For Team Leads

1. **Enforce Branch Protection**: Require PR reviews and status checks

2. **Configure Approvers**: Set up approval groups for production

3. **Monitor Deployments**: Review deployment frequency and success rate

4. **Audit Access**: Regularly review who has deployment permissions

5. **Set SLAs**: Define deployment windows and rollback procedures

## Additional Resources

- [GitHub Actions Documentation](https://docs.github.com/en/actions)
- [Azure Bicep Documentation](https://learn.microsoft.com/azure/azure-resource-manager/bicep/)
- [Azure Container Apps Documentation](https://learn.microsoft.com/azure/container-apps/)
- [.NET Aspire Documentation](https://learn.microsoft.com/dotnet/aspire)

## Related Documentation

- [README.md](./README.md) - Project overview and getting started
- [DEPLOYMENT.md](./DEPLOYMENT.md) - Manual deployment instructions
- [infra/README.md](./infra/README.md) - Infrastructure details
- [AUTHENTICATION_SETUP.md](./AUTHENTICATION_SETUP.md) - Authentication configuration

---

**Last Updated**: 2025-12-12
**Maintainer**: Fencemark Team
