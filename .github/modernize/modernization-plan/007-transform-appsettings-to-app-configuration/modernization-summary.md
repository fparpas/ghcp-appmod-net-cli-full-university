# Modernization Summary – 007-transform-appsettings-to-app-configuration

## Objective
Externalize non-secret application settings from `appsettings.json` / `Web.config` to Azure App Configuration using Managed Identity, while preserving the existing `IConfiguration` / `IOptions<T>` binding shape so no application code changes are required.

## Changes Made

### 1. `ContosoUniversity.csproj`
Added the Azure App Configuration provider NuGet package:
```xml
<PackageReference Include="Microsoft.Extensions.Configuration.AzureAppConfiguration" Version="8.0.0" />
```
`Azure.Identity` (1.14.0) was already present from prior migration tasks.

### 2. `Program.cs`
Wired Azure App Configuration as a configuration source early in the builder setup (after default file-based providers so App Configuration values can override local defaults during cutover):
```csharp
var appConfigEndpoint = Environment.GetEnvironmentVariable("AZURE_APP_CONFIGURATION_ENDPOINT");
if (!string.IsNullOrEmpty(appConfigEndpoint))
{
    builder.Configuration.AddAzureAppConfiguration(options =>
    {
        options.Connect(new Uri(appConfigEndpoint), new DefaultAzureCredential());
    });
}
```
- The endpoint is always read from `AZURE_APP_CONFIGURATION_ENDPOINT` (injected by the deployment agent).
- Authentication uses `DefaultAzureCredential` (Managed Identity) — no connection strings or secrets.
- The conditional block allows local development to continue without an App Configuration store configured.

### 3. `.azure/configuration-migration.json` *(new file)*
Emitted the seed file for the deployment agent containing all non-secret leaf values from `appsettings.json`:

| Key | Notes |
|-----|-------|
| `AzureAd:Instance` | Entra ID instance URL |
| `AzureAd:Domain` | Tenant domain |
| `AzureAd:TenantId` | Tenant identifier |
| `AzureAd:ClientId` | App registration client ID |
| `AzureAd:CallbackPath` | OIDC callback path |
| `AzureAd:SignedOutCallbackPath` | OIDC sign-out callback path |
| `AzureServiceBus:FullyQualifiedNamespace` | Service Bus namespace FQDN |
| `AzureServiceBus:QueueName` | Service Bus queue name |
| `Storage:ServiceUri` | Blob Storage service URI |
| `Storage:ContainerName` | Blob container name |
| `ConnectionStrings:DefaultConnection` | Azure SQL connection string (Managed Identity, no password) |

**Skipped (per guidance):**
- `Logging` — runtime-host concern, stays in `appsettings.json`
- `AllowedHosts` — runtime-host concern, stays in `appsettings.json`

**Legacy `Web.config` `<appSettings>` entries** (`webpages:Version`, `webpages:Enabled`, `ClientValidationEnabled`, `UnobtrusiveJavaScriptEnabled`, `NotificationQueuePath`) are obsolete ASP.NET MVC 5 / MSMQ settings not applicable to the migrated .NET 10 application and were not migrated.

## Preserved Behaviour
All existing `IConfiguration` call sites (e.g., `builder.Configuration["AzureServiceBus:FullyQualifiedNamespace"]`, `builder.Configuration.GetConnectionString("DefaultConnection")`, `builder.Configuration["Storage:ServiceUri"]`) continue to work unchanged because Azure App Configuration is added as an additional provider in the same `IConfigurationRoot`.

## Build & Test Results
- **Build**: ✅ 0 errors, 0 warnings (excluding pre-existing CVE advisory warnings)
- **Unit Tests**: ✅ 48/48 passed

## Consistency Check
No Critical or Major issues identified.
