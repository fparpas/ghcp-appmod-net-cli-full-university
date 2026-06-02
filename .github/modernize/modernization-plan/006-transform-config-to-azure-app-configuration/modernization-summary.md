# Modernization Summary: 006-transform-config-to-azure-app-configuration

## Objective

Externalize non-secret application settings from `Web.config` / `appsettings.json` to Azure App Configuration, eliminating hardcoded configuration in the application.

## Changes Made

### 1. `ContosoUniversity.csproj` — Added NuGet package reference

Added the Azure App Configuration SDK provider package:

```xml
<PackageReference Include="Microsoft.Extensions.Configuration.AzureAppConfiguration" Version="8.0.0" />
```

### 2. `Program.cs` — Wired up Azure App Configuration

Registered Azure App Configuration as an additional `IConfiguration` source, placed **after** the default file-based providers so App Configuration values take precedence during cutover. The integration is conditional: the app falls back to `appsettings.json` when the environment variable is absent (local development), ensuring zero disruption to existing workflows.

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

Authentication uses `DefaultAzureCredential` (Managed Identity in Azure, developer credentials locally). **No connection strings are stored in code or configuration files.**

### 3. `.azure/configuration-migration.json` — Deployment seed file

Created the seed file for the deployment agent to populate the Azure App Configuration store. All 10 non-secret leaf keys were flattened and emitted as colon-separated strings:

| Key | Notes |
|-----|-------|
| `AzureAd:Instance` | Entra ID endpoint |
| `AzureAd:Domain` | Tenant domain |
| `AzureAd:TenantId` | Directory identifier (not a secret) |
| `AzureAd:ClientId` | Application identifier (not a secret) |
| `AzureAd:CallbackPath` | OIDC redirect path |
| `AzureAd:SignedOutCallbackPath` | OIDC sign-out path |
| `AzureServiceBus:FullyQualifiedNamespace` | Service Bus hostname |
| `AzureServiceBus:QueueName` | Queue name |
| `Storage:ServiceUri` | Blob Storage account URI |
| `Storage:ContainerName` | Blob container name |

**Excluded** (as per migration contract):
- `ConnectionStrings:DefaultConnection` — connection string (Key Vault / Managed Identity)
- `Logging` / `AllowedHosts` — runtime host concerns, remain in `appsettings.json`

## Cutover Notes

Per the skill guidance, migrated keys are **kept** in `appsettings.json` as fallback values until the App Configuration store is seeded and the running application reads from it correctly. They can be removed in a follow-up commit once the store is operational.

## Success Criteria

| Criterion | Status |
|-----------|--------|
| Build passes (0 errors) | ✅ |
| Unit tests pass (53/53) | ✅ |
| No secrets in App Configuration seed | ✅ |
| `IConfiguration` / `IOptions<T>` shape preserved | ✅ |
| `.azure/configuration-migration.json` emitted | ✅ |
| `AZURE_APP_CONFIGURATION_ENDPOINT` env var — no hardcoding | ✅ |
| Managed Identity (`DefaultAzureCredential`) — no connection string | ✅ |
