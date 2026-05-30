using System.IO;
using System.Threading.Tasks;

namespace ContosoUniversity.Services
{
    /// <summary>
    /// Abstraction over Azure Blob Storage for teaching-material file operations.
    /// </summary>
    public interface IBlobStorageService
    {
        /// <summary>
        /// Uploads a file stream as a blob and returns the full blob URL.
        /// </summary>
        /// <param name="fileStream">The file content stream to upload.</param>
        /// <param name="blobName">The name of the blob within the container.</param>
        /// <param name="contentType">Optional HTTP content-type header value (e.g. "image/jpeg").</param>
        /// <returns>The absolute URL of the uploaded blob.</returns>
        Task<string> UploadFileAsync(Stream fileStream, string blobName, string contentType = null);

        /// <summary>
        /// Deletes the blob identified by its full URL.
        /// If the blob does not exist the call is a no-op.
        /// </summary>
        /// <param name="blobUrl">Full URL of the blob to delete (as stored in TeachingMaterialImagePath).</param>
        Task DeleteFileAsync(string blobUrl);
    }
}
