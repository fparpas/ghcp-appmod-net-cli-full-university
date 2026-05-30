# Modernization Summary — Task 005: Windows Authentication → Microsoft Entra ID

## Overview

Replaced legacy Windows Authentication (IIS Express) with Microsoft Entra ID (formerly Azure Active Directory) authentication using the `Microsoft.Identity.Web` library for ASP.NET Core.

## Changes Made

### 1. `ContosoUniversity.csproj`
- Added `Microsoft.Identity.Web` (v3.7.1) — core Entra ID integration for ASP.NET Core
- Added `Microsoft.Identity.Web.UI` (v3.7.1) — built-in sign-in/sign-out Razor Pages UI

### 2. `Program.cs`
- Added `using Microsoft.Identity.Web` and `using Microsoft.Identity.Web.UI`
- Replaced bare `AddControllersWithViews()` with Entra ID-enabled setup:
  - `AddMicrosoftIdentityWebAppAuthentication(builder.Configuration)` — registers OIDC/cookie authentication from `AzureAd` config section
  - `.AddMicrosoftIdentityUI()` — wires in the `/MicrosoftIdentity/Account/SignIn` and `SignOut` endpoints
  - `AddRazorPages()` — required by the Identity UI
- Added `app.UseAuthentication()` **before** `app.UseAuthorization()` (correct ASP.NET Core middleware order)
- Added `app.MapRazorPages()` to route the Identity UI endpoints

### 3. `appsettings.json`
- Added `AzureAd` configuration section with all required fields:
  - `Instance`, `Domain`, `TenantId`, `ClientId`, `CallbackPath`, `SignedOutCallbackPath`
- Values are placeholder templates (`your-tenant-id`, `your-client-id`) to be replaced with real Entra ID app registration values at deployment time

### 4. `Controllers/BaseController.cs`
- Removed hard-coded `userName = "System"` sentinel
- Now reads the authenticated user's name from Entra ID claims with a fallback chain:
  1. `name` claim (primary Entra ID display-name claim)
  2. `ClaimTypes.Name` claim
  3. `User.Identity.Name`
  4. `"Unknown"` as a final fallback
- Unauthenticated requests continue to use `"System"` as the notification user

### 5. `Views/Shared/_Layout.cshtml`
- Added `@using System.Security.Claims` import
- Added sign-in/sign-out navigation links to the navbar:
  - When authenticated: shows the user's display name (from `name` claim) and a **Sign Out** link routing to `MicrosoftIdentity/Account/SignOut`
  - When not authenticated: shows a **Sign In** link routing to `MicrosoftIdentity/Account/SignIn`

### 6. `Views/Home/Index.cshtml`
- Updated description text to remove Windows Authentication reference:
  - Before: "Entity Framework 6 in an ASP.NET MVC 5 application with Windows Authentication"
  - After: "Entity Framework Core in an ASP.NET Core MVC application with Microsoft Entra ID authentication"

### 7. `Properties/launchSettings.json` (new)
- Created new file explicitly setting `windowsAuthentication: false` and `anonymousAuthentication: true` for local IIS Express profile
- Defines HTTPS (`https://localhost:7001`) and HTTP (`http://localhost:5000`) launch profiles

## Verification

| Check | Result |
|-------|--------|
| Build | ✅ Succeeded (0 errors) |
| Unit Tests | ✅ 48/48 passed |
| Consistency Check | ✅ No Critical or Major issues |

## Deployment Notes

Before going to production, update `appsettings.json` (or environment configuration) with real values:
```json
"AzureAd": {
  "Instance": "https://login.microsoftonline.com/",
  "Domain": "<your-aad-domain>.onmicrosoft.com",
  "TenantId": "<your-tenant-guid>",
  "ClientId": "<your-app-registration-client-id>",
  "CallbackPath": "/signin-oidc",
  "SignedOutCallbackPath": "/signout-callback-oidc"
}
```

Register a Web application in the Entra ID portal with:
- Redirect URI: `https://<app-url>/signin-oidc`
- Front-channel logout URL: `https://<app-url>/signout-callback-oidc`
