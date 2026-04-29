using Azure.AI.FormRecognizer.DocumentAnalysis;
using Azure.Identity;

namespace JobApplicationSite.Services;

/// <summary>
/// Uses Azure Document Intelligence (formerly Form Recognizer) to extract text/skills
/// from uploaded resume documents.
/// </summary>
public class DocumentIntelligenceService : IResumeAnalysisService
{
    private readonly DocumentAnalysisClient _client;
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

        if (!string.IsNullOrWhiteSpace(endpoint))
        {
            _client = new DocumentAnalysisClient(new Uri(endpoint), new DefaultAzureCredential());
        }
        else
        {
            throw new InvalidOperationException("Azure Document Intelligence is not configured. Set AzureDocumentIntelligence:Endpoint in configuration.");
        }
    }

    public async Task<string> ExtractSkillsAsync(
        Stream resumeStream,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        var operation = await _client!.AnalyzeDocumentAsync(
            WaitUntil.Completed,
            "prebuilt-read",
            resumeStream,
            cancellationToken: cancellationToken);

        var result = operation.Value;
        var allText = string.Join(" ", result.Pages.SelectMany(p => p.Lines).Select(l => l.Content));

        return ExtractSkillsFromText(allText);
    }

    public bool MatchesSkillFilter(string extractedSkills, string filter)
    {
        if (string.IsNullOrWhiteSpace(filter) || string.IsNullOrWhiteSpace(extractedSkills))
            return true;

        var terms = filter.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return terms.Any(term =>
            extractedSkills.Contains(term, StringComparison.OrdinalIgnoreCase));
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
