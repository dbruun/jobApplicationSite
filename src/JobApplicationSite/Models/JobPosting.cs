using System.ComponentModel.DataAnnotations;

namespace JobApplicationSite.Models;

public class JobPosting
{
    public int Id { get; set; }

    [Required, MaxLength(200)]
    [Display(Name = "Job Title")]
    public string Title { get; set; } = string.Empty;

    [Required, MaxLength(50)]
    [Display(Name = "Posting Number")]
    public string PostingNumber { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Description")]
    public string Description { get; set; } = string.Empty;

    [MaxLength(200)]
    [Display(Name = "Department")]
    public string Department { get; set; } = string.Empty;

    [MaxLength(200)]
    [Display(Name = "Location")]
    public string Location { get; set; } = string.Empty;

    [Display(Name = "Posted Date")]
    public DateTime PostedDate { get; set; } = DateTime.UtcNow;

    [Display(Name = "Closing Date")]
    public DateTime? ClosingDate { get; set; }

    [Display(Name = "Is Active")]
    public bool IsActive { get; set; } = true;

    [Display(Name = "Created By")]
    public string CreatedBy { get; set; } = string.Empty;

    public ICollection<CandidateApplication> Applications { get; set; } = new List<CandidateApplication>();
}
