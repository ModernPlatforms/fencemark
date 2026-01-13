# Blazor WebAssembly Migration - Implementation Roadmap

**Status:** 📋 Ready to Execute  
**Approach:** Incremental migration with separate features/PRs  
**Total Timeline:** 2-3 weeks across multiple PRs

---

## Overview

This roadmap breaks down the Blazor WASM migration into **6 separate features/PRs** that can be implemented incrementally. This approach allows for:
- ✅ Smaller, reviewable changes
- ✅ Incremental testing and validation
- ✅ Ability to pause/rollback at any stage
- ✅ Parallel work on independent features

---

## Feature Breakdown

### 🎯 Feature 1: API JWT Bearer Authentication Support

**Goal:** Add JWT token validation to API while maintaining backward compatibility with cookies

**Why First:** Foundation for client-side authentication, no frontend changes needed yet

**Changes:**
- Add `Microsoft.AspNetCore.Authentication.JwtBearer` package to API
- Configure dual authentication (JWT + Cookie)
- Update API endpoints to accept both auth methods
- Add CORS configuration for future WASM client

**Files:**
- `fencemark.ApiService/fencemark.ApiService.csproj`
- `fencemark.ApiService/Program.cs`
- `fencemark.ApiService/appsettings.json`

**Testing:**
- Test existing cookie auth still works
- Test JWT token validation (manual with Postman/curl)
- Verify CORS headers

**Estimated Effort:** 4-6 hours  
**Risk:** Low (backward compatible)

**Success Criteria:**
- [ ] API accepts JWT Bearer tokens
- [ ] Existing cookie authentication unchanged
- [ ] CORS configured for WASM origin
- [ ] All existing tests pass
- [ ] Manual JWT token test succeeds

---

### 🎯 Feature 2: Create Blazor WASM Project

**Goal:** Create new `fencemark.Client` project with basic structure

**Why Second:** Foundation for client-side app, doesn't affect existing system

**Changes:**
- Create new `fencemark.Client` project (Blazor WebAssembly)
- Add MSAL authentication configuration
- Setup project structure (Components, Pages, Layout)
- Add MudBlazor and dependencies
- Configure for local development

**Files:**
- `fencemark.Client/fencemark.Client.csproj` (new)
- `fencemark.Client/Program.cs` (new)
- `fencemark.Client/wwwroot/appsettings.json` (new)
- `fencemark.Client/Components/App.razor` (new)
- `fencemark.slnx` (add project reference)

**Testing:**
- Build succeeds
- App runs locally
- MSAL authentication flow works
- Can call API with JWT token

**Estimated Effort:** 6-8 hours  
**Risk:** Low (new project, no impact on existing)

**Success Criteria:**
- [ ] Project builds successfully
- [ ] App runs on localhost
- [ ] MSAL login flow works
- [ ] Can authenticate and get JWT token
- [ ] Basic layout renders

---

### 🎯 Feature 3: Migrate Core Components

**Goal:** Migrate App.razor, MainLayout, and core shared components

**Why Third:** Establishes component migration pattern

**Changes:**
- Copy and adapt core components from `fencemark.Web`
- Remove `@rendermode` directives
- Update authentication components for MSAL
- Migrate shared components (LoadingState, AppCard, etc.)

**Files:**
- `fencemark.Client/Components/App.razor`
- `fencemark.Client/Components/Layout/MainLayout.razor` (new)
- `fencemark.Client/Components/Layout/NavMenu.razor` (new)
- `fencemark.Client/Components/Shared/*.razor` (new)
- `fencemark.Client/Components/_Imports.razor`

**Testing:**
- Components render correctly
- Navigation works
- Authentication state displays
- MudBlazor components work

**Estimated Effort:** 8-10 hours  
**Risk:** Low (mostly copy/adapt)

**Success Criteria:**
- [ ] Layout renders correctly
- [ ] Navigation menu works
- [ ] Authentication state displays
- [ ] Shared components render
- [ ] No console errors

---

### 🎯 Feature 4: Migrate API Clients and Feature Pages

**Goal:** Migrate API client classes and all feature pages (Fences, Gates, Components, Jobs, etc.)

**Why Fourth:** Main business logic migration

**Changes:**
- Create API client classes with JWT token attachment
- Migrate all page components from `fencemark.Web/Components/Pages`
- Update API calls to use new clients
- Remove server-specific dependencies

**Files:**
- `fencemark.Client/Features/Auth/AuthApiClient.cs` (new)
- `fencemark.Client/Features/Fences/FenceApiClient.cs` (new)
- `fencemark.Client/Features/Gates/GateApiClient.cs` (new)
- `fencemark.Client/Features/Components/ComponentApiClient.cs` (new)
- `fencemark.Client/Features/Jobs/JobApiClient.cs` (new)
- `fencemark.Client/Components/Pages/Home.razor` (new)
- `fencemark.Client/Components/Pages/Fences.razor` (new)
- `fencemark.Client/Components/Pages/Gates.razor` (new)
- `fencemark.Client/Components/Pages/Components.razor` (new)
- `fencemark.Client/Components/Pages/Jobs.razor` (new)
- And other feature pages...

**Testing:**
- All pages render
- CRUD operations work
- Data loads from API
- Authentication required on protected pages
- Form validation works

**Estimated Effort:** 16-20 hours  
**Risk:** Medium (main feature migration)

**Success Criteria:**
- [ ] All pages migrated
- [ ] All CRUD operations work
- [ ] API calls succeed with JWT
- [ ] Error handling works
- [ ] Loading states display correctly
- [ ] Data isolation per organization works

---

### 🎯 Feature 5: Azure Static Web App Infrastructure

**Goal:** Setup Azure Static Web Apps infrastructure and deployment

**Why Fifth:** Hosting infrastructure for WASM app

**Changes:**
- Create Bicep template for Azure Static Web Apps
- Link to existing Container App API
- Configure custom domain (if needed)
- Setup environment-specific configurations
- Update parameter files

**Files:**
- `infra/modules/static-web-app.bicep` (new)
- `infra/main.bicep` (updated)
- `infra/dev.bicepparam` (updated)
- `infra/staging.bicepparam` (updated)
- `infra/prod.bicepparam` (updated)

**Testing:**
- Bicep templates validate
- Test deployment to dev environment
- Verify Static Web App serves content
- Verify API backend link works

**Estimated Effort:** 6-8 hours  
**Risk:** Medium (infrastructure changes)

**Success Criteria:**
- [ ] Bicep templates validate
- [ ] Dev deployment succeeds
- [ ] Static Web App accessible
- [ ] API calls work from Static Web App
- [ ] Authentication flow works end-to-end

---

### 🎯 Feature 6: CI/CD Pipeline and Production Deployment

**Goal:** Setup GitHub Actions for WASM deployment and deploy to production

**Why Last:** Final piece - automated deployment

**Changes:**
- Create GitHub Actions workflow for Static Web App deployment
- Update existing workflows to handle both projects
- Configure deployment secrets
- Setup staging and production environments
- Deploy to production
- Update documentation

**Files:**
- `.github/workflows/deploy-static-web-app.yml` (new)
- `.github/workflows/deploy.yml` (updated)
- `fencemark.AppHost/AppHost.cs` (updated - optional for local dev)
- `README.md` (updated)
- `DEPLOYMENT.md` (updated)

**Testing:**
- Workflow builds successfully
- Deployment to dev works
- Deployment to staging works
- Production deployment (manual approval)
- Health checks pass
- Monitoring configured

**Estimated Effort:** 8-10 hours  
**Risk:** Medium (deployment automation)

**Success Criteria:**
- [ ] GitHub Actions workflow works
- [ ] Automated deployment to dev
- [ ] Manual deployment to staging/prod
- [ ] Health checks configured
- [ ] Monitoring active
- [ ] Documentation updated
- [ ] Old Blazor Server can be decommissioned

---

## Migration Strategy

### Parallel Operations

The following can be worked on in parallel:
- Feature 1 (API JWT) + Feature 2 (WASM project)
- Feature 3 (Core components) + Feature 4 (API clients - can start)
- Feature 5 (Infrastructure) can start anytime after Feature 2

### Sequential Dependencies

```
Feature 1 (API JWT)
    ↓
Feature 2 (WASM Project)
    ↓
Feature 3 (Core Components)
    ↓
Feature 4 (Feature Pages & API Clients)
    ↓
Feature 5 (Infrastructure)
    ↓
Feature 6 (CI/CD & Production)
```

### Rollback Points

After each feature, the system remains functional:
- After Feature 1: Existing app works, API supports JWT (unused)
- After Feature 2-4: New WASM app exists, old app still deployed
- After Feature 5: Both apps can run simultaneously
- After Feature 6: Can switch via DNS, rollback available

---

## Recommended PR Strategy

### PR #1: API JWT Support (Feature 1)
**Title:** Add JWT Bearer authentication support to API  
**Size:** Small (~200 lines)  
**Review Time:** 30 minutes

### PR #2: Create Blazor WASM Project (Feature 2)
**Title:** Create Blazor WebAssembly client project with MSAL authentication  
**Size:** Medium (~500 lines)  
**Review Time:** 1 hour

### PR #3: Migrate Core Components (Feature 3)
**Title:** Migrate core layout and shared components to Blazor WASM  
**Size:** Medium (~600 lines)  
**Review Time:** 1 hour

### PR #4: Migrate Feature Pages (Feature 4)
**Title:** Migrate all feature pages and API clients to Blazor WASM  
**Size:** Large (~2000+ lines)  
**Review Time:** 2-3 hours  
**Note:** Can be split into multiple PRs by feature area if too large

### PR #5: Static Web App Infrastructure (Feature 5)
**Title:** Add Azure Static Web Apps infrastructure  
**Size:** Small (~300 lines)  
**Review Time:** 45 minutes

### PR #6: Production Deployment (Feature 6)
**Title:** Add CI/CD for Static Web App and production deployment  
**Size:** Medium (~400 lines)  
**Review Time:** 1 hour

---

## Timeline Estimation

### Optimistic (Full-time focus, no blockers)
- Week 1: Features 1-3 (API + WASM project + Core components)
- Week 2: Feature 4 (Feature pages migration)
- Week 3: Features 5-6 (Infrastructure + Deployment)

### Realistic (Normal velocity, some reviews/iterations)
- Week 1: Features 1-2
- Week 2: Features 3-4
- Week 3: Features 5-6

### Conservative (Part-time, multiple review cycles)
- Weeks 1-2: Features 1-3
- Weeks 3-4: Feature 4
- Week 5: Features 5-6

---

## Risk Mitigation

### Feature 1 Risks
- **Risk:** Breaking existing cookie auth
- **Mitigation:** Dual authentication, comprehensive testing

### Feature 2 Risks
- **Risk:** MSAL configuration issues
- **Mitigation:** Follow Microsoft docs, test early

### Feature 3 Risks
- **Risk:** Component incompatibilities
- **Mitigation:** Test each component, MudBlazor is WASM-compatible

### Feature 4 Risks
- **Risk:** Large migration, many files
- **Mitigation:** Split into sub-PRs if needed, incremental testing

### Feature 5 Risks
- **Risk:** Infrastructure deployment issues
- **Mitigation:** Test in dev first, keep old infrastructure

### Feature 6 Risks
- **Risk:** Production deployment issues
- **Mitigation:** Staging environment, manual approval, rollback plan

---

## Testing Strategy

### Per-Feature Testing
Each PR should include:
- Unit tests (where applicable)
- Integration tests
- Manual testing checklist
- Screenshot/video for UI changes

### End-to-End Testing
After Feature 4:
- Full user journey testing
- Performance testing
- Security testing
- Cross-browser testing

### Production Readiness
Before Feature 6:
- Load testing
- Penetration testing
- Disaster recovery testing
- Documentation review

---

## Success Metrics

### Technical Metrics
- [ ] All features work identically to Blazor Server
- [ ] Initial load time < 5 seconds
- [ ] UI interactions < 100ms
- [ ] No security vulnerabilities
- [ ] Zero critical bugs in production

### Business Metrics
- [ ] Hosting costs reduced by ≥50%
- [ ] User satisfaction maintained
- [ ] Zero downtime during migration
- [ ] Team comfortable with new architecture

---

## Communication Plan

### Stakeholder Updates
- **Weekly:** Progress update (features completed, blockers)
- **Per PR:** Demo of new functionality
- **After Feature 4:** User acceptance testing
- **Before Feature 6:** Production readiness review

### Team Updates
- **Daily:** Standups - progress, blockers
- **Per Feature:** Knowledge sharing session
- **After Feature 6:** Retrospective

---

## Rollback Plan

### If Issues Found in Feature 1-2
- Revert PR, fix issues, re-submit
- No user impact (not deployed)

### If Issues Found in Feature 3-4
- Fix forward (small issues)
- Revert PR (major issues)
- No user impact (not deployed)

### If Issues Found in Feature 5-6
- Keep old Blazor Server running
- Fix Static Web App issues
- Switch DNS back if critical
- Full rollback capability maintained

---

## Post-Migration

### Decommission Old Blazor Server
After 2-4 weeks of stable operation:
- [ ] Verify all users migrated
- [ ] No traffic to old app
- [ ] Remove old Container App
- [ ] Update documentation
- [ ] Archive old code

### Optimization Opportunities
After migration stabilizes:
- Enable AOT compilation (reduce size)
- Add service worker (PWA)
- Implement lazy loading
- Add offline support
- Performance optimization

---

## Next Steps

1. **Create GitHub Issues** for each feature
2. **Assign Features 1-2** to developer(s)
3. **Setup dev environment** for Static Web Apps
4. **Begin Feature 1** (API JWT support)

---

## Questions?

- **Q: Can we skip any features?**  
  A: No, all are required for complete migration.

- **Q: Can we change the order?**  
  A: Features 1-2 can be swapped, but rest should follow sequence.

- **Q: Can we do features in one big PR?**  
  A: Not recommended - harder to review, test, and rollback.

- **Q: What if we need to pause?**  
  A: Can pause after any feature. System remains functional.

---

**Document Version:** 1.0  
**Last Updated:** January 2026  
**Status:** 📋 Ready for Implementation
