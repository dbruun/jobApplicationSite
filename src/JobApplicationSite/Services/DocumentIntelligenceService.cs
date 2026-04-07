using Azure;
using Azure.AI.FormRecognizer.DocumentAnalysis;

namespace JobApplicationSite.Services;

/// <summary>
/// Uses Azure Document Intelligence (formerly Form Recognizer) to extract text/skills
/// from uploaded resume documents.
/// </summary>
public class DocumentIntelligenceService : IResumeAnalysisService
{
    private readonly DocumentAnalysisClient? _client;
    private readonly ILogger<DocumentIntelligenceService> _logger;

    // Common technical / professional skill keywords to look for
    private static readonly string[] CommonSkillKeywords =
    [
        "sql", "python", "java", "javascript", "c#", ".net", "azure", "aws", "devops",
        "project management", "agile", "scrum", "data analysis", "machine learning",
        "networking", "security", "cloud", "docker", "kubernetes", "api", "rest",
        "communication", "leadership", "excel", "powerbi", "power bi", "sharepoint",
        "dynamics", "salesforce", "healthcare", "medicaid", "medicaid information technology",
        "mmis", "hipaa", "hl7", "fhir", "clinical", "nursing", "social work",
        "procurement", "contracts", "budget", "finance", "hr", "human resources",
        "business analysis", "requirements", "visio", "jira", "confluence"
    ];

    public DocumentIntelligenceService(IConfiguration configuration, ILogger<DocumentIntelligenceService> logger)
    {
        _logger = logger;

        var endpoint = configuration["AzureDocumentIntelligence:Endpoint"];
        var apiKey = configuration["AzureDocumentIntelligence:ApiKey"];

        if (!string.IsNullOrWhiteSpace(endpoint) && !string.IsNullOrWhiteSpace(apiKey))
        {
            _client = new DocumentAnalysisClient(new Uri(endpoint), new AzureKeyCredential(apiKey));
        }
        else
        {
            _logger.LogWarning("Azure Document Intelligence is not configured. Skill extraction will use text-based fallback.");
        }
    }

    public async Task<string> ExtractSkillsAsync(
        Stream resumeStream,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        if (_client == null)
        {
            return await ExtractSkillsFallbackAsync(resumeStream, cancellationToken);
        }

        try
        {
            var operation = await _client.AnalyzeDocumentAsync(
                WaitUntil.Completed,
                "prebuilt-read",
                resumeStream,
                cancellationToken: cancellationToken);

            var result = operation.Value;
            var allText = string.Join(" ", result.Pages.SelectMany(p => p.Lines).Select(l => l.Content));

            return ExtractSkillsFromText(allText);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Azure Document Intelligence analysis failed; falling back to stream-based extraction.");
            resumeStream.Position = 0;
            return await ExtractSkillsFallbackAsync(resumeStream, cancellationToken);
        }
    }

    public bool MatchesSkillFilter(string extractedSkills, string filter)
    {
        if (string.IsNullOrWhiteSpace(filter) || string.IsNullOrWhiteSpace(extractedSkills))
            return true;

        var terms = filter.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return terms.Any(term =>
            extractedSkills.Contains(term, StringComparison.OrdinalIgnoreCase));
    }

    // Fallback: read raw bytes as UTF-8 text and scan for keywords
    private Task<string> ExtractSkillsFallbackAsync(Stream resumeStream, CancellationToken cancellationToken)
    {
        try
        {
            using var reader = new StreamReader(resumeStream, leaveOpen: true);
            var text = reader.ReadToEnd();
            return Task.FromResult(ExtractSkillsFromText(text));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fallback skill extraction failed.");
            return Task.FromResult(string.Empty);
        }
    }

    private static string ExtractSkillsFromText(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;

        var found = CommonSkillKeywords
            .Where(skill => text.Contains(skill, StringComparison.OrdinalIgnoreCase))
            .Select(s => s.ToLowerInvariant())
            .Distinct()
            .OrderBy(s => s);

        return string.Join(", ", found);
    }
}
