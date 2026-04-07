using System.ComponentModel.DataAnnotations;

namespace JobApplicationSite.Models;

/// <summary>
/// View model for vendor submission form.
/// </summary>
public class SubmissionViewModel
{
    public int JobPostingId { get; set; }
    public string JobTitle { get; set; } = string.Empty;
    public string PostingNumber { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    [Display(Name = "First Name")]
    public string FirstName { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    [Display(Name = "Last Name")]
    public string LastName { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    [EmailAddress]
    [Display(Name = "Candidate Email")]
    public string Email { get; set; } = string.Empty;

    [MaxLength(20)]
    [Phone]
    [Display(Name = "Phone Number")]
    public string Phone { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    [Display(Name = "Vendor / Agency Name")]
    public string VendorName { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    [EmailAddress]
    [Display(Name = "Vendor Contact Email")]
    public string VendorContactEmail { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    [Display(Name = "DIR Candidate ID")]
    [RegularExpression(@"^[A-Za-z0-9\-_]+$", ErrorMessage = "DIR Candidate ID must contain only letters, numbers, dashes, and underscores.")]
    public string DirCandidateId { get; set; } = string.Empty;

    [Display(Name = "Cover Letter / Notes")]
    public string CoverLetter { get; set; } = string.Empty;

    [Required(ErrorMessage = "A resume attachment is required.")]
    [Display(Name = "Resume (PDF, DOC, or DOCX)")]
    public IFormFile? Resume { get; set; }
}

/// <summary>
/// View model for applications list (HHSC employee view).
/// </summary>
public class ApplicationsListViewModel
{
    public JobPosting JobPosting { get; set; } = null!;
    public List<CandidateApplication> Applications { get; set; } = new();
    public string? SkillsFilter { get; set; }
    public string? StatusFilter { get; set; }
    public string ShareableLink { get; set; } = string.Empty;
    public int TotalCount { get; set; }
    public int DisqualifiedCount { get; set; }
}
