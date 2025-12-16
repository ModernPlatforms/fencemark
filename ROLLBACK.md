# Rollback Procedures

This document provides comprehensive guidance on rolling back deployments in the Fencemark application when issues are detected in production or staging environments.

## Table of Contents

- [Overview](#overview)
- [When to Rollback](#when-to-rollback)
- [Rollback Methods](#rollback-methods)
- [Automated Rollback](#automated-rollback)
- [Manual Rollback](#manual-rollback)
- [Post-Rollback Validation](#post-rollback-validation)
- [Common Scenarios](#common-scenarios)
- [Prevention](#prevention)

## Overview

Fencemark uses Azure Container Apps for deployment, which provides multiple mechanisms for rolling back to previous stable versions. Each deployment creates a new revision, and Azure Container Apps can quickly switch traffic between revisions.

**Key Concepts:**
- **Revision**: An immutable snapshot of your container app configuration and container image
- **Active Revision**: The revision currently receiving traffic
- **Traffic Split**: The percentage of traffic directed to each revision

## When to Rollback

Consider rolling back immediately when:

- ✅ **Health checks fail** after deployment
- ✅ **Critical functionality is broken** (e.g., users can't login, create jobs, or generate quotes)
- ✅ **Performance degradation** exceeds acceptable thresholds (>2x response time)
- ✅ **Security vulnerabilities** are discovered in the deployed version
- ✅ **Data corruption** or integrity issues are detected
- ✅ **High error rates** in application logs (>5% of requests)

Do NOT rollback for:
- ❌ Minor UI issues that don't affect functionality
- ❌ Non-critical bugs that have workarounds
- ❌ Performance improvements that need tuning

## Rollback Methods

### Method 1: Revision Switch (Fastest - Recommended)

**Speed:** ~30 seconds  
**Risk:** Low  
**Use Case:** Quick rollback to last known good state

Azure Container Apps maintains previous revisions. You can instantly switch traffic back to a previous revision.

**Advantages:**
- No new deployment required
- Instant traffic switch
- Can be done via Azure Portal or CLI
- No build time

**Limitations:**
- Only works if previous revision still exists
- Limited to revisions within retention period (typically 100 revisions)

### Method 2: Redeploy Previous Image (Standard)

**Speed:** ~5-10 minutes  
**Risk:** Low-Medium  
**Use Case:** When previous revision is not available

Redeploy the container image from a previous commit.

**Advantages:**
- Works with any previous commit
- Full control over deployment
- Can be scripted

**Limitations:**
- Requires build and push time
- Infrastructure deployment overhead

### Method 3: Restore from Backup (Database Issues)

**Speed:** Varies (10 minutes - 2 hours)  
**Risk:** High (potential data loss)  
**Use Case:** Database corruption or data integrity issues

**⚠️ WARNING:** This should be a last resort and requires careful coordination.

## Automated Rollback

### Deployment Workflow Rollback

The deploy.yml workflow includes automatic health checks that will fail the deployment if services are unhealthy. This prevents bad deployments from completing.

**What happens:**
1. Deployment completes
2. Health checks run with 5 retries
3. If health checks fail, deployment is marked as failed
4. Manual rollback is then required

**To enable fully automated rollback:**

Update your workflow to include:

```yaml
- name: Rollback on health check failure
  if: failure() && steps.health-check-api.outputs.status != 'success'
  run: |
    # Get previous successful revision
    PREVIOUS_REVISION=$(az containerapp revision list \
      --name ${{ steps.get-outputs.outputs.api-service-name }} \
      --resource-group ${{ steps.get-outputs.outputs.resource-group }} \
      --query "[?properties.trafficWeight>0 && properties.active && properties.healthState=='Healthy'] | [1].name" \
      --output tsv)
    
    # Activate previous revision
    az containerapp revision activate \
      --name $PREVIOUS_REVISION \
      --resource-group ${{ steps.get-outputs.outputs.resource-group }}
```

## Manual Rollback

### Quick Rollback Using Azure CLI

#### Step 1: List Recent Revisions

```bash
# Set environment
ENVIRONMENT="prod"  # or dev, staging
RESOURCE_GROUP="rg-fencemark-${ENVIRONMENT}"
API_APP_NAME="ca-apiservice"
WEB_APP_NAME="ca-webfrontend"

# List API Service revisions
az containerapp revision list \
  --name $API_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --query "[].{Name:name, Created:properties.createdTime, Traffic:properties.trafficWeight, Active:properties.active, Health:properties.healthState}" \
  --output table

# List Web Frontend revisions
az containerapp revision list \
  --name $WEB_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --query "[].{Name:name, Created:properties.createdTime, Traffic:properties.trafficWeight, Active:properties.active, Health:properties.healthState}" \
  --output table
```

#### Step 2: Identify Target Revision

Look for the previous revision that:
- Has `Active: True`
- Has `Health: Healthy`
- Was created before the problematic deployment

#### Step 3: Switch Traffic to Previous Revision

```bash
# Activate previous revision for API Service
az containerapp revision activate \
  --name <PREVIOUS_REVISION_NAME> \
  --resource-group $RESOURCE_GROUP

# Set 100% traffic to previous revision
az containerapp ingress traffic set \
  --name $API_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --revision-weight <PREVIOUS_REVISION_NAME>=100

# Repeat for Web Frontend
az containerapp revision activate \
  --name <WEB_PREVIOUS_REVISION_NAME> \
  --resource-group $RESOURCE_GROUP

az containerapp ingress traffic set \
  --name $WEB_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --revision-weight <WEB_PREVIOUS_REVISION_NAME>=100
```

#### Step 4: Deactivate Problematic Revision

```bash
# Deactivate the problematic revision
az containerapp revision deactivate \
  --name <PROBLEMATIC_REVISION_NAME> \
  --resource-group $RESOURCE_GROUP
```

### Rollback Using Azure Portal

1. Navigate to **Azure Portal** → **Resource Groups** → `rg-fencemark-{env}`
2. Select the Container App (`ca-apiservice` or `ca-webfrontend`)
3. Go to **Revisions and replicas**
4. Find the previous healthy revision
5. Click on the revision → **Activate**
6. Under **Traffic**, set 100% to the activated revision
7. Click **Save**
8. Repeat for other container app if needed

### Rollback Using Script

A helper script is provided for quick rollbacks:

```bash
# Download and make executable
chmod +x infra/scripts/rollback.sh

# Rollback production
./infra/scripts/rollback.sh prod

# Rollback with specific revision (optional)
./infra/scripts/rollback.sh prod <REVISION_NAME>
```

## Post-Rollback Validation

After rolling back, verify the system is stable:

### 1. Health Checks

```bash
# Check API health
curl https://<api-fqdn>/health

# Check Web health
curl https://<web-url>/health
```

### 2. Smoke Tests

```bash
# Test home page
curl -I https://<web-url>/

# Test API endpoints (requires auth)
curl -H "Authorization: Bearer <token>" https://<api-fqdn>/api/jobs
```

### 3. Monitor Logs

```bash
# View Container App logs
az containerapp logs show \
  --name $API_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --follow \
  --tail 100
```

### 4. Check Metrics

- Navigate to Azure Portal → Container App → Metrics
- Monitor:
  - Request count
  - Response time
  - Error rate
  - CPU/Memory usage

### 5. User Verification

- Test critical workflows:
  - User login
  - Create job
  - Generate quote
  - View pricing

## Common Scenarios

### Scenario 1: Broken Authentication After Deployment

**Symptoms:**
- Users cannot login
- 401/403 errors in logs
- Health checks may still pass

**Rollback Steps:**
1. Immediately switch to previous revision (Method 1)
2. Verify authentication works
3. Investigate configuration changes
4. Fix in non-production environment
5. Redeploy with fix

### Scenario 2: Database Migration Failed

**Symptoms:**
- Application crashes on startup
- Database connection errors
- Health checks fail

**Rollback Steps:**
1. Roll back application to previous revision
2. Assess database state
3. If needed, rollback database migration manually
4. Verify data integrity
5. Fix migration scripts
6. Test in staging thoroughly before redeploying

### Scenario 3: Performance Degradation

**Symptoms:**
- Slow response times
- Timeouts
- High CPU/memory usage
- Health checks may pass but slow

**Rollback Steps:**
1. Monitor metrics to confirm degradation
2. If >2x slower than baseline, roll back
3. Analyze performance issue in non-production
4. Optimize and load test before redeploying

### Scenario 4: Feature Causing Issues

**Symptoms:**
- Specific feature broken
- Other features work fine
- Partial functionality loss

**Options:**
1. **Feature flag off** (if implemented)
2. **Quick hotfix** deployment
3. **Full rollback** if critical

## Prevention

### Before Deployment

- ✅ Run all unit and integration tests
- ✅ Deploy to staging first and validate
- ✅ Run E2E tests in staging
- ✅ Perform load testing for major changes
- ✅ Review infrastructure changes carefully
- ✅ Have rollback plan ready
- ✅ Communicate deployment window to team

### During Deployment

- ✅ Monitor health checks in real-time
- ✅ Watch application logs
- ✅ Monitor Azure metrics
- ✅ Have team member ready to rollback if needed
- ✅ Deploy during low-traffic periods
- ✅ Consider blue-green deployment for critical changes

### After Deployment

- ✅ Monitor for 30-60 minutes post-deployment
- ✅ Run smoke tests immediately
- ✅ Check error rates and response times
- ✅ Review user feedback channels
- ✅ Document any issues found

### Deployment Best Practices

1. **Use Canary Deployments** for major changes
2. **Implement Feature Flags** for easy rollback of features
3. **Maintain Database Backward Compatibility** for at least one version
4. **Test Rollback Procedures** regularly in staging
5. **Document Known Issues** in each deployment
6. **Keep Deployment Windows Short** (< 30 minutes)
7. **Automate Health Checks** at every stage

## Emergency Contacts

In case of critical production issues:

1. **On-Call Engineer**: Check incident management system
2. **DevOps Team**: Slack channel `#devops-alerts`
3. **Platform Team**: Email `platform-team@company.com`

## Rollback Checklist

Use this checklist during rollback:

- [ ] Confirm rollback is necessary (severity assessment)
- [ ] Notify team in `#deployments` channel
- [ ] Identify target revision or commit SHA
- [ ] Execute rollback (CLI, Portal, or Script)
- [ ] Verify health checks pass
- [ ] Run smoke tests
- [ ] Monitor logs for errors
- [ ] Check application metrics
- [ ] Test critical user workflows
- [ ] Document incident and root cause
- [ ] Update postmortem document
- [ ] Schedule debrief meeting
- [ ] Implement preventive measures

## Related Documentation

- [CI/CD Pipeline Documentation](CI-CD.md)
- [Deployment Guide](DEPLOYMENT.md)
- [Infrastructure Overview](infra/README.md)
- [Aspire Orchestration](ASPIRE_ORCHESTRATION.md)

---

**Last Updated:** 2025-12-16  
**Version:** 1.0  
**Maintained By:** Fencemark DevOps Team
