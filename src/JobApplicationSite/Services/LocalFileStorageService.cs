namespace JobApplicationSite.Services;

/// <summary>
/// Development/local fallback for blob storage. Stores files in wwwroot/uploads.
/// NOT suitable for production.
/// </summary>
public class LocalFileStorageService : IBlobStorageService
{
    private readonly string _baseDir;
    private readonly ILogger<LocalFileStorageService> _logger;

    public LocalFileStorageService(IWebHostEnvironment env, ILogger<LocalFileStorageService> logger)
    {
        _baseDir = Path.Combine(env.ContentRootPath, "uploads");
        Directory.CreateDirectory(_baseDir);
        _logger = logger;
        _logger.LogWarning("Using LocalFileStorageService — Azure Blob Storage is not configured.");
    }

    public async Task<string> UploadResumeAsync(Stream fileStream, string fileName, string contentType, int jobPostingId, CancellationToken cancellationToken = default)
    {
        var dir = Path.Combine(_baseDir, $"job-{jobPostingId}");
        Directory.CreateDirectory(dir);

        var blobPath = Path.Combine($"job-{jobPostingId}", $"{Guid.NewGuid():N}_{Path.GetFileName(fileName)}");
        var fullPath = Path.Combine(_baseDir, blobPath);

        await using var fs = new FileStream(fullPath, FileMode.Create);
        await fileStream.CopyToAsync(fs, cancellationToken);

        return blobPath;
    }

    public Task<(Stream Content, string ContentType, string FileName)> DownloadResumeAsync(string blobPath, CancellationToken cancellationToken = default)
    {
        var fullPath = Path.Combine(_baseDir, blobPath);
        if (!File.Exists(fullPath)) throw new FileNotFoundException("Resume file not found.", blobPath);

        Stream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read);
        return Task.FromResult<(Stream, string, string)>((stream, "application/octet-stream", Path.GetFileName(blobPath)));
    }

    public Task<string> GetResumeDownloadUrlAsync(string blobPath, CancellationToken cancellationToken = default)
    {
        return Task.FromResult($"/uploads/{blobPath.Replace('\\', '/')}");
    }

    public Task DeleteResumeAsync(string blobPath, CancellationToken cancellationToken = default)
    {
        var fullPath = Path.Combine(_baseDir, blobPath);
        if (File.Exists(fullPath)) File.Delete(fullPath);
        return Task.CompletedTask;
    }
}
