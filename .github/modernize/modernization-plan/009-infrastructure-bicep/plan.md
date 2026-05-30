# Task 009 — Provision Azure Infrastructure (Bicep)

## Objective

Generate Bicep IaC files and provision all required Azure resources in the **SwedenCentral** region under resource group **app-mod-cli-full-uni** for the ContosoUniversity .NET 10 application.

## Resources to Provision

| Resource | Type | SKU / Tier | Notes |
|----------|------|-----------|-------|
| App Service Plan | `Microsoft.Web/serverfarms` | B2 (Basic) | Linux, reserved |
| App Service | `Microsoft.Web/sites` | — | .NET 10 (DOTNET\|10.0), System-Assigned MI |
| Azure SQL Server | `Microsoft.Sql/servers` | — | TLS 1.2 min, Azure services firewall rule |
| Azure SQL Database | `Microsoft.Sql/servers/databases` | S1 Standard | ContosoUniversityDB |
| Service Bus Namespace | `Microsoft.ServiceBus/namespaces` | Standard | |
| Service Bus Queue | `Microsoft.ServiceBus/namespaces/queues` | — | contoso-university-notifications |
| Storage Account | `Microsoft.Storage/storageAccounts` | Standard_LRS | StorageV2, HTTPS-only |
| Blob Container | `Microsoft.Storage/storageAccounts/blobServices/containers` | — | teaching-materials, private |
| App Configuration | `Microsoft.AppConfiguration/configurationStores` | Standard | Managed Identity auth |

## RBAC Assignments (App Service → Resources)

| Role | Target Resource |
|------|----------------|
| Azure Service Bus Data Owner | Service Bus Namespace |
| Storage Blob Data Contributor | Storage Account |
| App Configuration Data Reader | App Configuration Store |

## File Structure

```
infra/
├── main.bicep                # Orchestrates all modules
├── parameters.json           # Environment-specific parameters
├── modules/
│   ├── appservice.bicep      # App Service Plan + App Service
│   ├── appsettings.bicep     # App Service application settings
│   ├── sql.bicep             # SQL Server + Database
│   ├── servicebus.bicep      # Service Bus namespace + queue
│   ├── storage.bicep         # Storage account + blob container
│   ├── appconfig.bicep       # App Configuration store
│   └── roleassignments.bicep # RBAC role assignments
├── deploy.ps1                # Windows deployment script
├── deploy.sh                 # Linux/macOS deployment script
├── README.md                 # Infrastructure documentation
├── infra-config.md           # Provisioned resource config (post-deployment)
└── compliance.md             # Rules compliance report
```

## Execution Steps

1. **Prerequisites**: Ensure `az cli` is installed and authenticated (`az login`).
2. **Create resource group** (if not exists):
   ```powershell
   az group create --name app-mod-cli-full-uni --location swedencentral
   ```
3. **Deploy infrastructure**:
   ```powershell
   cd infra
   .\deploy.ps1 -ResourceGroup app-mod-cli-full-uni -Location swedencentral
   ```
4. **Post-deployment**: The script will:
   - Capture all deployment outputs
   - Populate Azure App Configuration with values from `.azure/configuration-migration.json`
   - Configure App Service application settings
   - Print instructions for adding the SQL contained database user
5. **SQL Managed Identity setup** (manual step if sqlcmd not available):
   ```sql
   CREATE USER [<app-service-name>] FROM EXTERNAL PROVIDER;
   ALTER ROLE db_datareader ADD MEMBER [<app-service-name>];
   ALTER ROLE db_datawriter ADD MEMBER [<app-service-name>];
   ALTER ROLE db_ddladmin ADD MEMBER [<app-service-name>];
   ```

## Notes

- All secrets are passed as secure parameters (not stored in source control).
- SQL admin password is prompted at deploy time or passed via `SQLPASSWORD` env variable.
- The `infra-config.md` file is generated after successful provisioning.
