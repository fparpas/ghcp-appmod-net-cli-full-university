# 02.10-layout-and-shared-views: Migrate _Layout.cshtml and shared views from MVC 5 to ASP.NET Core

## Objective
Migrate shared views: _Layout.cshtml (remove bundle references, update to ASP.NET Core Tag Helpers), Views/Shared/Error.cshtml, _ViewStart.cshtml, _ViewImports.cshtml.

## Key Changes
- Remove @Styles.Render and @Scripts.Render — replace with direct <link> and <script> tags pointing to /css/ and /js/ files in wwwroot
- Replace @Html.ActionLink with asp-controller/asp-action tag helpers (or keep Html.ActionLink)
- Create _ViewImports.cshtml with @addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers
- Create Views/web.config equivalent (not needed in Core, but need _ViewImports)
- Remove Views/web.config if present
- Update Error.cshtml for ASP.NET Core error model

## Done when
Layout and shared views compile with 0 errors, no @Scripts.Render or @Styles.Render references remain.
