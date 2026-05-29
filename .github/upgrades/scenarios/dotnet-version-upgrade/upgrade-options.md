# Upgrade Options — ContosoUniversity

Assessment: 1 project (ContosoUniversity.csproj, net48, old-style WAP), 83 files, 160 issues, System.Web/ASP.NET MVC 5 detected, 2 incompatible packages

## Strategy

### Upgrade Strategy
Single .NET Framework web project — only applicable strategy per framework migration rules for a single-project solution.

| Value | Description |
|-------|-------------|
| **All-at-Once** (selected) | All projects upgraded simultaneously in a single atomic operation. Mandatory for single-project solutions. |

## Project Structure

### Project Approach
ASP.NET MVC 5 web application with System.Web. In-place rewrite appropriate: 7 controllers (≤10), demo application, no continuous deployment constraints.

| Value | Description |
|-------|-------------|
| **In-place rewrite** (selected) | Replace the Framework web project entirely in one pass — directly migrates to ASP.NET Core MVC. |
| Side-by-side | Creates a new ASP.NET Core project alongside the existing Framework project for incremental migration. |

## Compatibility

### Unsupported Packages
2 packages require replacement for net10.0: Microsoft.AspNet.Web.Optimization (no .NET Core equivalent, replace with direct HTML tags) and Antlr (replace with Antlr4 4.6.6). Small count warrants inline resolution.

| Value | Description |
|-------|-------------|
| **Resolve Inline** (selected) | Research and resolve each incompatible package within the same task. Remove old references and add replacements or equivalent code. |
| Defer Resolution | Generate minimal stubs and create follow-up tasks for real replacements. |

### Unsupported API Handling
Binary/source incompatibilities detected (Api.0001: 63 instances, Api.0002: 29 instances) — primarily System.Web and System.Messaging APIs. Fix inline as default.

| Value | Description |
|-------|-------------|
| **Fix Inline** (selected) | Resolve every API change in the same task including complex ones. No deferred stubs. |
| Defer Complex Changes | Apply simple replacements inline; generate stubs for complex changes with resolution subtasks. |

### System.Web Adapters
ASP.NET MVC 5 / System.Web detected. In-place rewrite confirmed — direct migration to ASP.NET Core APIs without compatibility shims.

| Value | Description |
|-------|-------------|
| **Direct Migration to ASP.NET Core APIs** (selected) | All System.Web usage replaced with native ASP.NET Core equivalents. Clean result with no compatibility layer to remove later. |
| Use System.Web Adapters | Adds Microsoft.AspNetCore.SystemWebAdapters compatibility shims for incremental migration. |

## Modernization

### Assembly Binding Redirects
Binding redirect conflicts detected (Binding.0006: 6 instances, Binding.0007: 6 instances). These are auto-generated .NET Framework patterns not needed in .NET Core.

| Value | Description |
|-------|-------------|
| **Remove Binding Redirects** (selected) | Remove all assembly binding redirects from web.config. .NET Core does not use them. |
| Document and Review Before Removing | Generate a report of all redirects and their purposes before removal. |
