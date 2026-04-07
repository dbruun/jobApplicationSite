# HHSC Job Application Portal

A .NET ASP.NET Core MVC container application for Texas Health and Human Services Commission (HHSC) to manage vendor-submitted candidate applications for job postings.

## Features

- **HHSC Employee Authentication** via Entra ID (Azure AD) — employees sign in with their Microsoft account to manage job postings and review applications.
- **Job Postings Management** — HHSC employees can create, edit, and deactivate job postings. Each posting has a unique posting number.
- **Vendor Candidate Submission** — Vendors fill out a form with candidate details and attach a resume (PDF/DOC/DOCX). The form is publicly accessible via link.
- **Azure Document Intelligence** — Uploaded resumes are analyzed by Azure Document Intelligence (Form Recognizer) to extract skills and keywords. Falls back to text-based extraction if not configured.
- **Skills Filtering** — HHSC employees can filter applications by skills (comma-separated keywords matched against extracted resume text).
- **Download All Resumes as ZIP** — Download all resumes for a job posting in a single ZIP file.
- **Shareable Links** — Copy a direct link to a job's applications list to share with other HHSC employees.
- **Validation / Disqualification (DQ) Logic**:
  - Duplicate submissions (same email + job posting) are automatically DQ'd.
  - Missing resume attachment → DQ.
  - Missing DIR Candidate ID → DQ (form validation).

## Technology Stack

| Component | Technology |
|-----------|-----------|
| Framework | ASP.NET Core 10 MVC |
| Authentication | Microsoft.Identity.Web (Entra ID / Azure AD) |
| Database | **Azure SQL Database** (EF Core SqlServer provider) |
| Resume Storage | **Azure Blob Storage** |
| AI Skills Extraction | Azure Document Intelligence (Form Recognizer) |
| Container | Docker / Azure Container Apps |
| UI | Bootstrap 5 |

## Getting Started

### Prerequisites

- .NET SDK 10.0+
- Docker & Docker Compose
- Azure subscription (Azure SQL, Blob Storage, Entra ID app registration, optionally Document Intelligence)

### Azure SQL Database Setup

1. Create an **Azure SQL Server** and **Azure SQL Database** in the Azure portal (or via `az sql server create / az sql db create`).
2. Enable **Microsoft Entra authentication** on the SQL Server.
3. Grant your Container App's **Managed Identity** the `db_owner` (or appropriate) role on the database.
4. Use the following connection string format (no password needed when using Managed Identity):
   ```
   Server=tcp:<server>.database.windows.net,1433;Database=JobApplicationSiteDb;Authentication=Active Directory Default;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;
   ```
   > For local development you can use a standard SQL authentication connection string or LocalDB.

### Azure Blob Storage Setup

1. Create an **Azure Storage Account**.
2. Create a container named `resumes` (or set `AzureStorage:ContainerName` to your chosen name).
3. Grant your Container App's **Managed Identity** the `Storage Blob Data Contributor` role on the storage account.
4. Set `AzureStorage:AccountUri` to `https://<account>.blob.core.windows.net` — no key or connection string required.
   > For local development you can alternatively set `AzureStorage:ConnectionString` to a shared-key or SAS connection string. If neither is set the app falls back to local filesystem storage.

### Local Development

1. **Clone the repository**

2. **Configure Azure AD** — register an app in Entra ID and fill in `appsettings.Development.json` (or user secrets):
   ```json
   "AzureAd": {
     "TenantId": "YOUR_TENANT_ID",
     "ClientId": "YOUR_CLIENT_ID",
     "Domain": "yourdomain.onmicrosoft.com"
   }
   ```

3. **Database** — for local dev, `appsettings.Development.json` overrides the connection string to use LocalDB:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=JobApplicationSiteDb;Trusted_Connection=True;"
   }
   ```

4. **Blob Storage** (optional for local dev) — set `AzureStorage:AccountUri` (Managed Identity) or `AzureStorage:ConnectionString` (shared key). If omitted, the app stores resumes in the local `uploads/` folder.

5. **Document Intelligence** (optional):
   ```json
   "AzureDocumentIntelligence": { "Endpoint": "...", "ApiKey": "..." }
   ```

6. **Run with Docker Compose** (starts app + local SQL Server container):
   ```bash
   docker-compose up --build
   ```
   The app will be available at `http://localhost:8080`.

7. **Run locally with .NET CLI**:
   ```bash
   cd src/JobApplicationSite
   dotnet run
   ```

### Container Deployment (Azure Container Apps)

The `Dockerfile` in the repository root builds a production image. Assign a **system-assigned Managed Identity** to the Container App, grant it:
- `db_owner` (or least-privilege reader/writer) on the Azure SQL Database
- `Storage Blob Data Contributor` on the Azure Storage Account

Then deploy:

```bash
az containerapp create \
  --name hhsc-job-portal \
  --resource-group <rg> \
  --environment <env> \
  --image <acr>.azurecr.io/hhsc-job-portal:latest \
  --target-port 8080 \
  --system-assigned \
  --env-vars \
    AzureAd__TenantId=<tid> \
    AzureAd__ClientId=<cid> \
    "ConnectionStrings__DefaultConnection=Server=tcp:<server>.database.windows.net,1433;Database=JobApplicationSiteDb;Authentication=Active Directory Default;Encrypt=True;" \
    "AzureStorage__AccountUri=https://<account>.blob.core.windows.net" \
    AzureDocumentIntelligence__Endpoint=<endpoint> \
    AzureDocumentIntelligence__ApiKey=<key>
```

> **Tip:** Store secrets (`ApiKey`, connection strings with passwords) in **Azure Key Vault** and reference them via Container Apps secrets rather than plain environment variables.

## Project Structure

```
src/
  JobApplicationSite/
    Controllers/
      HomeController.cs           — Public home page / open postings
      JobPostingsController.cs    — HHSC employee job posting management (auth required)
      ApplicationsController.cs  — Application list, submission, download (mixed auth)
    Data/
      ApplicationDbContext.cs     — EF Core DbContext
      Migrations/                 — EF Core migrations
    Models/
      JobPosting.cs               — Job posting entity
      CandidateApplication.cs     — Candidate application entity
      ViewModels.cs               — SubmissionViewModel, ApplicationsListViewModel
    Services/
      IBlobStorageService.cs      — Blob storage interface
      BlobStorageService.cs       — Azure Blob Storage implementation
      LocalFileStorageService.cs  — Local file fallback (development only)
      IResumeAnalysisService.cs   — Resume analysis interface
      DocumentIntelligenceService.cs — Azure Document Intelligence implementation
    Views/
      Home/Index.cshtml           — Open postings landing page
      JobPostings/                — CRUD views for job postings
      Applications/               — ForJob, Submit, SubmitConfirmation views
      Shared/_Layout.cshtml       — Site layout with Entra ID sign-in/out
    Program.cs                    — Application startup / DI configuration
    appsettings.json              — Configuration (replace placeholder values)
Dockerfile                        — Multi-stage Docker build
docker-compose.yml                — Local development with SQL Server
```
