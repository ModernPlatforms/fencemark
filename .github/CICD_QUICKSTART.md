# CI/CD Quick Start Guide

> 🚀 **New to the team?** This guide will get you up to speed with the CI/CD pipeline in 10 minutes.

## What You Need to Know

Fencemark uses **GitHub Actions** for automated building, testing, and deployment with **Azure Bicep** for infrastructure management.

### Key Workflows

| Workflow | Trigger | Purpose |
|----------|---------|---------|
| **Build and Test** | Every push/PR to `main` | Validates code quality |
| **Deploy to Azure** | Push to `main` or manual | Deploys to Azure Container Apps |
| **Dependency Review** | Every PR | Scans for vulnerabilities |

## For Developers

### Making Code Changes

1. **Create a feature branch**
   ```bash
   git checkout -b feature/my-awesome-feature
   ```

2. **Make your changes and commit**
   ```bash
   git add .
   git commit -m "Add awesome feature"
   git push origin feature/my-awesome-feature
   ```

3. **Create a Pull Request**
   - Go to GitHub and create a PR to `main`
   - The **Build and Test** workflow will run automatically
   - The **Dependency Review** workflow will scan for vulnerabilities

4. **Check the Results**
   - ✅ Green checkmark = All tests passed!
   - ❌ Red X = Tests failed, click to see details
   - Test results will be commented on your PR

5. **After Merge**
   - Code automatically deploys to **dev** environment
   - Check deployment status in the Actions tab
   - Find your app URL in the deployment summary

### Running Tests Locally

Before pushing, run tests locally:

```bash
# Run all tests
dotnet test

# Run only unit tests
dotnet test --filter "FullyQualifiedName~AuthServiceTests|FullyQualifiedName~OrganizationServiceTests"

# Run specific test
dotnet test --filter "FullyQualifiedName~YourTestName"
```

### Understanding Test Failures

If tests fail in CI:

1. **Check the Actions tab** - Click on the failed workflow
2. **Expand the "Run tests" step** - See which tests failed
3. **Look at test annotations** - Failed tests appear as PR comments
4. **Download test results** - Available as artifacts for detailed analysis

## For DevOps Engineers

### Deploying to Environments

#### Dev Environment (Automatic)
- Deploys automatically on every push to `main`
- No manual action needed
- Good for continuous testing

#### Staging Environment (Manual)
1. Go to **Actions** tab
2. Select **Deploy to Azure** workflow
3. Click **Run workflow**
4. Choose `staging` environment
5. Click **Run workflow** button
6. Approve when prompted

#### Production Environment (Manual)
1. Ensure staging is tested
2. Go to **Actions** tab
3. Select **Deploy to Azure** workflow
4. Click **Run workflow**
5. Choose `prod` environment
6. Click **Run workflow** button
7. Wait for approval from designated approvers
8. Monitor deployment progress

### Monitoring Deployments

**In GitHub**:
- Go to **Actions** tab
- Click on the running workflow
- Watch real-time logs
- Check deployment summary for URLs

**In Azure**:
- Go to Azure Portal
- Navigate to Container Apps
- Check application logs
- View metrics and health

### Troubleshooting Deployments

**Build Failed?**
```bash
# Check the build logs in Actions tab
# Common issues:
# - NuGet restore failed → Check package sources
# - Build errors → Fix compilation issues
# - Test failures → Fix failing tests
```

**Deployment Failed?**
```bash
# Check deployment logs in Actions tab
# Common issues:
# - Authentication failed → Verify OIDC setup
# - Bicep deployment failed → Check parameters
# - Image push failed → Verify ACR access
```

**App Not Working After Deploy?**
```bash
# Check Container App logs
az containerapp logs show \
  --name ca-webfrontend-<token> \
  --resource-group rg-fencemark-<env> \
  --follow
```

### Rolling Back

**Option 1: Redeploy Previous Version**
1. Go to Actions tab
2. Find successful previous deployment
3. Click "Re-run all jobs"

**Option 2: Manual Rollback**
```bash
# Find previous image
az acr repository show-tags \
  --name <acr-name> \
  --repository webfrontend \
  --orderby time_desc

# Update Container App
az containerapp update \
  --name ca-webfrontend-<token> \
  --resource-group rg-fencemark-<env> \
  --image <acr-login-server>/webfrontend:<previous-sha>
```

## For Team Leads

### Setting Up Environments

**GitHub Environments** are configured with:
- **dev**: No approval required, auto-deploy
- **staging**: Requires 1 approval
- **prod**: Requires 2 approvals from designated team

**To modify approvers**:
1. Go to **Settings** → **Environments**
2. Select environment
3. Configure protection rules
4. Add required reviewers

### Required Secrets

These must be configured in GitHub:

**Repository Variables** (Settings → Secrets and variables → Actions):
- `AZURE_CLIENT_ID` - Service principal client ID
- `AZURE_TENANT_ID` - Azure AD tenant ID
- `AZURE_SUBSCRIPTION_ID` - Target subscription

**Environment Variables** (per environment):
- `ENTRA_EXTERNAL_ID_TENANT_ID` - Auth tenant
- `ENTRA_EXTERNAL_ID_CLIENT_ID` - Auth client ID
- `KEY_VAULT_URL` - Key Vault URL
- `CERTIFICATE_NAME` - Auth certificate name

### Best Practices

✅ **DO**:
- Review PRs thoroughly before approving
- Ensure all tests pass before merging
- Test staging before promoting to prod
- Document infrastructure changes
- Monitor deployment metrics

❌ **DON'T**:
- Commit directly to `main`
- Merge PRs with failing tests
- Skip staging validation
- Store secrets in code
- Deploy to prod without approval

## Key Files to Know

| File | Purpose |
|------|---------|
| `.github/workflows/build.yml` | Build and test workflow |
| `.github/workflows/deploy.yml` | Deployment workflow |
| `.github/workflows/dependency-review.yml` | Security scanning |
| `infra/main.bicep` | Main infrastructure template |
| `infra/dev.bicepparam` | Dev environment config |
| `infra/staging.bicepparam` | Staging environment config |
| `infra/prod.bicepparam` | Production environment config |

## Getting Help

- 📖 Read the [Complete CI/CD Documentation](../CI-CD.md)
- 🚀 Check the [Deployment Guide](../DEPLOYMENT.md)
- 🏗️ Review [Infrastructure Details](../infra/README.md)
- 💬 Ask in the team channel
- 🐛 Check workflow logs in Actions tab

## Learning Resources

- [GitHub Actions Basics](https://docs.github.com/en/actions/learn-github-actions)
- [Azure Bicep Tutorial](https://learn.microsoft.com/azure/azure-resource-manager/bicep/learn-bicep)
- [Azure Container Apps](https://learn.microsoft.com/azure/container-apps/)
- [.NET Aspire Documentation](https://learn.microsoft.com/dotnet/aspire)

---

**Need more details?** See the [complete CI/CD documentation](../CI-CD.md).
