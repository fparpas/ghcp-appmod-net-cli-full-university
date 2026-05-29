# .NET Version Upgrade

## Preferences
- **Flow Mode**: Automatic
- **Target Framework**: net10.0

## Source Control
- **Source Branch**: modernize-execute-plan
- **Working Branch**: dotnet-version-upgrade-net10
- **Commit Strategy**: After Each Task
- **Branch Sync**: Auto (Merge)

## Upgrade Options
**Source**: .github/upgrades/scenarios/dotnet-version-upgrade/upgrade-options.md

### Strategy
- Upgrade Strategy: All-at-Once

### Project Structure
- Project Approach: In-place rewrite (web)

### Compatibility
- Unsupported Packages: Resolve Inline (2 incompatible packages: Microsoft.AspNet.Web.Optimization, Antlr)
- Unsupported API Handling: Fix Inline
- System.Web Adapters: Direct Migration to ASP.NET Core APIs

### Modernization
- Assembly Binding Redirects: Remove

## Strategy
**Selected**: All-at-Once
**Rationale**: Single project on net48. Framework migration rules mandate All-at-Once for single-project solutions.

### Execution Constraints
- Single atomic upgrade — all changes applied in one pass across SDK conversion then TFM upgrade tasks
- SDK-style conversion task must complete and validate (builds on net48) before the TFM upgrade task begins
- Full solution build validation after each task completes
- In-place rewrite: no scaffold/migrate task injection, no System.Web adapter shims
- Direct ASP.NET Core migration: all System.Web references replaced inline, no compatibility layer
