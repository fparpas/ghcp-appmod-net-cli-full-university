# Infrastructure Compliance Report

## ContosoUniversity — Bicep IaC Compliance

---

## 1. Managed Identity (Passwordless Auth)

**Status: ✅ Compliant**

- The Azure App Service Web App has a **system-assigned managed identity** enabled (`identity: { type: 'SystemAssigned' }`).
- All service-to-service connections use **RBAC role assignments** instead of connection strings with secrets:
  - Service Bus: Azure Service Bus Data Sender + Data Receiver roles
  - Storage: Storage Blob Data Contributor role
  - App Configuration: App Configuration Data Reader role
  - Azure SQL: Active Directory Default authentication (post-deploy SQL script required)
- No service account passwords or connection string credentials are stored in Bicep templates.

---

## 2. Secret Management

**Status: ✅ Compliant**

- SQL Server administrator password is declared as `@secure()` parameter — it is **never** logged, echoed to outputs, or stored in state files.
- `parameters.json` ships with an empty `sqlAdminPassword` value — callers must supply it at deploy time.
- No secrets appear in Bicep `outputs`.
- `infra-config.md` contains **no credentials** — only resource names and endpoints.

---

## 3. Azure Naming Conventions

**Status: ✅ Compliant**

Resource names follow [Azure naming conventions](https://docs.microsoft.com/azure/cloud-adoption-framework/ready/azure-best-practices/resource-naming):

| Resource | Name Pattern | Compliant |
|----------|-------------|-----------|
| App Service Plan | `asp-{app}-{env}` | ✅ |
| Web App | `app-{app}-{env}-{suffix}` | ✅ |
| SQL Server | `sql-{app}-{env}-{suffix}` | ✅ |
| SQL Database | `sqldb-{app}-{env}` | ✅ |
| Service Bus Namespace | `sb-{app}-{env}-{suffix}` | ✅ |
| Storage Account | `st{app}{env}{suffix}` (lowercase, no hyphens, ≤24 chars) | ✅ |
| App Configuration | `appcs-{app}-{env}-{suffix}` | ✅ |

- `uniqueStr = substring(uniqueString(resourceGroup().id), 0, 6)` ensures global uniqueness for resources with globally unique name requirements.

---

## 4. Parameterized Environment Values

**Status: ✅ Compliant**

- `environmentName`, `location`, `appName`, SQL credentials, and AAD admin details are all parameters with sensible defaults.
- `parameters.json` provides dev-environment defaults; production overrides are supplied at deploy time.
- No hard-coded subscription IDs, tenant IDs, or resource IDs in templates.

---

## 5. Resource Tagging Strategy

**Status: ✅ Compliant**

All resources receive the following tags propagated from `main.bicep`:

| Tag | Value |
|-----|-------|
| `environment` | Value of `environmentName` parameter (e.g., `dev`) |
| `application` | Value of `appName` parameter (e.g., `contosouniv`) |
| `managedBy` | `bicep` |

Tags enable cost allocation, environment filtering, and compliance reporting in Azure Policy.

---

## 6. Network Security

**Status: ⚠️ Basic — Production Hardening Recommended**

Current configuration (suitable for dev/test):
- All resources use `publicNetworkAccess: 'Enabled'` for simplicity.
- SQL Server has a firewall rule `AllowAllAzureIPs` (0.0.0.0 → 0.0.0.0) to allow Azure services.
- Storage Account allows access from Azure services via `networkAcls.bypass: 'AzureServices'`.
- HTTPS-only enforced on App Service (`httpsOnly: true`).
- TLS 1.2 minimum enforced on App Service, SQL Server, Storage Account, and Service Bus.

**Recommended for Production:**
- Enable **VNet Integration** for the App Service.
- Add **Private Endpoints** for SQL, Storage, Service Bus, and App Configuration.
- Set `publicNetworkAccess: 'Disabled'` on all services.
- Configure **Azure Firewall** or **NSG** rules.
- Enable **Microsoft Defender for Cloud** on SQL and Storage.

---

## 7. SKU Justification

| Resource | SKU | Justification |
|----------|-----|---------------|
| App Service Plan | B2 (Basic) | Low-cost plan suitable for dev/test; upgrade to P2v3 for production to gain autoscale and premium networking |
| SQL Database | GP_S_Gen5_2 (Serverless) | Serverless tier auto-pauses after 60 min of inactivity — ideal for dev workloads; auto-scales 0.5–2 vCores; upgrade to provisioned for production SLAs |
| Service Bus | Standard | Supports queues and topics; sufficient for the `notifications` queue pattern; upgrade to Premium for VNet integration and larger message sizes |
| Storage Account | Standard_LRS | Locally redundant — adequate for dev; use GRS or GZRS for production data durability requirements |
| App Configuration | Free | 10 MB storage, 1,000 requests/day — sufficient for configuration seeding; upgrade to Standard for geo-replication and higher throughput |

---

## 8. High Availability & Resilience

**Status: ⚠️ Dev Configuration**

- SQL Database serverless with `autoPauseDelay: 60` minutes — not suitable for always-on production.
- App Service `alwaysOn: true` prevents cold starts on the B2 plan.
- No zone redundancy configured (not supported on Basic App Service Plan or Free App Configuration).

**Production Recommendations:**
- Switch SQL to provisioned tier with zone redundancy.
- Use P2v3 App Service Plan with availability zones.
- Enable geo-replication for App Configuration (Standard tier).
- Enable Storage Account GRS and soft-delete retention.
