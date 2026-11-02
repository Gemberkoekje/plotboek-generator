namespace PlotboekGenerator.infrastructure.Services;

/// <summary>
/// Abstraction for uploading files to Google Drive.
/// </summary>
public interface IGoogleDriveService
{
    /// <summary>
    /// Whether Google Drive upload is fully configured and ready to use.
    /// </summary>
    bool IsConfigured { get; }

    /// <summary>
    /// Whether the OAuth2 authorization flow is available but a refresh token has not yet been obtained.
    /// When <c>true</c>, the UI should show the "Autoriseer Google Drive" button.
    /// </summary>
    bool NeedsOAuthAuthorization { get; }

    /// <summary>
    /// Validates the stored refresh token by attempting a token refresh.
    /// Returns <c>false</c> if the token has been revoked or expired (invalid_grant).
    /// </summary>
    Task<bool> ValidateTokenAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Uploads or updates a file in the specified Google Drive folder.
    /// If a file with the same name already exists in the folder, it will be updated.
    /// Returns the web view URL of the uploaded file.
    /// </summary>
    Task<string> UploadOrUpdateFileAsync(string folderUrl, string fileName, byte[] content, string contentType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports the file currently sitting in the given Drive folder (from a previous sync) as a
    /// PDF, so its real, Google-rendered pagination can be read back. Internally converts a
    /// temporary copy to native Google Docs format to export it (Drive can only export its own
    /// document types, not an uploaded .docx directly), then always deletes that copy afterward.
    /// </summary>
    /// <returns>The rendered PDF bytes, or <c>null</c> if no file exists yet at that name.</returns>
    Task<byte[]> TryExportCurrentFileAsPdfAsync(string folderUrl, string fileName, CancellationToken cancellationToken = default);
}
