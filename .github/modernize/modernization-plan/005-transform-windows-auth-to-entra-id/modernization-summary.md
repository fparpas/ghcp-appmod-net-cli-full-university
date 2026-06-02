# Task 005: Replace Windows Authentication with Microsoft Entra ID

## Overview

This task migrated the ContosoUniversity ASP.NET Core application from Windows Authentication (IIS Express-based) to **Microsoft Entra ID (formerly Azure AD)** using OpenID Connect (OIDC) via the `Microsoft.Identity.Web` library. The application is now cloud-compatible and can be deployed to Azure App Service with Entra ID-based identity management.

---

## Changes Made

### 1. `ContosoUniversity.csproj`
- Added `Microsoft.Identity.Web` (v3.7.1) for OIDC authentication middleware and token handling.
- Added `Microsoft.Identity.Web.UI` (v3.7.1) for the built-in Sign In / Sign Out razor pages (via the `MicrosoftIdentity` area).

### 2. `appsettings.json`
- Added the `AzureAd` configuration section with required OIDC settings:
  - `Instance`: `https://login.microsoftonline.com/`
  - `Domain`, `TenantId`, `ClientId` — placeholder values to be replaced at deployment
  - `CallbackPath`: `/signin-oidc`
  - `SignedOutCallbackPath`: `/signout-callback-oidc`

### 3. `Program.cs`
- Added `using Microsoft.Identity.Web` and `using Microsoft.Identity.Web.UI`.
- Registered Entra ID authentication before the service container is built:
  ```csharp
  builder.Services.AddMicrosoftIdentityWebAppAuthentication(builder.Configuration);
  ```
- Updated `AddControllersWithViews()` to chain `.AddMicrosoftIdentityUI()` for the built-in account pages.
- Added `builder.Services.AddRazorPages()` to support the Microsoft Identity UI area.
- Added `app.UseAuthentication()` to the middleware pipeline (before `app.UseAuthorization()`).
- Added `app.MapRazorPages()` to register Microsoft Identity UI routes.

### 4. `Controllers/BaseController.cs`
- Removed the hardcoded `userName = "System"` pattern.
- Now resolves the authenticated user's display name from Entra ID claims using the recommended priority chain:
  1. `User.FindFirst("name")?.Value` — OIDC `name` claim (display name from Entra ID)
  2. `User.FindFirst(ClaimTypes.Name)?.Value` — standard .NET claim fallback
  3. `User.Identity.Name` — identity name fallback
  4. `"System"` — default when unauthenticated (anonymous access)

### 5. `Views/Shared/_Layout.cshtml`
- Added Sign In / Sign Out navigation links in the navbar using the `MicrosoftIdentity` area:
  - Authenticated users see their display name and a **Sign Out** link.
  - Unauthenticated users see a **Sign In** link.

### 6. `Views/_ViewImports.cshtml`
- Added `@using System.Security.Claims` so that claim operations are available in all views.

### 7. `Views/Home/AccessDenied.cshtml` *(new file)*
- Created an Access Denied view to handle authorization failures gracefully with a user-friendly message and a Sign In link.

---

## Windows Authentication Removal

- The legacy IIS Express Windows Authentication settings (`IISExpressWindowsAuthentication`) referenced in the assessment report are no longer present in the current `.csproj`.
- `Web.config` contained no Windows Authentication directives; no changes were needed there.
- The hardcoded `"System"` user name in `BaseController.cs` (the only code-level reliance on a non-authenticated identity) has been replaced with Entra ID claim extraction.

---

## Build & Test Results

| Metric | Result |
|--------|--------|
| Build | ✅ Succeeded (0 errors) |
| Unit Tests | ✅ 53/53 passed |
| Consistency Check | ✅ No Critical or Major issues |
