using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;

namespace JobApplicationSite.Services;

public class BlobStorageService : IBlobStorageService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly string _containerName;
    private readonly ILogger<BlobStorageService> _logger;

    public BlobStorageService(
        BlobServiceClient blobServiceClient,
        IConfiguration configuration,
        ILogger<BlobStorageService> logger)
    {
        _blobServiceClient = blobServiceClient;
        _containerName = configuration["AzureStorage:ContainerName"] ?? "resumes";
        _logger = logger;
    }

    private BlobContainerClient GetContainerClient() =>
        _blobServiceClient.GetBlobContainerClient(_containerName);

    public async Task<string> UploadResumeAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        int jobPostingId,
        CancellationToken cancellationToken = default)
    {
        var container = GetContainerClient();
        await container.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: cancellationToken);

        // Organize blobs by job posting
        var blobName = $"job-{jobPostingId}/{Guid.NewGuid():N}/{SanitizeFileName(fileName)}";
        var blobClient = container.GetBlobClient(blobName);

        var uploadOptions = new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = contentType }
        };

        await blobClient.UploadAsync(fileStream, uploadOptions, cancellationToken);
        _logger.LogInformation("Uploaded resume blob: {BlobName}", blobName);
        return blobName;
    }

    public async Task<(Stream Content, string ContentType, string FileName)> DownloadResumeAsync(
        string blobPath,
        CancellationToken cancellationToken = default)
    {
        var container = GetContainerClient();
        var blobClient = container.GetBlobClient(blobPath);

        var response = await blobClient.DownloadStreamingAsync(cancellationToken: cancellationToken);
        var contentType = response.Value.Details.ContentType ?? "application/octet-stream";
        var fileName = Path.GetFileName(blobPath);

        return (response.Value.Content, contentType, fileName);
    }

    public async Task<string> GetResumeDownloadUrlAsync(string blobPath, CancellationToken cancellationToken = default)
    {
        var container = GetContainerClient();
        var blobClient = container.GetBlobClient(blobPath);

        // Generate a short-lived SAS URI (1 hour)
        if (blobClient.CanGenerateSasUri)
        {
            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = _containerName,
                BlobName = blobPath,
                Resource = "b",
                ExpiresOn = DateTimeOffset.UtcNow.AddHours(1)
            };
            sasBuilder.SetPermissions(BlobSasPermissions.Read);
            return blobClient.GenerateSasUri(sasBuilder).ToString();
        }

        // Fallback: return the blob URI (requires public access or app-level auth)
        return blobClient.Uri.ToString();
    }

    public async Task DeleteResumeAsync(string blobPath, CancellationToken cancellationToken = default)
    {
        var container = GetContainerClient();
        var blobClient = container.GetBlobClient(blobPath);
        await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);
        _logger.LogInformation("Deleted resume blob: {BlobPath}", blobPath);
    }

    private static string SanitizeFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = string.Join("_", fileName.Split(invalidChars));
        return sanitized.Length > 200 ? sanitized[^200..] : sanitized;
    }
}
