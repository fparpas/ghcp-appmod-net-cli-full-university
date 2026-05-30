using Azure.Identity;
using Azure.Storage.Blobs;
using ContosoUniversity.Data;
using ContosoUniversity.Services;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;

// Register the Azure AD authentication providers for Microsoft.Data.SqlClient v7+
// This re-enables the 'Authentication=Active Directory Default' connection string keyword
// which was moved to the Microsoft.Data.SqlClient.Extensions.Azure package in v7.
SqlAuthenticationProvider.SetProvider(
    SqlAuthenticationMethod.ActiveDirectoryDefault,
    new ActiveDirectoryAuthenticationProvider());

var builder = WebApplication.CreateBuilder(args);

// Add Azure App Configuration as a configuration source so all non-secret application
// settings are loaded from the centralised store via Managed Identity.
// The endpoint is injected by the deployment agent via AZURE_APP_CONFIGURATION_ENDPOINT.
var appConfigEndpoint = Environment.GetEnvironmentVariable("AZURE_APP_CONFIGURATION_ENDPOINT");
if (!string.IsNullOrEmpty(appConfigEndpoint))
{
    builder.Configuration.AddAzureAppConfiguration(options =>
    {
        options.Connect(new Uri(appConfigEndpoint), new DefaultAzureCredential());
    });
}

// Configure console logging for cloud-native log aggregation (Azure App Service / Azure Monitor)
builder.Logging.AddJsonConsole(options =>
{
    options.IncludeScopes = true;
    options.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";
    options.UseUtcTimestamp = true;
    options.JsonWriterOptions = new System.Text.Json.JsonWriterOptions
    {
        Indented = false
    };
});

// Add Microsoft Entra ID authentication
builder.Services.AddMicrosoftIdentityWebAppAuthentication(builder.Configuration);

// Add MVC services with Microsoft Identity UI (provides /MicrosoftIdentity/Account/SignIn and SignOut endpoints)
builder.Services.AddControllersWithViews()
    .AddMicrosoftIdentityUI();

// Add Razor Pages (required by Microsoft.Identity.Web.UI)
builder.Services.AddRazorPages();

// Add EF Core with SQL Server
builder.Services.AddDbContext<SchoolContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register NotificationService as a singleton and as a hosted service so the
// Azure Service Bus processor lifecycle (start/stop) is managed by the host.
builder.Services.AddSingleton<NotificationService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<NotificationService>());

// Register Azure Blob Storage client as a singleton (thread-safe, connection-pool reuse).
// Authenticates via Managed Identity (DefaultAzureCredential).
builder.Services.AddSingleton(sp =>
{
    var serviceUri = builder.Configuration["Storage:ServiceUri"]
        ?? throw new InvalidOperationException(
            "Storage:ServiceUri is not configured. " +
            "Add it to appsettings.json: { \"Storage\": { \"ServiceUri\": \"https://<account>.blob.core.windows.net\" } }");
    return new BlobServiceClient(new Uri(serviceUri), new DefaultAzureCredential());
});
builder.Services.AddSingleton<IBlobStorageService, AzureBlobStorageService>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

// Initialize the database
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<SchoolContext>();
    DbInitializer.Initialize(context);
}

app.Run();
