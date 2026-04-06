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
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"));

builder.Services.AddAuthorization();

// ── MVC + Razor Pages (for Microsoft.Identity.Web.UI sign-in/out pages) ──────
builder.Services.AddControllersWithViews()
    .AddMicrosoftIdentityUI();

// ── Database (SQL Server; swap provider via configuration) ────────────────────
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ── Azure Blob Storage ────────────────────────────────────────────────────────
var storageConnectionString = builder.Configuration["AzureStorage:ConnectionString"];
if (!string.IsNullOrWhiteSpace(storageConnectionString))
{
    builder.Services.AddSingleton(new BlobServiceClient(storageConnectionString));
    builder.Services.AddScoped<IBlobStorageService, BlobStorageService>();
}
else
{
    // No-op implementation for local/development without Azure storage configured
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

