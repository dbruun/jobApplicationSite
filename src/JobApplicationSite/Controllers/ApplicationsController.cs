using System.IO.Compression;
using JobApplicationSite.Data;
using JobApplicationSite.Models;
using JobApplicationSite.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JobApplicationSite.Controllers;

/// <summary>
/// Manages candidate applications for job postings.
/// Viewing/downloading requires HHSC employee authentication.
/// Submitting is public (vendor-facing).
/// </summary>
public class ApplicationsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IBlobStorageService _blobStorage;
    private readonly IResumeAnalysisService _resumeAnalysis;
    private readonly ILogger<ApplicationsController> _logger;

    public ApplicationsController(
        ApplicationDbContext db,
        IBlobStorageService blobStorage,
        IResumeAnalysisService resumeAnalysis,
        ILogger<ApplicationsController> logger)
    {
        _db = db;
        _blobStorage = blobStorage;
        _resumeAnalysis = resumeAnalysis;
        _logger = logger;
    }

    // ─────────────────────────────────────────
    // HHSC Employee views (require auth)
    // ─────────────────────────────────────────

    /// <summary>
    /// Lists all applications for a job posting, with optional skill and status filters.
    /// GET /Applications/ForJob/5?skills=sql,azure&status=Submitted
    /// </summary>
    [Authorize]
    public async Task<IActionResult> ForJob(int id, string? skills, string? status)
    {
        var posting = await _db.JobPostings
            .Include(j => j.Applications)
            .FirstOrDefaultAsync(j => j.Id == id);

        if (posting == null) return NotFound();

        var query = posting.Applications.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(skills))
        {
            query = query.Where(a => _resumeAnalysis.MatchesSkillFilter(a.ExtractedSkills, skills));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(a => a.Status.Equals(status, StringComparison.OrdinalIgnoreCase));
        }

        var applications = query.OrderByDescending(a => a.SubmittedAt).ToList();

        var shareLink = Url.Action("ForJob", "Applications", new { id }, Request.Scheme) ?? string.Empty;

        var vm = new ApplicationsListViewModel
        {
            JobPosting = posting,
            Applications = applications,
            SkillsFilter = skills,
            StatusFilter = status,
            ShareableLink = shareLink,
            TotalCount = posting.Applications.Count,
            DisqualifiedCount = posting.Applications.Count(a => a.IsDisqualified)
        };

        return View(vm);
    }

    /// <summary>
    /// Downloads a single resume.
    /// GET /Applications/DownloadResume/5
    /// </summary>
    [Authorize]
    public async Task<IActionResult> DownloadResume(int id)
    {
        var application = await _db.CandidateApplications.FindAsync(id);
        if (application == null || string.IsNullOrEmpty(application.ResumeBlobPath))
            return NotFound();

        try
        {
            var (stream, contentType, fileName) = await _blobStorage.DownloadResumeAsync(application.ResumeBlobPath);
            var displayName = string.IsNullOrEmpty(application.ResumeFileName) ? fileName : application.ResumeFileName;
            return File(stream, contentType, displayName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download resume for application {Id}", id);
            TempData["Error"] = "Unable to download resume. Please try again later.";
            return RedirectToAction(nameof(ForJob), new { id = application.JobPostingId });
        }
    }

    /// <summary>
    /// Downloads all resumes for a job posting as a ZIP file.
    /// GET /Applications/DownloadAllResumes/5
    /// </summary>
    [Authorize]
    public async Task<IActionResult> DownloadAllResumes(int id)
    {
        var posting = await _db.JobPostings
            .Include(j => j.Applications)
            .FirstOrDefaultAsync(j => j.Id == id);

        if (posting == null) return NotFound();

        var applicationsWithResumes = posting.Applications
            .Where(a => !string.IsNullOrEmpty(a.ResumeBlobPath))
            .ToList();

        if (!applicationsWithResumes.Any())
        {
            TempData["Info"] = "No resumes are available for this job posting.";
            return RedirectToAction(nameof(ForJob), new { id });
        }

        var zipStream = new MemoryStream();
        using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var app in applicationsWithResumes)
            {
                try
                {
                    var (resumeStream, _, _) = await _blobStorage.DownloadResumeAsync(app.ResumeBlobPath);
                    var entryName = $"{app.LastName}_{app.FirstName}_{app.Id}{Path.GetExtension(app.ResumeFileName)}";
                    var entry = archive.CreateEntry(entryName, CompressionLevel.Fastest);
                    await using var entryStream = entry.Open();
                    await resumeStream.CopyToAsync(entryStream);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Skipping resume for application {Id} during ZIP creation.", app.Id);
                }
            }
        }

        zipStream.Position = 0;
        var zipFileName = $"Resumes_{posting.PostingNumber}_{DateTime.UtcNow:yyyyMMdd}.zip";
        return File(zipStream, "application/zip", zipFileName);
    }

    // ─────────────────────────────────────────
    // Vendor submission (public)
    // ─────────────────────────────────────────

    /// <summary>
    /// Shows the vendor submission form for a job posting.
    /// GET /Applications/Submit/5
    /// </summary>
    [AllowAnonymous]
    public async Task<IActionResult> Submit(int id)
    {
        var posting = await _db.JobPostings.FindAsync(id);
        if (posting == null || !posting.IsActive) return NotFound();

        var vm = new SubmissionViewModel
        {
            JobPostingId = id,
            JobTitle = posting.Title,
            PostingNumber = posting.PostingNumber
        };
        return View(vm);
    }

    /// <summary>
    /// Processes the vendor submission form.
    /// POST /Applications/Submit/5
    /// </summary>
    [HttpPost, ValidateAntiForgeryToken, AllowAnonymous]
    public async Task<IActionResult> Submit(int id, SubmissionViewModel model)
    {
        var posting = await _db.JobPostings.FindAsync(id);
        if (posting == null || !posting.IsActive) return NotFound();

        model.JobTitle = posting.Title;
        model.PostingNumber = posting.PostingNumber;

        if (!ModelState.IsValid)
            return View(model);

        // ── Validation / DQ checks ──────────────────────────────────────

        // 1. Resume is required (DQ if missing)
        if (model.Resume == null || model.Resume.Length == 0)
        {
            ModelState.AddModelError(nameof(model.Resume), "A resume attachment is required. Submissions without a resume will be disqualified.");
            return View(model);
        }

        // Validate resume file type
        var allowedExtensions = new[] { ".pdf", ".doc", ".docx" };
        var ext = Path.GetExtension(model.Resume.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(ext))
        {
            ModelState.AddModelError(nameof(model.Resume), "Only PDF, DOC, and DOCX files are accepted.");
            return View(model);
        }

        // 2. DIR Candidate ID is required (DQ if missing)
        if (string.IsNullOrWhiteSpace(model.DirCandidateId))
        {
            ModelState.AddModelError(nameof(model.DirCandidateId), "Submissions must include a valid DIR Candidate ID. Submissions without a DIR upload are disqualified.");
            return View(model);
        }

        // 3. Duplicate detection: same email for same job posting
        bool isDuplicate = await _db.CandidateApplications.AnyAsync(
            a => a.JobPostingId == id && a.Email == model.Email);

        // Upload resume to blob storage
        string blobPath = string.Empty;
        string extractedSkills = string.Empty;
        try
        {
            await using var stream = model.Resume.OpenReadStream();

            // Extract skills before uploading (stream will be read)
            var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);
            memoryStream.Position = 0;

            extractedSkills = await _resumeAnalysis.ExtractSkillsAsync(
                memoryStream, model.Resume.ContentType ?? "application/octet-stream");

            memoryStream.Position = 0;
            blobPath = await _blobStorage.UploadResumeAsync(
                memoryStream, model.Resume.FileName, model.Resume.ContentType ?? "application/octet-stream", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload resume for submission.");
            ModelState.AddModelError(string.Empty, "There was an error uploading your resume. Please try again.");
            return View(model);
        }

        // Persist the application
        var application = new CandidateApplication
        {
            JobPostingId = id,
            FirstName = model.FirstName,
            LastName = model.LastName,
            Email = model.Email,
            Phone = model.Phone,
            VendorName = model.VendorName,
            VendorContactEmail = model.VendorContactEmail,
            DirCandidateId = model.DirCandidateId,
            CoverLetter = model.CoverLetter,
            ResumeFileName = model.Resume.FileName,
            ResumeBlobPath = blobPath,
            ResumeContentType = model.Resume.ContentType ?? "application/octet-stream",
            ExtractedSkills = extractedSkills,
            SubmittedAt = DateTime.UtcNow
        };

        // Apply DQ if duplicate
        if (isDuplicate)
        {
            application.IsDisqualified = true;
            application.DisqualificationReason = "Duplicate submission: a candidate with this email address was already submitted to this job posting.";
            application.Status = "Disqualified";
        }

        _db.CandidateApplications.Add(application);
        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "Application {AppId} submitted for job {JobId} by vendor {Vendor}. DQ={IsDQ}",
            application.Id, id, model.VendorName, application.IsDisqualified);

        return RedirectToAction(nameof(SubmitConfirmation), new { applicationId = application.Id });
    }

    /// <summary>
    /// Confirmation page shown after successful submission.
    /// GET /Applications/SubmitConfirmation?applicationId=123
    /// </summary>
    [AllowAnonymous]
    public async Task<IActionResult> SubmitConfirmation(int applicationId)
    {
        var app = await _db.CandidateApplications
            .Include(a => a.JobPosting)
            .FirstOrDefaultAsync(a => a.Id == applicationId);

        if (app == null) return NotFound();
        return View(app);
    }
}
