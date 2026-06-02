using System;
using System.IO;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ContosoUniversity.Services
{
    /// <summary>
    /// Azure Blob Storage implementation of <see cref="IBlobStorageService"/>.
    /// Uses the injected <see cref="BlobServiceClient"/>, which is authenticated with
    /// <c>DefaultAzureCredential</c> (Managed Identity) — no connection strings or access keys.
    ///
    /// Must be registered as a <b>Singleton</b> in the DI container because it holds a
    /// <see cref="BlobContainerClient"/> that wraps an Azure SDK HTTP pipeline. Creating a
    /// new instance per request would defeat the token cache and connection pool.
    /// </summary>
    public class AzureBlobStorageService : IBlobStorageService
    {
        private readonly BlobContainerClient _containerClient;
        private readonly string _containerName;
        private readonly ILogger<AzureBlobStorageService> _logger;

        /// <summary>
        /// Production constructor — called by the DI container.
        /// </summary>
        public AzureBlobStorageService(
            BlobServiceClient blobServiceClient,
            IConfiguration configuration,
            ILogger<AzureBlobStorageService> logger = null)
        {
            _containerName = configuration["Storage:ContainerName"] ?? "teaching-materials";
            _containerClient = blobServiceClient.GetBlobContainerClient(_containerName);
            _logger = logger;
        }

        /// <inheritdoc />
        /// <remarks>
        /// The container is created with <see cref="PublicAccessType.Blob"/> on first upload,
        /// making individual blobs publicly readable so images can be rendered directly in HTML.
        /// This preserves the same public-access semantics as the original static-file serving
        /// from the local <c>Uploads/TeachingMaterials</c> folder.
        ///
        /// Note: Public blob access requires the storage account's "Allow Blob Anonymous Access"
        /// setting to be enabled (default for new accounts). If it is disabled at the account
        /// level, images must be accessed through the application or via short-lived SAS URLs.
        /// </remarks>
        public async Task<string> UploadAsync(Stream content, string blobName, string contentType)
        {
            // Ensure the container exists. Idempotent: returns immediately if it already exists.
            // PublicAccessType.Blob allows direct image rendering in HTML without SAS tokens,
            // matching the original local static-file serving behaviour.
            await _containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);

            var blobClient = _containerClient.GetBlobClient(blobName);

            var uploadOptions = new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders { ContentType = contentType }
                // Conditions intentionally omitted → unconditional overwrite.
            };

            // MIGRATION NOTE: unconditional overwrite preserves the original FileMode.Create
            // semantics used when writing to the local file system.
            await blobClient.UploadAsync(content, uploadOptions);

            _logger?.LogInformation(
                "Uploaded teaching material blob '{BlobName}' to container '{Container}'.",
                blobName, _containerName);

            return blobClient.Uri.ToString();
        }

        /// <inheritdoc />
        public async Task DeleteAsync(string blobUrl)
        {
            if (string.IsNullOrEmpty(blobUrl))
                return;

            try
            {
                // Extract the blob name from the Azure Blob Storage URL.
                // Expected format: https://{account}.blob.core.windows.net/{container}/{blobName}
                var uri = new Uri(blobUrl);
                var pathWithoutLeadingSlash = uri.AbsolutePath.TrimStart('/');
                var containerPrefix = _containerName + "/";

                if (!pathWithoutLeadingSlash.StartsWith(containerPrefix,
                        StringComparison.OrdinalIgnoreCase))
                {
                    _logger?.LogWarning(
                        "Blob URL '{BlobUrl}' does not match expected container '{Container}'; skipping deletion.",
                        blobUrl, _containerName);
                    return;
                }

                var blobName = pathWithoutLeadingSlash.Substring(containerPrefix.Length);
                var blobClient = _containerClient.GetBlobClient(blobName);
                await blobClient.DeleteIfExistsAsync();

                _logger?.LogInformation(
                    "Deleted teaching material blob '{BlobName}' from container '{Container}'.",
                    blobName, _containerName);
            }
            catch (UriFormatException ex)
            {
                // Legacy local paths such as ~/Uploads/TeachingMaterials/... are not valid
                // absolute URIs. Log a warning and skip deletion gracefully so existing DB
                // records with old-style paths do not cause runtime errors.
                _logger?.LogWarning(ex,
                    "Skipping blob deletion — stored path '{BlobUrl}' is not a valid Azure Blob URL.",
                    blobUrl);
            }
        }
    }
}
