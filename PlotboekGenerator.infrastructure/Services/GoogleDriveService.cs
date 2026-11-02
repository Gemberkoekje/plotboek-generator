using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PlotboekGenerator.infrastructure.Resources;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

namespace PlotboekGenerator.infrastructure.Services;

/// <summary>
/// Service for uploading files to Google Drive using OAuth2 user credentials.
/// <para>
/// Requires <c>GoogleDrive:RefreshToken</c> plus the existing
/// <c>Authentication:Google:ClientId</c> and <c>Authentication:Google:ClientSecret</c>.
/// Use the "Autoriseer Google Drive" button in the Plotboek settings to obtain a refresh token.
/// </para>
/// </summary>
public sealed partial class GoogleDriveService(IConfiguration configuration, ILogger<GoogleDriveService> logger) : IGoogleDriveService, IDisposable
{
    private readonly string _clientId = configuration["Authentication:Google:ClientId"];
    private readonly string _clientSecret = configuration["Authentication:Google:ClientSecret"];
    private readonly string _refreshToken = configuration["GoogleDrive:RefreshToken"];
    private GoogleAuthorizationCodeFlow _flow;

    public void Dispose() => _flow?.Dispose();

    /// <summary>
    /// Whether Google Drive upload is fully configured and ready to use.
    /// </summary>
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_refreshToken) &&
        !string.IsNullOrWhiteSpace(_clientId) &&
        !string.IsNullOrWhiteSpace(_clientSecret);

    /// <summary>
    /// Whether the OAuth2 authorization flow is available but a refresh token has not yet been obtained.
    /// When <c>true</c>, the UI should show the "Autoriseer Google Drive" button.
    /// </summary>
    public bool NeedsOAuthAuthorization =>
        !IsConfigured &&
        !string.IsNullOrWhiteSpace(_clientId) &&
        !string.IsNullOrWhiteSpace(_clientSecret);

    /// <summary>
    /// Validates the stored refresh token by attempting a token refresh.
    /// Returns <c>false</c> if the token has been revoked or expired (invalid_grant).
    /// </summary>
    public async Task<bool> ValidateTokenAsync(CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
            return false;

        try
        {
            EnsureFlow();
            await _flow.RefreshTokenAsync("user", _refreshToken, cancellationToken);
            return true;
        }
        catch (TokenResponseException ex) when (ex.Error.Error == "invalid_grant")
        {
            return false;
        }
    }

    /// <summary>
    /// Uploads or updates a file in the specified Google Drive folder.
    /// If a file with the same name already exists in the folder, it will be updated.
    /// Returns the web view URL of the uploaded file.
    /// </summary>
    public Task<string> UploadOrUpdateFileAsync(string folderUrl, string fileName, byte[] content, string contentType, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            throw new InvalidOperationException("Google Drive is not configured. Set 'GoogleDrive:RefreshToken' in configuration (use the 'Autoriseer Google Drive' button to obtain one).");
        }

        var folderId = ExtractFolderId(folderUrl);
        if (string.IsNullOrEmpty(folderId))
        {
            // The folder URL is typed into the plotboek settings, so this one reaches the person uploading.
            throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, InfraTekst.GoogleDriveFolderUrlOnleesbaar, folderUrl), nameof(folderUrl));
        }

        return UploadOrUpdateFileCoreAsync(folderId, fileName, content, contentType, cancellationToken);
    }

    private async Task<string> UploadOrUpdateFileCoreAsync(string folderId, string fileName, byte[] content, string contentType, CancellationToken cancellationToken)
    {
        using var service = CreateDriveService();

        // Check if a file with this name already exists in the folder
        var existingFileId = await FindExistingFileAsync(service, folderId, fileName, cancellationToken);

        using var stream = new MemoryStream(content);

        if (existingFileId != null)
        {
            // Update existing file
            var updateMetadata = new Google.Apis.Drive.v3.Data.File
            {
                Name = fileName,
            };

            var updateRequest = service.Files.Update(updateMetadata, existingFileId, stream, contentType);
            updateRequest.SupportsAllDrives = true;
            var updateResult = await updateRequest.UploadAsync(cancellationToken);

            if (updateResult.Status != Google.Apis.Upload.UploadStatus.Completed)
            {
                throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, InfraTekst.GoogleDriveBijwerkenMislukt, updateResult.Exception?.Message));
            }

            logger.LogInformation("Updated existing file '{FileName}' (ID: {FileId}) in Google Drive folder {FolderId}", fileName, existingFileId, folderId);
            return $"https://docs.google.com/document/d/{existingFileId}/edit";
        }
        else
        {
            // Create new file
            var fileMetadata = new Google.Apis.Drive.v3.Data.File
            {
                Name = fileName,
                Parents = [folderId],
            };

            var createRequest = service.Files.Create(fileMetadata, stream, contentType);
            createRequest.Fields = "id, name";
            createRequest.SupportsAllDrives = true;
            var createResult = await createRequest.UploadAsync(cancellationToken);

            if (createResult.Status != Google.Apis.Upload.UploadStatus.Completed)
            {
                throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, InfraTekst.GoogleDriveUploadenMislukt, createResult.Exception?.Message));
            }

            var newFileId = createRequest.ResponseBody?.Id;
            logger.LogInformation("Uploaded new file '{FileName}' (ID: {FileId}) to Google Drive folder {FolderId}", fileName, newFileId, folderId);
            return $"https://docs.google.com/document/d/{newFileId}/edit";
        }
    }

    /// <summary>
    /// Exports the file currently sitting in the given Drive folder (from a previous sync) as a
    /// PDF, so its real, Google-rendered pagination can be read back. Internally converts a
    /// temporary copy to native Google Docs format to export it (Drive can only export its own
    /// document types, not an uploaded .docx directly), then always deletes that copy afterward.
    /// </summary>
    public Task<byte[]> TryExportCurrentFileAsPdfAsync(string folderUrl, string fileName, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            throw new InvalidOperationException("Google Drive is not configured. Set 'GoogleDrive:RefreshToken' in configuration (use the 'Autoriseer Google Drive' button to obtain one).");
        }

        var folderId = ExtractFolderId(folderUrl);
        if (string.IsNullOrEmpty(folderId))
        {
            throw new ArgumentException($"Could not extract a folder ID from the URL: {folderUrl}", nameof(folderUrl));
        }

        return TryExportCurrentFileAsPdfCoreAsync(folderId, fileName, cancellationToken);
    }

    private async Task<byte[]> TryExportCurrentFileAsPdfCoreAsync(string folderId, string fileName, CancellationToken cancellationToken)
    {
        using var service = CreateDriveService();

        var existingFileId = await FindExistingFileAsync(service, folderId, fileName, cancellationToken);
        if (existingFileId is null)
        {
            return null;
        }

        var copyMetadata = new Google.Apis.Drive.v3.Data.File
        {
            MimeType = "application/vnd.google-apps.document",
        };

        var copyRequest = service.Files.Copy(copyMetadata, existingFileId);
        copyRequest.Fields = "id";
        copyRequest.SupportsAllDrives = true;

        Google.Apis.Drive.v3.Data.File copy;
        try
        {
            copy = await copyRequest.ExecuteAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            // Drive may have already created the copy server-side even though this client never
            // observed a successful response (a timeout, a deserialization failure, cancellation
            // landing in that exact window) - there is no ID to clean up with, but at least record
            // that an orphaned "Copy of ..." file may now exist in the folder.
            logger.LogWarning(ex, "Requesting a Google Docs copy of '{FileName}' to export as PDF failed; an orphaned copy may have been created in the Drive folder", fileName);
            throw;
        }

        try
        {
            using var stream = new MemoryStream();
            var exportRequest = service.Files.Export(copy.Id, "application/pdf");
            var progress = await exportRequest.DownloadAsync(stream, cancellationToken);

            if (progress.Status != Google.Apis.Download.DownloadStatus.Completed)
            {
                throw new InvalidOperationException($"Failed to export Drive file '{fileName}' (copy {copy.Id}) as PDF: {progress.Exception?.Message}");
            }

            return stream.ToArray();
        }
        finally
        {
            // Deliberately CancellationToken.None: this cleanup must still run even when the
            // outer token is the reason execution reached here (e.g. the export above was
            // cancelled) - reusing an already-cancelled token would make the delete request never
            // actually go out, deterministically leaking the temporary copy on every cancellation.
            await DeleteQuietlyAsync(service, copy.Id, fileName, CancellationToken.None);
        }
    }

    /// <summary>
    /// Deletes the temporary converted copy created to export a PDF. Logs and swallows any
    /// failure - an orphaned copy left behind in the folder is a cosmetic problem, not one worth
    /// failing the whole sync cycle over.
    /// </summary>
    private async Task DeleteQuietlyAsync(DriveService service, string copyId, string fileName, CancellationToken cancellationToken)
    {
        try
        {
            var deleteRequest = service.Files.Delete(copyId);
            deleteRequest.SupportsAllDrives = true;
            await deleteRequest.ExecuteAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to delete the temporary Google Docs copy {CopyId} created while exporting {FileName} as PDF", copyId, fileName);
        }
    }

    private void EnsureFlow()
    {
        _flow ??= new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
        {
            ClientSecrets = new ClientSecrets
            {
                ClientId = _clientId,
                ClientSecret = _clientSecret,
            },
            Scopes = [DriveService.Scope.Drive],
        });
    }

    private DriveService CreateDriveService()
    {
        EnsureFlow();

        var credential = new UserCredential(_flow, "user", new TokenResponse
        {
            RefreshToken = _refreshToken,
        });

        return new DriveService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "PlotboekGenerator",
        });
    }

    private static async Task<string> FindExistingFileAsync(DriveService service, string folderId, string fileName, CancellationToken cancellationToken)
    {
        var listRequest = service.Files.List();
        listRequest.Q = $"'{folderId}' in parents and name = '{EscapeDriveQuery(fileName)}' and trashed = false";
        listRequest.Fields = "files(id, name)";
        listRequest.PageSize = 1;
        // Drive's List API leaves result order undefined without an explicit OrderBy. This method
        // is called twice per sync cycle (once to export the current file for page-number
        // correction, once to upload the new version) - without a deterministic order, a
        // duplicate-named file could make the two calls resolve to different physical files.
        // Oldest-first is the deterministic, stable choice: it always resolves to the original
        // file rather than a later accidental duplicate.
        listRequest.OrderBy = "createdTime";
        listRequest.SupportsAllDrives = true;
        listRequest.IncludeItemsFromAllDrives = true;

        var result = await listRequest.ExecuteAsync(cancellationToken);
        return result.Files?.FirstOrDefault()?.Id;
    }

    private static string ExtractFolderId(string folderUrl)
    {
        if (string.IsNullOrWhiteSpace(folderUrl))
        {
            return null;
        }

        var match = FolderIdRegex().Match(folderUrl);
        if (match.Success)
        {
            return match.Groups[1].Value;
        }

        return folderUrl.Trim();
    }

    private static string EscapeDriveQuery(string value) => value.Replace("'", "\\'");

    [GeneratedRegex(@"folders/([a-zA-Z0-9_-]+)")]
    private static partial Regex FolderIdRegex();
}
