# ContosoUniversity — Azure Infrastructure

Bicep-based IaC for the ContosoUniversity .NET 10 application deployed to **Azure App Service** in the **SwedenCentral** region.

---

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────────────┐
│  Resource Group: app-mod-cli-full-uni  (swedencentral)              │
│                                                                     │
│  ┌─────────────────────────────────────────────────────────────┐    │
│  │  App Service Plan (Linux B2)                                │    │
│  │  └── App Service (.NET 10)  ──[System-Assigned MI]──────┐  │    │
│  └─────────────────────────────────────────────────────────│──┘    │
│                                                            │         │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────▼──────┐  │
│  │  Azure SQL DB   │  │  Service Bus     │  │  RBAC Assignments  │  │
│  │  (Standard S1)  │  │  (Standard)      │  │  - SB Data Owner   │  │
│  └─────────────────┘  │  queue: notifs   │  │  - Blob Contributor│  │
│                        └─────────────────┘  │  - AppConfig Reader│  │
│  ┌─────────────────┐  ┌─────────────────┐   └────────────────────┘  │
│  │  Storage Acct   │  │  App Config     │                            │
│  │  (Standard LRS) │  │  (Standard)     │                            │
│  │  blob: teach.   │  │                 │                            │
│  └─────────────────┘  └─────────────────┘                            │
└─────────────────────────────────────────────────────────────────────┘
```

---

## Resources

| Resource | Type | SKU / Tier |
|----------|------|-----------|
| App Service Plan | `Microsoft.Web/serverfarms` | B2 Basic (Linux) |
| App Service | `Microsoft.Web/sites` | .NET 10 |
| Azure SQL Server | `Microsoft.Sql/servers` | — |
| Azure SQL Database | `Microsoft.Sql/servers/databases` | Standard S1 |
| Service Bus Namespace | `Microsoft.ServiceBus/namespaces` | Standard |
| Service Bus Queue | `Microsoft.ServiceBus/namespaces/queues` | `contoso-university-notifications` |
| Storage Account | `Microsoft.Storage/storageAccounts` | Standard LRS (StorageV2) |
| Blob Container | `Microsoft.Storage/storageAccounts/blobServices/containers` | `teaching-materials` |
| App Configuration | `Microsoft.AppConfiguration/configurationStores` | Standard |

---

## Managed Identity & RBAC

The App Service uses a **System-Assigned Managed Identity** with the following RBAC roles:

| Azure Built-in Role | Scope | Purpose |
|--------------------|-------|---------|
| Azure Service Bus Data Owner | Service Bus Namespace | Send & receive messages |
| Storage Blob Data Contributor | Storage Account | Read/write teaching material blobs |
| App Configuration Data Reader | App Configuration | Read application settings |

> **SQL Database**: RBAC alone does not grant SQL access. A **contained database user** must be created manually after deployment (see [Post-Deployment Steps](#post-deployment-steps)).

---

## File Structure

```
infra/
├── main.bicep                # Entry point — orchestrates all modules
├── parameters.json           # Non-secret deployment parameters
├── modules/
│   ├── appservice.bicep      # App Service Plan + App Service
│   ├── appsettings.bicep     # App Service application settings
│   ├── sql.bicep             # Azure SQL Server + Database
│   ├── servicebus.bicep      # Service Bus namespace + queue
│   ├── storage.bicep         # Storage account + blob container
│   ├── appconfig.bicep       # App Configuration store
│   └── roleassignments.bicep # Managed Identity RBAC assignments
├── deploy.ps1                # Windows PowerShell deployment script
├── deploy.sh                 # Linux/macOS bash deployment script
├── README.md                 # This file
├── infra-config.md           # Machine-readable provisioned resource info
└── compliance.md             # WAF / rules compliance report
```

---

## Prerequisites

- **Azure CLI** ≥ 2.50 (`az --version`)
- **Bicep CLI** ≥ 0.22 (auto-installed by Azure CLI: `az bicep install`)
- Azure subscription with **Contributor** + **User Access Administrator** rights on the resource group (needed for RBAC role assignments)

---

## Deployment

### Windows (PowerShell)

```powershell
cd infra

# Basic deployment (prompts for SQL password)
.\deploy.ps1

# Full example
.\deploy.ps1 `
  -ResourceGroup app-mod-cli-full-uni `
  -Location swedencentral `
  -SqlAdminPassword "MySecurePass123!" `
  -SqlEntraAdminObjectId "<your-entra-object-id>" `
  -SqlEntraAdminLogin "admin@contoso.com"
```

### Linux / macOS (Bash)

```bash
cd infra
chmod +x deploy.sh

# Basic deployment (prompts for SQL password)
./deploy.sh

# Full example
./deploy.sh \
  -g app-mod-cli-full-uni \
  -l swedencentral \
  -p "MySecurePass123!" \
  -o "<your-entra-object-id>" \
  -n "admin@contoso.com"
```

### Using environment variable for SQL password

```powershell
$env:SQLPASSWORD = "MySecurePass123!"
.\deploy.ps1
```

```bash
export SQLPASSWORD="MySecurePass123!"
./deploy.sh
```

---

## Parameters

| Parameter | Description | Default |
|-----------|-------------|---------|
| `appName` | Resource name prefix | `contoso-uni` |
| `environmentName` | Environment tag | `prod` |
| `location` | Azure region | `swedencentral` |
| `sqlAdminLogin` | SQL Server admin login | `sqladmin` |
| `sqlAdminPassword` | SQL Server admin password (**secure**) | *(prompted)* |
| `sqlEntraAdminObjectId` | Azure AD admin object ID for SQL | *(auto-detected)* |
| `sqlEntraAdminLogin` | Azure AD admin display name | *(auto-detected)* |

---

## Post-Deployment Steps

### 1. SQL Managed Identity access

After the App Service is created and has a Managed Identity principal ID, connect to the SQL database using your Entra admin credentials and run:

```sql
CREATE USER [<app-service-name>] FROM EXTERNAL PROVIDER;
ALTER ROLE db_datareader ADD MEMBER [<app-service-name>];
ALTER ROLE db_datawriter ADD MEMBER [<app-service-name>];
ALTER ROLE db_ddladmin   ADD MEMBER [<app-service-name>];
```

Replace `<app-service-name>` with the App Service name shown in the deployment output.

### 2. App Configuration seed values

The deployment script automatically imports `.azure/configuration-migration.json` into App Configuration with the actual provisioned resource endpoints replacing placeholders.

### 3. Entra ID app registration

Update the Entra ID app registration (created during Task 5) with the actual App Service URL as a redirect URI:
```
https://<app-service-name>.<unique>.azurewebsites.net/signin-oidc
```

---

## Resource Naming Convention

Resources follow Azure naming best practices with a unique suffix derived from the resource group ID:

| Resource | Pattern | Example |
|----------|---------|---------|
| App Service Plan | `asp-{appName}-{env}` | `asp-contoso-uni-prod` |
| App Service | `app-{appName}-{suffix8}` | `app-contoso-uni-a1b2c3d4` |
| SQL Server | `sql-{appName}-{suffix8}` | `sql-contoso-uni-a1b2c3d4` |
| Service Bus | `sb-{appName}-{suffix8}` | `sb-contoso-uni-a1b2c3d4` |
| Storage Account | `st{appNameNoHyphen}{suffix8}` | `stcontosouni a1b2c3d4` |
| App Configuration | `appcs-{appName}-{suffix8}` | `appcs-contoso-uni-a1b2c3d4` |
