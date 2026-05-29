# 01-sdk-style: Convert ContosoUniversity to SDK-style project format

## Objective
Convert ContosoUniversity.csproj from old-style MSBuild format (ToolsVersion="15.0") to SDK-style format, migrating packages.config to PackageReference, while retaining net48 as the target framework. This must validate (build on net48) before the TFM upgrade task begins.

## Scope Inventory
- **Projects affected**: ContosoUniversity.csproj (1 project)
- **Current format**: Legacy old-style WAP (`<Project ToolsVersion="15.0">`)
- **packages.config**: Present — 45 packages to migrate to PackageReference
- **Assembly references**: ~25 framework references (System.Web, System.Messaging, System.Drawing, etc.) — most will be removed as SDK auto-includes them
- **ProjectTypeGuids**: `{349c5851...};{fae04ec0...}` (WAP + C#) — must be removed

## Conversion Approach
Use the `convert_project_to_sdk_style` tool which handles:
- Converts to `<Project Sdk="Microsoft.NET.Sdk.Web">`
- Migrates packages.config → PackageReference entries
- Removes redundant assembly reference imports
- Strips legacy MSBuild targets

## Issues from Assessment
- Project.0001: Project file needs to be converted to SDK-style (Mandatory)
- Project.0002: Project's target framework(s) needs to be changed (handled in task 02)

## Done-When Adjustment (In-Place Rewrite)
For in-place rewrite scenarios, net48 build validation after SDK conversion is not applicable: the conversion tool correctly removes incompatible packages (Microsoft.AspNet.Web.Optimization) which causes System.Web API references to fail. This is expected — the code will be fully migrated off System.Web in task 02. The SDK-style structural conversion is complete and correct.
