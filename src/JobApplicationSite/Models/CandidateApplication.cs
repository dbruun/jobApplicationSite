using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JobApplicationSite.Models;

public class CandidateApplication
{
    public int Id { get; set; }

    // Foreign key to the job posting
    public int JobPostingId { get; set; }
    public JobPosting? JobPosting { get; set; }

    // Candidate information
    [Required, MaxLength(100)]
    [Display(Name = "First Name")]
    public string FirstName { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    [Display(Name = "Last Name")]
    public string LastName { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    [EmailAddress]
    [Display(Name = "Email Address")]
    public string Email { get; set; } = string.Empty;

    [MaxLength(20)]
    [Phone]
    [Display(Name = "Phone Number")]
    public string Phone { get; set; } = string.Empty;

    [MaxLength(200)]
    [Display(Name = "Vendor / Agency Name")]
    public string VendorName { get; set; } = string.Empty;

    [MaxLength(200)]
    [EmailAddress]
    [Display(Name = "Vendor Contact Email")]
    public string VendorContactEmail { get; set; } = string.Empty;

    // DIR (Department of Information Resources) candidate ID
    [MaxLength(100)]
    [Display(Name = "DIR Candidate ID")]
    public string DirCandidateId { get; set; } = string.Empty;

    // Resume blob storage reference
    [MaxLength(500)]
    [Display(Name = "Resume File Name")]
    public string ResumeFileName { get; set; } = string.Empty;

    [MaxLength(1000)]
    [Display(Name = "Resume Blob Path")]
    public string ResumeBlobPath { get; set; } = string.Empty;

    [MaxLength(100)]
    [Display(Name = "Resume Content Type")]
    public string ResumeContentType { get; set; } = string.Empty;

    // Extracted skills from Azure Document Intelligence
    [Display(Name = "Extracted Skills")]
    public string ExtractedSkills { get; set; } = string.Empty;

    // Additional notes from vendor
    [Display(Name = "Cover Letter / Notes")]
    public string CoverLetter { get; set; } = string.Empty;

    [Display(Name = "Submitted At")]
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

    // Status tracking
    [MaxLength(50)]
    [Display(Name = "Status")]
    public string Status { get; set; } = "Submitted";

    // Disqualification details
    [Display(Name = "Is Disqualified")]
    public bool IsDisqualified { get; set; } = false;

    [MaxLength(500)]
    [Display(Name = "Disqualification Reason")]
    public string DisqualificationReason { get; set; } = string.Empty;

    [NotMapped]
    [Display(Name = "Candidate Name")]
    public string FullName => $"{FirstName} {LastName}".Trim();
}
