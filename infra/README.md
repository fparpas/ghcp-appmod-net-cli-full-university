# ContosoUniversity — Azure Infrastructure

This directory contains Bicep templates to provision all Azure resources required to run **ContosoUniversity** — an ASP.NET Core MVC application on .NET 10 deployed to Azure App Service.

---

## Architecture

```
┌─────────────────────────────────────────────────────────┐
│                   Azure Resource Group                   │
│                                                         │
│  ┌──────────────────────────────────────────────────┐   │
│  │           Azure App Service (Linux)              │   │
│  │   Web App: app-contosouniv-dev-<suffix>          │   │
│  │   Runtime: .NET 10  │  MI: System-Assigned       │   │
│  └───────────┬──────────────────────────────────────┘   │
│              │  Managed Identity (RBAC)                  │
│    ┌─────────┼──────────────────────────────────┐        │
│    │         │                                  │        │
│    ▼         ▼                                  ▼        │
│ ┌──────┐ ┌────────┐ ┌───────────┐  ┌──────────────────┐ │
│ │Azure │ │Service │ │ Storage   │  │ App              │ │
│ │SQL   │ │Bus NS  │ │ Account   │  │ Configuration    │ │
│ │      │ │+ Queue │ │+ Container│  │                  │ │
│ └──────┘ └────────┘ └───────────┘  └──────────────────┘ │
└─────────────────────────────────────────────────────────┘
```

---

## Resources Deployed

| Resource | Name Pattern | SKU/Tier |
|----------|-------------|----------|
| App Service Plan | `asp-contosouniv-{env}` | B2 Linux |
| Web App | `app-contosouniv-{env}-{suffix}` | .NET 10 |
| SQL Server | `sql-contosouniv-{env}-{suffix}` | — |
| SQL Database | `sqldb-contosouniv-{env}` | GeneralPurpose Serverless GP_S_Gen5_2 |
| Service Bus Namespace | `sb-contosouniv-{env}-{suffix}` | Standard |
| Service Bus Queue | `notifications` | — |
| Storage Account | `stcontosouniv{env}{suffix}` | Standard_LRS |
| Blob Container | `teaching-materials` | Private |
| App Configuration | `appcs-contosouniv-{env}-{suffix}` | Free |

---

## Prerequisites

- [Azure CLI](https://docs.microsoft.com/cli/azure/install-azure-cli) ≥ 2.50
- Bicep CLI: `az bicep install`
- An Azure subscription with Owner or Contributor + User Access Administrator permissions
- An Azure AD user/group to serve as SQL Server AAD administrator

---

## Deployment Steps

### Windows (PowerShell)

```powershell
cd infra

.\deploy.ps1 `
  -ResourceGroupName "rg-contosouniv-dev" `
  -SubscriptionId    "<your-subscription-id>" `
  -SqlAdminPassword  "<strong-password>" `
  -AadAdminLogin     "<aad-admin-upn>" `
  -AadAdminObjectId  "<aad-admin-object-id>" `
  -AadTenantId       "<tenant-id>"
```

### Linux / macOS (Bash)

```bash
chmod +x infra/deploy.sh

./infra/deploy.sh \
  -g rg-contosouniv-dev \
  -s <your-subscription-id> \
  -p <strong-password> \
  -a <aad-admin-upn> \
  -o <aad-admin-object-id> \
  -t <tenant-id>
```

### Manual (az CLI)

```bash
az group create --name rg-contosouniv-dev --location eastus

az deployment group create \
  --resource-group rg-contosouniv-dev \
  --template-file infra/main.bicep \
  --parameters infra/parameters.json \
  --parameters sqlAdminPassword=<pwd> aadAdminLogin=<login> aadAdminObjectId=<oid> aadTenantId=<tid>
```

---

## Post-Deployment Configuration

### 1. SQL Managed Identity Setup (Required)

The Web App uses **Managed Identity** (Active Directory Default) to authenticate to Azure SQL. After deployment, connect to the SQL Database as an AAD admin and run:

```sql
CREATE USER [app-contosouniv-dev-<suffix>] FROM EXTERNAL PROVIDER;
ALTER ROLE db_owner ADD MEMBER [app-contosouniv-dev-<suffix>];
```

Replace `<suffix>` with the 6-character suffix shown in the deployment outputs.

You can run this via:
- **Azure Portal** → SQL Database → Query Editor (use AAD authentication)
- **sqlcmd** with `-G` flag for AAD authentication

### 2. Seed Azure App Configuration

If using Azure App Configuration for non-secret settings:

```bash
az appconfig kv import \
  --name appcs-contosouniv-dev-<suffix> \
  --source file \
  --path .azure/configuration-migration.json \
  --format json
```

### 3. Update appsettings.json

After provisioning, update `appsettings.json` (or App Service configuration) with the actual resource names/endpoints from the deployment outputs.

---

## Environment Parameters

| Parameter | Default | Description |
|-----------|---------|-------------|
| `environmentName` | `dev` | Environment suffix (`dev`, `staging`, `prod`) |
| `location` | `eastus` | Azure region |
| `appName` | `contosouniv` | Short app name used in resource names |
| `sqlAdminLogin` | `sqladmin` | SQL Server SQL authentication admin |
| `sqlAdminPassword` | *(required)* | SQL Server admin password |
| `aadAdminLogin` | *(required)* | Azure AD admin UPN for SQL |
| `aadAdminObjectId` | *(required)* | Object ID of the AAD SQL admin |
| `aadTenantId` | *(required)* | Azure AD tenant ID |

---

## RBAC Role Assignments

The Web App's **system-assigned managed identity** is granted:

| Role | Resource | Role ID |
|------|----------|---------|
| Azure Service Bus Data Sender | Service Bus Namespace | `69a216fc-b8fb-44d8-bc22-1f3c2cd27a39` |
| Azure Service Bus Data Receiver | Service Bus Namespace | `4f6d3b9f-4099-4b3c-ad6b-d7e3f2c3e8b2` |
| Storage Blob Data Contributor | Storage Account | `ba92f5b4-2d11-453d-a403-e96b0029c9fe` |
| App Configuration Data Reader | App Configuration | `516239f1-63e1-4d78-a4de-a74fb236a071` |

---

## File Structure

```
infra/
├── main.bicep                      # Root template — orchestrates all modules
├── parameters.json                 # Dev environment defaults
├── modules/
│   ├── appservice.bicep            # App Service Plan + Web App
│   ├── sql.bicep                   # SQL Server + Database + firewall
│   ├── servicebus.bicep            # Service Bus Namespace + Queue
│   ├── storage.bicep               # Storage Account + Blob Container
│   ├── appconfiguration.bicep      # App Configuration store
│   └── roleassignments.bicep       # RBAC role assignments
├── deploy.sh                       # Linux/macOS deployment script
├── deploy.ps1                      # Windows deployment script
├── README.md                       # This file
├── infra-config.md                 # Provisioned resource reference
└── compliance.md                   # Rules compliance report
```
