using Azure.Identity;
using Azure.Storage.Blobs;
using JobApplicationSite.Data;
using JobApplicationSite.Services;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;

var builder = WebApplication.CreateBuilder(args);

// ── Authentication: Entra ID (Azure AD) ───────────────────────────────────────
builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(options =>
    {
        builder.Configuration.GetSection("AzureAd").Bind(options);
        options.ResponseType = "code";
    });

builder.Services.AddAuthorization();

// ── MVC + Razor Pages (for Microsoft.Identity.Web.UI sign-in/out pages) ──────
builder.Services.AddControllersWithViews()
    .AddMicrosoftIdentityUI();
builder.Services.AddRazorPages();

// ── Database: Azure SQL (EF Core SqlServer provider) ─────────────────────────
// In production, use an Azure SQL connection string with Managed Identity:
//   "Server=tcp:<server>.database.windows.net,1433;Database=<db>;Authentication=Active Directory Default"
// For local development, a standard SQL Server connection string works too.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions => sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null)));

// ── Azure Blob Storage ────────────────────────────────────────────────────────
// Managed Identity (DefaultAzureCredential): set AzureStorage:AccountUri to
//   "https://<account>.blob.core.windows.net"
// Dev-only fallback: local file system when AccountUri is not configured.
var storageAccountUri = builder.Configuration["AzureStorage:AccountUri"];

if (!string.IsNullOrWhiteSpace(storageAccountUri))
{
    // Managed Identity / DefaultAzureCredential (recommended for production)
    builder.Services.AddSingleton(
        new BlobServiceClient(new Uri(storageAccountUri), new DefaultAzureCredential()));
    builder.Services.AddScoped<IBlobStorageService, BlobStorageService>();
}
else
{
    // Local file system fallback — DEVELOPMENT ONLY, not suitable for production
    builder.Services.AddScoped<IBlobStorageService, LocalFileStorageService>();
}

// ── Document Intelligence (resume skills extraction) ─────────────────────────
builder.Services.AddScoped<IResumeAnalysisService, DocumentIntelligenceService>();

var app = builder.Build();

// ── Apply EF Core migrations automatically at startup ────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
}

// ── HTTP Pipeline ─────────────────────────────────────────────────────────────
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

// Microsoft.Identity.Web.UI routes (/MicrosoftIdentity/Account/SignIn, etc.)
app.MapRazorPages();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();


