using Azure.Identity;
using Azure.Storage.Blobs;
using ContosoUniversity.Data;
using ContosoUniversity.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using System;
using System.IO;

var builder = WebApplication.CreateBuilder(args);

// Wire up Azure App Configuration as an additional configuration source.
// The endpoint is injected via the AZURE_APP_CONFIGURATION_ENDPOINT environment variable
// by the deployment agent. When the variable is absent (e.g. local dev), the app falls back
// to the values in appsettings.json so the host continues to start normally.
var appConfigEndpoint = Environment.GetEnvironmentVariable("AZURE_APP_CONFIGURATION_ENDPOINT");
if (!string.IsNullOrEmpty(appConfigEndpoint))
{
    builder.Configuration.AddAzureAppConfiguration(options =>
    {
        options.Connect(new Uri(appConfigEndpoint), new DefaultAzureCredential());
    });
}

// Add Microsoft Entra ID (formerly Azure AD) authentication using OIDC
builder.Services.AddMicrosoftIdentityWebAppAuthentication(builder.Configuration);

// Add services to the container.
builder.Services.AddControllersWithViews()
    .AddMicrosoftIdentityUI();

builder.Services.AddRazorPages();

// Register EF Core DbContext
builder.Services.AddDbContext<SchoolContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register NotificationService as singleton — uses Azure Service Bus with Managed Identity
builder.Services.AddSingleton<NotificationService>();

// Register BlobServiceClient as singleton, authenticated via Managed Identity (DefaultAzureCredential).
// Per Azure SDK guidance, BlobServiceClient is thread-safe and designed for singleton lifetime.
builder.Services.AddSingleton(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var serviceUri = config["Storage:ServiceUri"]
        ?? throw new InvalidOperationException(
            "Storage:ServiceUri is not configured. " +
            "Set it in appsettings.json or as an environment variable.");
    return new BlobServiceClient(new Uri(serviceUri), new DefaultAzureCredential());
});

// Register AzureBlobStorageService as singleton — it holds a BlobContainerClient
// (Azure SDK client) and must not be Scoped or Transient.
builder.Services.AddSingleton<IBlobStorageService, AzureBlobStorageService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

// Serve static files from wwwroot (default) if it exists
app.UseStaticFiles();

// Serve static files from project-root Content/ folder
var contentPath = Path.Combine(app.Environment.ContentRootPath, "Content");
if (Directory.Exists(contentPath))
{
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(contentPath),
        RequestPath = "/Content"
    });
}

// Serve static files from project-root Scripts/ folder
var scriptsPath = Path.Combine(app.Environment.ContentRootPath, "Scripts");
if (Directory.Exists(scriptsPath))
{
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(scriptsPath),
        RequestPath = "/Scripts"
    });
}

// NOTE: Teaching material images are now served from Azure Blob Storage.
// The local Uploads/ static-file middleware has been removed as part of the
// migration from local file system storage to Azure Blob Storage.

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

// Initialize database
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<SchoolContext>();
        DbInitializer.Initialize(context);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

app.Run();
