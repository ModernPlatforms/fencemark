# JWT Bearer Authentication - Manual Testing Guide

This guide provides steps to manually test the JWT Bearer authentication implementation for the API.

## Prerequisites

- The API service must be running
- You need a tool like Postman, curl, or Thunder Client (VS Code extension)
- You have valid Azure AD JWT tokens OR can register a user via the existing auth endpoints

## Test 1: Verify CORS Headers

Test that CORS is properly configured for the future WASM client.

### Using curl:

```bash
curl -H "Origin: https://localhost:5001" \
     -H "Access-Control-Request-Method: GET" \
     -H "Access-Control-Request-Headers: Authorization" \
     -X OPTIONS \
     -v \
     http://localhost:5000/api/fences
```

**Expected Result:**
- Response should include CORS headers:
  - `Access-Control-Allow-Origin: https://localhost:5001`
  - `Access-Control-Allow-Methods: GET, POST, PUT, DELETE...`
  - `Access-Control-Allow-Headers: Authorization...`
  - `Access-Control-Allow-Credentials: true`

## Test 2: Verify JWT Token Validation

Test that the API accepts and validates JWT Bearer tokens.

### Step 1: Get a JWT Token

You have two options:

#### Option A: Use Azure AD/Entra External ID (Production)

1. Authenticate through the configured Azure AD tenant
2. Extract the JWT access token from the authentication response
3. Use this token in the Authorization header

#### Option B: Use the existing API auth endpoints (Development)

```bash
# Register a new user
curl -X POST http://localhost:5000/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@example.com",
    "password": "Password123!",
    "organizationName": "Test Company"
  }'

# Login to get session cookie (existing cookie auth still works)
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@example.com",
    "password": "Password123!"
  }' \
  -c cookies.txt

# Access protected endpoint with cookie
curl -X GET http://localhost:5000/api/fences \
  -b cookies.txt
```

### Step 2: Test JWT Bearer Token

With a valid JWT token:

```bash
curl -X GET http://localhost:5000/api/fences \
  -H "Authorization: Bearer <YOUR_JWT_TOKEN_HERE>" \
  -H "Origin: https://localhost:5001"
```

**Expected Result:**
- Status Code: 200 OK (if authenticated) or 401 Unauthorized (if token invalid)
- Response includes CORS headers
- No redirect to login page (API should return status codes, not redirects)

## Test 3: Verify Dual Authentication Support

Confirm that both cookie-based and JWT authentication work simultaneously.

### Test Cookie Auth (Existing):

```bash
# Login and get cookie
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@example.com",
    "password": "Password123!"
  }' \
  -c cookies.txt \
  -v

# Access protected endpoint with cookie
curl -X GET http://localhost:5000/api/fences \
  -b cookies.txt
```

**Expected Result:** 200 OK with fence data

### Test JWT Auth (New):

```bash
# Access protected endpoint with Bearer token
curl -X GET http://localhost:5000/api/fences \
  -H "Authorization: Bearer <YOUR_JWT_TOKEN_HERE>"
```

**Expected Result:** 200 OK with fence data (same as cookie auth)

## Test 4: Verify Unauthorized Access Returns Proper Status

Test that unauthorized requests return 401, not redirects.

```bash
# Try to access protected endpoint without auth
curl -X GET http://localhost:5000/api/fences -v
```

**Expected Result:**
- Status Code: 401 Unauthorized
- NOT a redirect (302/303) to a login page
- Response body may contain error details

## Test 5: Check API Startup Logs

Verify CORS configuration is logged at startup:

```bash
# Start the API and check logs
dotnet run --project fencemark.ApiService/fencemark.ApiService.csproj
```

**Expected Output in Logs:**
```
[ApiService] CORS configured for 2 origins: https://localhost:5001, https://localhost:7001
```

## Success Criteria Checklist

- [ ] CORS headers are present in OPTIONS preflight requests
- [ ] CORS headers include `Access-Control-Allow-Credentials: true`
- [ ] JWT Bearer tokens are accepted in `Authorization: Bearer <token>` header
- [ ] Cookie authentication still works (backward compatibility)
- [ ] Unauthorized requests return 401 status, not redirects
- [ ] API logs show CORS configuration at startup

## Troubleshooting

### Issue: CORS headers not appearing

**Check:**
- Is the Origin header set in the request?
- Is the origin in the allowed list? (https://localhost:5001 or https://localhost:7001)
- Check appsettings.json for Cors:AllowedOrigins configuration

### Issue: JWT token not accepted

**Check:**
- Is the token in the correct format: `Authorization: Bearer <token>`?
- Is the token from the correct Azure AD tenant configured in appsettings?
- Check API logs for authentication errors
- Verify token hasn't expired

### Issue: Cookie auth broken

**Check:**
- Are you sending cookies with requests (`-b cookies.txt` in curl)?
- Is the session still valid?
- Try registering a new user and logging in again

## Notes for Future WASM Client

When implementing the Blazor WASM client (Feature 2):

1. Add the actual Static Web App URL to `appsettings.json` CORS origins
2. Configure the WASM client to send JWT tokens in the Authorization header
3. Ensure the WASM client includes credentials in fetch requests for CORS
4. Test cross-origin requests from the WASM client to the API

## References

- JWT Bearer authentication: Microsoft.AspNetCore.Authentication.JwtBearer
- CORS documentation: https://learn.microsoft.com/aspnet/core/security/cors
- Azure AD tokens: https://learn.microsoft.com/azure/active-directory/develop/
