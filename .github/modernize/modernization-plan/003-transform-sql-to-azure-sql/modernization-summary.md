# Modernization Summary: 003-transform-sql-to-azure-sql

## Task
Migrate the SQL Server database connection to Azure SQL Database with Managed Identity (passwordless authentication).

## Changes Made

### Configuration Files

| File | Change |
|------|--------|
| `appsettings.json` | Updated `DefaultConnection` from LocalDB (`Integrated Security=True`) to Azure SQL Database with Managed Identity (`Authentication=Active Directory Default`) |
| `appsettings.Development.json` | **Unchanged** — kept LocalDB connection string for local development (overrides production config in `Development` environment) |
| `Web.config` | Removed hardcoded `DefaultConnection` connection string (legacy file, not used in .NET 10 apps) |

### New Connection String Format (appsettings.json)
```json
"DefaultConnection": "Server=tcp:<YOUR_SERVER>.database.windows.net;Database=ContosoUniversityNoAuthEFCore;Authentication=Active Directory Default;TrustServerCertificate=True"
```
> The `<YOUR_SERVER>` placeholder will be replaced with the actual Azure SQL Database server name when infrastructure is provisioned in task `009-infrastructure-bicep`.

### No Code Changes Required
- `Program.cs` — unchanged; already reads `DefaultConnection` from `IConfiguration` and passes to `UseSqlServer()`. The `Authentication=Active Directory Default` keyword in the connection string activates Managed Identity authentication automatically via `Microsoft.Data.SqlClient`.
- `Data/SchoolContext.cs` — unchanged; standard EF Core DbContext with constructor injection.
- `Data/SchoolContextFactory.cs` — unchanged; reads from `appsettings.json` for EF Core Migrations tooling.

### Packages (already present, verified)
| Package | Version | Purpose |
|---------|---------|---------|
| `Azure.Identity` | 1.14.0 | Provides `DefaultAzureCredential` for Managed Identity |
| `Microsoft.Data.SqlClient` | 7.0.1 | Modern SQL Client supporting Active Directory authentication |
| `Microsoft.EntityFrameworkCore.SqlServer` | 10.0.8 | EF Core SQL Server provider backed by Microsoft.Data.SqlClient |

### New Test Packages
| Package | Version | Purpose |
|---------|---------|---------|
| `Microsoft.EntityFrameworkCore.InMemory` | 10.0.8 | In-memory database provider for unit tests |

### New Unit Tests: `ContosoUniversity.Tests/Data/SchoolContextTests.cs`
16 new tests covering:
- All required `DbSet<T>` properties exist on `SchoolContext`
- Basic CRUD operations (add, retrieve, update, delete) using the in-memory database provider
- Azure SQL connection string format validation (contains `Authentication=Active Directory Default`, no passwords, targets `.database.windows.net`)
- Development vs. production connection string separation
- `SchoolContext` constructor instantiation and isolation between instances

## Test Results
- **Build**: ✅ 0 errors, 0 warnings
- **Tests**: ✅ 37/37 passed (21 existing + 16 new)

## Security Improvements
- **Removed**: Hardcoded `Integrated Security=True` with Windows credentials from production connection string
- **Removed**: Hardcoded LocalDB connection string from `Web.config`
- **Added**: Managed Identity (`Authentication=Active Directory Default`) — no passwords stored in configuration
- **Separation of concerns**: Production config uses Azure SQL; development config retains LocalDB for easy local development
