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
    /// Authenticates via Managed Identity (<see cref="Azure.Identity.DefaultAzureCredential"/>).
    /// Register this class as a Singleton so the <see cref="BlobServiceClient"/> and its
    /// connection pool are shared across all requests.
    /// </summary>
    public class AzureBlobStorageService : IBlobStorageService
    {
        private readonly BlobContainerClient _containerClient;
        private readonly ILogger<AzureBlobStorageService> _logger;

        /// <summary>
        /// Production constructor: accepts a singleton <see cref="BlobServiceClient"/> that is
        /// already authenticated with <see cref="Azure.Identity.DefaultAzureCredential"/>.
        /// </summary>
        public AzureBlobStorageService(
            BlobServiceClient blobServiceClient,
            IConfiguration configuration,
            ILogger<AzureBlobStorageService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            var containerName = configuration["Storage:ContainerName"] ?? "teaching-materials";
            _containerClient = blobServiceClient.GetBlobContainerClient(containerName);
        }

        /// <summary>
        /// Test constructor: accepts a pre-built <see cref="BlobContainerClient"/> so unit tests
        /// can inject a fake/mock without needing a real storage account.
        /// </summary>
        internal AzureBlobStorageService(BlobContainerClient containerClient, ILogger<AzureBlobStorageService> logger)
        {
            _containerClient = containerClient ?? throw new ArgumentNullException(nameof(containerClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task<string> UploadFileAsync(Stream fileStream, string blobName, string contentType = null)
        {
            if (fileStream == null) throw new ArgumentNullException(nameof(fileStream));
            if (string.IsNullOrEmpty(blobName)) throw new ArgumentNullException(nameof(blobName));

            // Ensure the container exists (idempotent – no-op if it already exists).
            await _containerClient.CreateIfNotExistsAsync(PublicAccessType.None);

            var blobClient = _containerClient.GetBlobClient(blobName);

            var uploadOptions = new BlobUploadOptions();
            if (!string.IsNullOrEmpty(contentType))
            {
                uploadOptions.HttpHeaders = new BlobHttpHeaders { ContentType = contentType };
            }

            // MIGRATION NOTE: unconditional overwrite (Conditions intentionally null) to preserve
            // the existing SaveAs / File.Create semantics – every upload replaces a same-named blob.
            await blobClient.UploadAsync(fileStream, uploadOptions);

            _logger.LogInformation(
                "Uploaded blob '{BlobName}' to container '{ContainerName}'.",
                blobName,
                _containerClient.Name);

            return blobClient.Uri.ToString();
        }

        /// <inheritdoc/>
        public async Task DeleteFileAsync(string blobUrl)
        {
            if (string.IsNullOrEmpty(blobUrl))
                return;

            try
            {
                var blobUriBuilder = new BlobUriBuilder(new Uri(blobUrl));
                var blobClient = _containerClient.GetBlobClient(blobUriBuilder.BlobName);
                await blobClient.DeleteIfExistsAsync();

                _logger.LogInformation(
                    "Deleted blob '{BlobName}' from container '{ContainerName}'.",
                    blobUriBuilder.BlobName,
                    _containerClient.Name);
            }
            catch (Exception ex)
            {
                // Log and continue — a stale blob URL should not block the main operation.
                _logger.LogWarning(ex, "Failed to delete blob at URL '{BlobUrl}'.", blobUrl);
            }
        }
    }
}
