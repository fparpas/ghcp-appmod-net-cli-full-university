# Infrastructure Compliance Report

## Overview

This report documents compliance with Azure Well-Architected Framework (WAF) pillars and IaC best practices for the ContosoUniversity infrastructure.

---

## Azure Well-Architected Framework Compliance

### Reliability

| Rule | Status | Implementation |
|------|--------|----------------|
| Use managed services | ✅ | Azure SQL Database, Service Bus, Blob Storage, App Configuration — all PaaS |
| Enable soft delete for storage | ✅ | `deleteRetentionPolicy.days = 7` on blob service |
| Dead-letter queue for Service Bus | ✅ | `deadLetteringOnMessageExpiration = true` on queue |
| Health check endpoint | ✅ | `healthCheckPath: '/health'` on App Service |
| Always-on App Service | ✅ | `alwaysOn: true` on App Service |

### Security

| Rule | Status | Implementation |
|------|--------|----------------|
| Managed Identity for service authentication | ✅ | System-Assigned MI on App Service; no connection strings with credentials |
| RBAC least-privilege | ✅ | Scoped role assignments (Service Bus Data Owner, Storage Blob Data Contributor, App Config Data Reader) |
| HTTPS only | ✅ | `httpsOnly: true` on App Service |
| Minimum TLS 1.2 | ✅ | App Service (`minTlsVersion: '1.2'`), SQL Server (`minimalTlsVersion: '1.2'`), Storage (`minimumTlsVersion: 'TLS1_2'`) |
| FTP disabled | ✅ | `ftpsState: 'Disabled'` on App Service |
| No public blob access | ✅ | `allowBlobPublicAccess: false` on Storage Account |
| Private blob containers | ✅ | `publicAccess: 'None'` on blob container |
| Secrets not in source control | ✅ | `sqlAdminPassword` is `@secure()` parameter; never stored in parameters.json |
| SQL firewall: allow Azure services only | ✅ | Firewall rule `AllowAllWindowsAzureIps` (0.0.0.0-0.0.0.0) |

### Cost Optimization

| Rule | Status | Implementation |
|------|--------|----------------|
| Right-sized SKUs for workload | ✅ | App Service B2 (dev/test appropriate), SQL S1 Standard, Storage Standard_LRS |
| Use PaaS over IaaS | ✅ | All resources are managed PaaS services |

### Operational Excellence

| Rule | Status | Implementation |
|------|--------|----------------|
| Consistent resource tagging | ✅ | All resources tagged with `application`, `environment`, `managedBy`, `project` |
| Modular IaC | ✅ | One Bicep module per resource type |
| Parameterized deployments | ✅ | All environment-specific values are parameters |
| Unique resource names | ✅ | `uniqueString(resourceGroup().id)` suffix for globally-unique resources |
| Deployment scripts for CI/CD | ✅ | `deploy.ps1` (Windows) and `deploy.sh` (Linux/macOS) |
| Azure CLI deployment (not azd) | ✅ | `az deployment group create` used throughout |

### Performance Efficiency

| Rule | Status | Implementation |
|------|--------|----------------|
| HTTP/2 enabled | ✅ | `http20Enabled: true` on App Service |
| Hot access tier for storage | ✅ | `accessTier: 'Hot'` on Storage Account |
| Batched Service Bus operations | ✅ | `enableBatchedOperations: true` on queue |

---

## Bicep IaC Best Practices

| Practice | Status | Notes |
|----------|--------|-------|
| No hardcoded resource IDs | ✅ | All references use `existing` or module outputs |
| Outputs for downstream consumption | ✅ | All modules expose relevant outputs |
| `@description` decorators on all params | ✅ | Every parameter has a description |
| `@secure()` on sensitive parameters | ✅ | `sqlAdminPassword` uses `@secure()` |
| Conditional resources (`if`) | ✅ | SQL Entra admin resource is conditional on `sqlEntraAdminObjectId` |
| `dependsOn` for resource ordering | ✅ | Role assignments and app settings depend on all resource modules |
| `subscriptionResourceId` for built-in roles | ✅ | RBAC uses `subscriptionResourceId('Microsoft.Authorization/roleDefinitions', ...)` |

---

## Limitations & Known Gaps

| Item | Notes |
|------|-------|
| SQL contained database user | Cannot be created via Bicep; must be done via T-SQL after deployment (documented in README.md) |
| Entra ID app registration | Created outside Bicep; redirect URIs must be updated manually after App Service provisioning |
| Production sizing | Current SKUs (B2 App Service Plan, S1 SQL) are appropriate for dev/test. For production, upgrade to P2v3 + S3 or Premium SQL |
| Private endpoints | Not configured; public network access is enabled. For production, add VNet integration and private endpoints |
| Diagnostic settings | Azure Monitor / Log Analytics integration not configured in this phase |
