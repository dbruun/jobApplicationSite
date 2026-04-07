namespace JobApplicationSite.Services;

public interface IBlobStorageService
{
    /// <summary>
    /// Uploads a resume file and returns the blob path.
    /// </summary>
    Task<string> UploadResumeAsync(Stream fileStream, string fileName, string contentType, int jobPostingId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads a resume as a stream.
    /// </summary>
    Task<(Stream Content, string ContentType, string FileName)> DownloadResumeAsync(string blobPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a download URL (SAS or direct) for a resume.
    /// </summary>
    Task<string> GetResumeDownloadUrlAsync(string blobPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a resume blob.
    /// </summary>
    Task DeleteResumeAsync(string blobPath, CancellationToken cancellationToken = default);
}
