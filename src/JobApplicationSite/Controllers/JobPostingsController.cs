using JobApplicationSite.Data;
using JobApplicationSite.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JobApplicationSite.Controllers;

/// <summary>
/// Manages job postings. Only authenticated HHSC employees can create/edit/delete postings.
/// </summary>
[Authorize]
public class JobPostingsController : Controller
{
    private readonly ApplicationDbContext _db;

    public JobPostingsController(ApplicationDbContext db)
    {
        _db = db;
    }

    // GET: /JobPostings
    public async Task<IActionResult> Index()
    {
        var postings = await _db.JobPostings
            .OrderByDescending(j => j.PostedDate)
            .ToListAsync();
        return View(postings);
    }

    // GET: /JobPostings/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var posting = await _db.JobPostings
            .Include(j => j.Applications)
            .FirstOrDefaultAsync(j => j.Id == id);

        if (posting == null) return NotFound();
        return View(posting);
    }

    // GET: /JobPostings/Create
    public IActionResult Create()
    {
        return View(new JobPosting());
    }

    // POST: /JobPostings/Create
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(JobPosting model)
    {
        if (!ModelState.IsValid) return View(model);

        // Ensure posting number is unique
        if (await _db.JobPostings.AnyAsync(j => j.PostingNumber == model.PostingNumber))
        {
            ModelState.AddModelError(nameof(model.PostingNumber), "A job posting with this posting number already exists.");
            return View(model);
        }

        model.PostedDate = DateTime.UtcNow;
        model.CreatedBy = User.Identity?.Name ?? "unknown";
        _db.JobPostings.Add(model);
        await _db.SaveChangesAsync();

        TempData["Success"] = $"Job posting '{model.Title}' created successfully.";
        return RedirectToAction(nameof(Index));
    }

    // GET: /JobPostings/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        var posting = await _db.JobPostings.FindAsync(id);
        if (posting == null) return NotFound();
        return View(posting);
    }

    // POST: /JobPostings/Edit/5
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, JobPosting model)
    {
        if (id != model.Id) return BadRequest();
        if (!ModelState.IsValid) return View(model);

        var existing = await _db.JobPostings.FindAsync(id);
        if (existing == null) return NotFound();

        // Check posting number uniqueness (excluding self)
        if (await _db.JobPostings.AnyAsync(j => j.PostingNumber == model.PostingNumber && j.Id != id))
        {
            ModelState.AddModelError(nameof(model.PostingNumber), "Another job posting with this posting number already exists.");
            return View(model);
        }

        existing.Title = model.Title;
        existing.PostingNumber = model.PostingNumber;
        existing.Description = model.Description;
        existing.Department = model.Department;
        existing.Location = model.Location;
        existing.ClosingDate = model.ClosingDate;
        existing.IsActive = model.IsActive;

        await _db.SaveChangesAsync();
        TempData["Success"] = $"Job posting '{existing.Title}' updated.";
        return RedirectToAction(nameof(Index));
    }

    // GET: /JobPostings/Delete/5
    public async Task<IActionResult> Delete(int id)
    {
        var posting = await _db.JobPostings.FindAsync(id);
        if (posting == null) return NotFound();
        return View(posting);
    }

    // POST: /JobPostings/Delete/5
    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var posting = await _db.JobPostings.FindAsync(id);
        if (posting != null)
        {
            _db.JobPostings.Remove(posting);
            await _db.SaveChangesAsync();
            TempData["Success"] = $"Job posting deleted.";
        }
        return RedirectToAction(nameof(Index));
    }
}
