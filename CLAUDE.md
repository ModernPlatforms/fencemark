# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build and Run Commands

```bash
# Run the full application (starts all services via .NET Aspire)
dotnet run --project fencemark.AppHost

# Build the solution
dotnet build

# Run all tests
dotnet test

# Run unit tests only (faster)
dotnet test --filter "FullyQualifiedName~AuthServiceTests|FullyQualifiedName~OrganizationServiceTests"

# Run a single test
dotnet test --filter "FullyQualifiedName~TestClassName.TestMethodName"

# Run E2E tests (requires AppHost running)
dotnet test fencemark.Tests --filter "FullyQualifiedName~AllEndpointsE2ETests"

# Publish Blazor WASM client for local nginx serving
dotnet publish fencemark.Client -c Release
```

### E2E Test Environment Variables
```powershell
$env:TEST_USER_EMAIL="test@fencemark.local"
$env:TEST_USER_PASSWORD="TestPassword123!"
$env:TEST_HEADLESS="false"  # Set to see browser
```

### First-time Setup
Install Playwright browsers (required for E2E tests):
```bash
pwsh fencemark.Tests/bin/Debug/net10.0/playwright.ps1 install
```

## Architecture Overview

Fencemark is a **multi-tenant B2B application** for fence contractor job estimation and quoting, built with .NET Aspire orchestration.

### Project Structure

| Project | Description |
|---------|-------------|
| `fencemark.AppHost` | Aspire orchestration - starts SQL Server container, API service, and nginx for WASM client |
| `fencemark.Client` | Blazor WebAssembly frontend (MudBlazor UI), authenticates via Azure Entra External ID |
| `fencemark.ApiService` | ASP.NET Core Minimal API with JWT auth, EF Core, SQL Server |
| `fencemark.ServiceDefaults` | Shared Aspire configuration (OpenTelemetry, health checks, resilience) |
| `fencemark.Tests` | Unit tests, integration tests (Aspire testing), and Playwright E2E tests |

### Key Architecture Patterns

**Multi-Tenancy via SQL Server RLS:**
- `TenantConnectionInterceptor` sets `SESSION_CONTEXT` with OrganizationId before each query
- All tenant-scoped entities implement `IOrganizationScoped` interface
- RLS policies in SQL Server enforce data isolation at database level

**Authentication Flow:**
1. Blazor WASM authenticates via MSAL.js to Azure Entra External ID (CIAM)
2. JWT tokens are sent to API in Authorization header
3. `OnTokenValidated` event auto-creates users and links to organizations
4. Custom claims (`ApplicationUserId`, `OrganizationId`) are added to principal

**Feature-Based Organization:**
- API endpoints organized under `Features/` (Auth, Jobs, Fences, Gates, Components, etc.)
- Each feature has: `*Endpoints.cs` (minimal API routes), models/DTOs
- Services in `Services/` folder (AuthService, OrganizationService, PricingService)

### Data Model Highlights

- **Organization** → owns all tenant data
- **ApplicationUser** → extends IdentityUser with ExternalId (Azure AD oid)
- **OrganizationMember** → links users to organizations with roles
- **Job** → represents a fencing project with line items
- **FenceType/GateType** → product catalog with component compositions
- **Quote** → generated from Job with pricing config, BOM, versioning

### Local Development

The AppHost starts:
1. SQL Server container (port 1433, persistent)
2. API Service with auto-migration
3. nginx container serving published WASM client (HTTP: 5173, HTTPS: 7173)

WASM client connects to API at `https://localhost:62010` (or Aspire-assigned port).

### Infrastructure (Azure)

- **Production**: Blazor WASM on Azure Static Web Apps, API on Container Apps, Azure SQL
- **IaC**: Bicep templates in `infra/` using Azure Verified Modules
- **CI/CD**: GitHub Actions workflows for build, test, and deployment

### Key Configuration Files

- `fencemark.Client/wwwroot/appsettings.json` - WASM client config (gitignored, created at build)
- `fencemark.ApiService/appsettings.json` - API config including AzureAd settings
- `fencemark.AppHost/nginx.conf` - nginx config for local WASM serving


**Sub-Agent Routing & Execution Rules**  
*(Aspire-hosted API + Blazor WebAssembly, C# 10)*

---

## Purpose

This project uses **specialised sub-agents** to improve correctness, safety, and velocity.

Claude MUST route work to the most appropriate sub-agent based on **intent**, **scope**, and **risk**, following the rules below.

Default behaviour should favour **clarity and safety over speed**.

---

## Agent Roster & Responsibilities

### 🧠 Architecture & Aspire Orchestration Agent  
**Model:** Claude 3.5 Opus  
**Role:** System-level design, service boundaries, Aspire wiring

---

### 🔍 Code Reviewer Agent  
**Model:** Claude 3.5 Sonnet (upgrade to Opus for large/high-risk diffs)  
**Role:** Review existing code and changes without rewriting

---

### 🔧 Refactor & Modernisation Agent  
**Model:** Claude 3.5 Sonnet  
**Role:** Incremental refactors that preserve behaviour

---

### 🧪 Testing & QA Agent (Strategy)  
**Model:** Claude 3.5 Sonnet  
**Role:** Decide what to test and identify coverage gaps

---

### ✍️ Code Author Agent  
**Model:** Claude 3.5 Haiku  
**Role:** Write production code in tightly scoped files only

---

### 🧫 Test Author Agent  
**Model:** Claude 3.5 Haiku  
**Role:** Write test code only (unit, integration, or component tests)

---

### 🔐 Security & AuthZ Agent  
**Model:** Claude 3.5 Sonnet (Opus for deep reviews)  
**Role:** Identify and explain security risks

---

### ⚡ Performance & Observability Agent  
**Model:** Claude 3.5 Sonnet  
**Role:** Identify performance issues and observability gaps

---

### 🎨 Blazor UX & Componentisation Agent  
**Model:** Claude 3.5 Sonnet  
**Role:** Improve Blazor WASM UI structure, UX, and rendering behaviour

---

### 📚 Documentation & Onboarding Agent  
**Model:** Claude 3.5 Sonnet  
**Role:** Produce README, feature docs, and onboarding material

---

### 🧾 Git & PR Assistant Agent  
**Model:** Claude 3.5 Haiku (Sonnet if risk analysis is required)  
**Role:** Draft PR descriptions, changelogs, and summaries

---

## Model Selection Rules

- **Opus** → Architecture, deep security, system-wide reasoning  
- **Sonnet** → Review, refactor, strategy, UX, performance  
- **Haiku** → Bounded implementation (code/tests/docs text)

Haiku agents MUST NOT make architectural decisions.

---

## Hard Routing Rules (Intent-Based)

### Architecture & Design
Route to **Architecture & Aspire Orchestration Agent** when:
- Project structure, layering, or service boundaries are discussed
- Aspire host wiring or cross-cutting concerns are involved
- The question is “where should this live?” or “how should this be structured?”

---

### Code Review
Route to **Code Reviewer Agent** when:
- Reviewing existing code or a PR/diff
- The user wants feedback, risks, or critique
- No major restructuring is requested

---

### Refactoring
Route to **Refactor & Modernisation Agent** when:
- Behaviour must be preserved
- Code needs cleanup, splitting, or modernisation
- Scope is larger than a single file but not full architecture

---

### Testing (Strategy vs Implementation)

- **Testing & QA Agent** → deciding *what* to test  
- **Test Author Agent** → writing *actual test code*

Do NOT skip strategy for non-trivial features.

---

### Code Writing

Route to **Code Author Agent** ONLY when:
- Scope is explicitly limited (single file / method / component)
- Requirements are clear
- No architectural decisions are required

---

### Security
Route to **Security & AuthZ Agent** when:
- Auth, authz, tokens, secrets, PII, or trust boundaries are involved
- The question is “is this safe?”

---

### Performance & Observability
Route to **Performance & Observability Agent** when:
- Performance, rendering, payload size, async behaviour, logging, or telemetry are discussed

---

### Blazor UX
Route to **Blazor UX & Componentisation Agent** when:
- The concern is UI structure, UX, validation, or rendering efficiency

---

### Documentation
Route to **Documentation & Onboarding Agent** when:
- Writing or improving README, feature docs, or onboarding content

---

### Git / PR Work
Route to **Git & PR Assistant Agent** when:
- Writing PR descriptions, summaries, or changelogs
- Explaining diffs rather than modifying code

---

## Dispatch Strategy

### Parallel Dispatch  
**ALL conditions must be met:**

- 3+ unrelated tasks or independent domains
- No shared files or state
- Clear, non-overlapping file boundaries

---

### Sequential Dispatch  
**ANY condition triggers:**

- Task dependencies (output of A required by B)
- Shared files or shared state (merge conflict risk)
- Unclear scope requiring investigation before execution
- Architecture or security review before implementation

---

### Background Dispatch

Use background-only agents when:

- Task is research or analysis
- No file modifications are required
- Results are non-blocking

Typical background agents:
- Architecture & Aspire
- Security & AuthZ
- Performance & Observability

---

## Standard Workflow: Think → Write → Verify

1. **Think**  
   - Architecture / Refactor / Testing & QA agent decides structure and approach

2. **Write**  
   - Code Author or Test Author implements narrowly scoped code

3. **Verify**  
   - Code Reviewer validates correctness and alignment with intent

This workflow MUST be followed for non-trivial changes.

---

## Safety Rules

- Only **Code Author**, **Test Author**, and **Refactor Agent** may modify files
- No agent may commit or push to git
- Security and architecture concerns override implementation speed
- When in doubt, ask for clarification before writing code

---

## Default Behaviour

If intent is ambiguous:
1. Pause
2. Clarify scope
3. Route conservatively (Review or Architecture before Authoring)
