namespace JobApplicationSite.Services;

public interface IResumeAnalysisService
{
    /// <summary>
    /// Analyzes a resume and extracts skills/keywords using Azure Document Intelligence.
    /// </summary>
    Task<string> ExtractSkillsAsync(Stream resumeStream, string contentType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns true if the extracted skills contain the filter keyword.
    /// </summary>
    bool MatchesSkillFilter(string extractedSkills, string filter);
}
