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
| Database | SQL Server (EF Core) |
| Resume Storage | Azure Blob Storage |
| AI Skills Extraction | Azure Document Intelligence (Form Recognizer) |
| Container | Docker / Azure Container Apps |
| UI | Bootstrap 5 |

## Getting Started

### Prerequisites

- .NET SDK 10.0+
- Docker & Docker Compose
- Azure subscription (for Entra ID, Blob Storage, Document Intelligence)

### Local Development

1. **Clone the repository**

2. **Configure Azure AD** — register an app in Entra ID and fill in `appsettings.json`:
   ```json
   "AzureAd": {
     "TenantId": "YOUR_TENANT_ID",
     "ClientId": "YOUR_CLIENT_ID",
     "Domain": "yourdomain.onmicrosoft.com"
   }
   ```

3. **Configure Azure Storage and Document Intelligence** (optional for local dev — falls back to local file storage):
   ```json
   "AzureStorage": { "ConnectionString": "...", "ContainerName": "resumes" },
   "AzureDocumentIntelligence": { "Endpoint": "...", "ApiKey": "..." }
   ```

4. **Run with Docker Compose**:
   ```bash
   cp .env.example .env   # edit as needed
   docker-compose up --build
   ```
   The app will be available at `http://localhost:8080`.

5. **Run locally with .NET CLI** (requires SQL Server):
   ```bash
   cd src/JobApplicationSite
   dotnet run
   ```

### Container Deployment (Azure Container Apps)

The `Dockerfile` in the repository root builds a production image. Deploy to Azure Container Apps with:

```bash
az containerapp create \
  --name hhsc-job-portal \
  --resource-group <rg> \
  --environment <env> \
  --image <acr>.azurecr.io/hhsc-job-portal:latest \
  --target-port 8080 \
  --env-vars \
    AzureAd__TenantId=<tid> \
    AzureAd__ClientId=<cid> \
    ConnectionStrings__DefaultConnection="<sql-connection-string>" \
    AzureStorage__ConnectionString="<storage>" \
    AzureDocumentIntelligence__Endpoint="<endpoint>" \
    AzureDocumentIntelligence__ApiKey="<key>"
```

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
