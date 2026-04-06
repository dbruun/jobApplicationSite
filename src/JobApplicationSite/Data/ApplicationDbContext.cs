using JobApplicationSite.Models;
using Microsoft.EntityFrameworkCore;

namespace JobApplicationSite.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<JobPosting> JobPostings => Set<JobPosting>();
    public DbSet<CandidateApplication> CandidateApplications => Set<CandidateApplication>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<JobPosting>(entity =>
        {
            entity.HasIndex(e => e.PostingNumber).IsUnique();
            entity.Property(e => e.PostedDate).HasDefaultValueSql("GETUTCDATE()");
        });

        modelBuilder.Entity<CandidateApplication>(entity =>
        {
            entity.HasOne(e => e.JobPosting)
                  .WithMany(j => j.Applications)
                  .HasForeignKey(e => e.JobPostingId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.Property(e => e.SubmittedAt).HasDefaultValueSql("GETUTCDATE()");

            // Index for duplicate detection
            entity.HasIndex(e => new { e.JobPostingId, e.Email })
                  .HasDatabaseName("IX_CandidateApplication_JobPosting_Email");
        });
    }
}
