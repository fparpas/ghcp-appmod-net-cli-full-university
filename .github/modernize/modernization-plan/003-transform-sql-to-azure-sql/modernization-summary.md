# Task 003 – Migrate SQL Server LocalDB to Azure SQL Database (Managed Identity)

## Overview

Replaced the hardcoded LocalDB connection string (Windows Integrated Security) with an Azure SQL Database connection string using Managed Identity (`Authentication=Active Directory Default`). The EF Core `SchoolContextFactory` was upgraded to implement `IDesignTimeDbContextFactory<SchoolContext>` so that EF Core migrations work against Azure SQL Database.

---

## Changes Made

### 1. `appsettings.json`
- **Before:** `Data Source=(LocalDb)\MSSQLLocalDB;Initial Catalog=ContosoUniversityNoAuthEFCore;Integrated Security=True;MultipleActiveResultSets=True`
- **After:** `Server=tcp:<YOUR_SERVER_NAME>.database.windows.net;Database=ContosoUniversity;Authentication=Active Directory Default;`
- Removed `Integrated Security=True` (Windows-only, not supported on Azure).
- Replaced `(LocalDb)\MSSQLLocalDB` with Azure SQL Database FQDN endpoint.
- Uses `Authentication=Active Directory Default` (Managed Identity / DefaultAzureCredential).

### 2. `Web.config`
- Same connection string update as `appsettings.json`.
- Removed `Integrated Security=True` and LocalDB `Data Source`.

### 3. `Data/SchoolContextFactory.cs`
- Converted from a `static` utility class to an `IDesignTimeDbContextFactory<SchoolContext>` implementation (required by `dotnet ef migrations`).
- `CreateDbContext(string[] args)` reads the connection string from `appsettings.json` / environment variables via `ConfigurationBuilder`, with null-guard that throws a descriptive `InvalidOperationException` when the connection string is missing.
- Retained the backward-compatible `static Create(string connectionString)` method for programmatic use.

### 4. `Tests/ContosoUniversity.Tests.csproj`
- Added `Microsoft.EntityFrameworkCore.InMemory` (v10.0.8) to support unit testing of `SchoolContext` without a live database.

### 5. `Tests/Data/SchoolContextTests.cs` *(new file)*
- 11 new unit tests covering:
  - Connection string format validation (no LocalDB, no Integrated Security, contains Active Directory Default, targets `.database.windows.net`).
  - `SchoolContext` CRUD operations (Students, Courses, Departments, Notifications) using the InMemory provider.
  - `DbSet` non-null assertions on all entities.
  - `SchoolContextFactory.Create()` factory method returns a valid context.

---

## Packages Used

| Package | Version | Purpose |
|---------|---------|---------|
| `Azure.Identity` | 1.14.2 | Already present – provides `DefaultAzureCredential` used by `Authentication=Active Directory Default` |
| `Microsoft.EntityFrameworkCore.SqlServer` | 10.0.8 | Already present – uses `Microsoft.Data.SqlClient` internally, which supports AAD authentication |
| `Microsoft.EntityFrameworkCore.InMemory` | 10.0.8 | Added to test project only – enables in-memory DB for unit testing |

---

## Migration Pattern

```
Authentication=Active Directory Default
```

This keyword in the connection string causes `Microsoft.Data.SqlClient` (used internally by EF Core's SQL Server provider) to acquire a token via `DefaultAzureCredential`. At runtime on Azure App Service this resolves to the Managed Identity. Locally it falls back to developer credentials (Azure CLI / VS login).

---

## Deployment Note

Replace `<YOUR_SERVER_NAME>` in `appsettings.json` with the actual Azure SQL logical server name (e.g., `contoso-university-sql`) before deploying. The infrastructure task (008-infrastructure-bicep) will provision this server and grant the App Service Managed Identity the `db_datareader` / `db_datawriter` roles.

---

## Build & Test Results

- **Build:** ✅ 0 errors, 0 warnings
- **Unit Tests:** ✅ 29/29 passed (18 existing + 11 new)
