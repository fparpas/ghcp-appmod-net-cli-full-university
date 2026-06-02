using System.IO;
using System.Threading.Tasks;

namespace ContosoUniversity.Services
{
    /// <summary>
    /// Abstraction over Azure Blob Storage for teaching-material image operations.
    /// </summary>
    public interface IBlobStorageService
    {
        /// <summary>
        /// Uploads a stream to Azure Blob Storage and returns the public blob URI.
        /// </summary>
        Task<string> UploadAsync(Stream content, string blobName, string contentType);

        /// <summary>
        /// Deletes the blob identified by the given Azure Blob Storage URL.
        /// No-ops gracefully if the URL is null, empty, or not a recognised Azure Blob URL.
        /// </summary>
        Task DeleteAsync(string blobUrl);
    }
}
