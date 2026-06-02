using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using ContosoUniversity.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace ContosoUniversity.Tests.Services
{
    /// <summary>
    /// Unit tests for <see cref="AzureBlobStorageService"/>.
    ///
    /// Azure SDK clients (<see cref="BlobServiceClient"/>, <see cref="BlobContainerClient"/>,
    /// <see cref="BlobClient"/>) expose virtual methods so they can be mocked with Moq without
    /// requiring a real Azure Storage account.
    /// </summary>
    public class BlobStorageServiceTests
    {
        // ── Helpers ─────────────────────────────────────────────────────────────

        private static IConfiguration BuildConfig(
            string containerName = "teaching-materials",
            string serviceUri = "https://testaccount.blob.core.windows.net")
        {
            return new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["Storage:ContainerName"] = containerName,
                    ["Storage:ServiceUri"]    = serviceUri
                })
                .Build();
        }

        private static (
            Mock<BlobServiceClient> ServiceClientMock,
            Mock<BlobContainerClient> ContainerClientMock,
            Mock<BlobClient> BlobClientMock,
            AzureBlobStorageService Service)
            BuildSut(string blobUri = "https://testaccount.blob.core.windows.net/teaching-materials/test.jpg")
        {
            var serviceMock    = new Mock<BlobServiceClient>();
            var containerMock  = new Mock<BlobContainerClient>();
            var blobMock       = new Mock<BlobClient>();

            // Wire up GetBlobContainerClient → containerMock
            serviceMock
                .Setup(s => s.GetBlobContainerClient(It.IsAny<string>()))
                .Returns(containerMock.Object);

            // CreateIfNotExistsAsync — we don't use the return value
            containerMock
                .Setup(c => c.CreateIfNotExistsAsync(
                    It.IsAny<PublicAccessType>(),
                    It.IsAny<IDictionary<string, string>>(),
                    It.IsAny<BlobContainerEncryptionScopeOptions>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((Response<BlobContainerInfo>)null);

            // Wire up GetBlobClient → blobMock
            containerMock
                .Setup(c => c.GetBlobClient(It.IsAny<string>()))
                .Returns(blobMock.Object);

            // UploadAsync — we don't use the return value
            blobMock
                .Setup(b => b.UploadAsync(
                    It.IsAny<Stream>(),
                    It.IsAny<BlobUploadOptions>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((Response<BlobContentInfo>)null);

            // Uri property
            blobMock.SetupGet(b => b.Uri).Returns(new Uri(blobUri));

            // DeleteIfExistsAsync — we don't use the return value
            blobMock
                .Setup(b => b.DeleteIfExistsAsync(
                    It.IsAny<DeleteSnapshotsOption>(),
                    It.IsAny<BlobRequestConditions>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((Response<bool>)null);

            var config  = BuildConfig();
            var service = new AzureBlobStorageService(
                serviceMock.Object, config, NullLogger<AzureBlobStorageService>.Instance);

            return (serviceMock, containerMock, blobMock, service);
        }

        // ── UploadAsync ──────────────────────────────────────────────────────────

        [Fact]
        public async Task UploadAsync_CallsCreateIfNotExistsOnContainer()
        {
            var (_, containerMock, _, svc) = BuildSut();

            using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
            await svc.UploadAsync(stream, "test.jpg", "image/jpeg");

            containerMock.Verify(c => c.CreateIfNotExistsAsync(
                PublicAccessType.Blob,
                It.IsAny<IDictionary<string, string>>(),
                It.IsAny<BlobContainerEncryptionScopeOptions>(),
                It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task UploadAsync_CallsGetBlobClientWithCorrectBlobName()
        {
            var (_, containerMock, _, svc) = BuildSut();

            using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
            await svc.UploadAsync(stream, "course_1_abc.png", "image/png");

            containerMock.Verify(
                c => c.GetBlobClient("course_1_abc.png"),
                Times.Once);
        }

        [Fact]
        public async Task UploadAsync_CallsBlobUploadAsyncWithCorrectContentType()
        {
            var (_, _, blobMock, svc) = BuildSut();

            using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
            await svc.UploadAsync(stream, "test.png", "image/png");

            blobMock.Verify(b => b.UploadAsync(
                It.IsAny<Stream>(),
                It.Is<BlobUploadOptions>(o => o.HttpHeaders.ContentType == "image/png"),
                It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task UploadAsync_ReturnsBlobUri()
        {
            const string expectedUri = "https://testaccount.blob.core.windows.net/teaching-materials/course_1_test.jpg";
            var (_, _, _, svc) = BuildSut(blobUri: expectedUri);

            using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
            var result = await svc.UploadAsync(stream, "course_1_test.jpg", "image/jpeg");

            Assert.Equal(expectedUri, result);
        }

        [Fact]
        public async Task UploadAsync_PassesStreamDirectlyToBlobClient()
        {
            // Verifies that the stream provided by the caller is passed to UploadAsync unchanged
            // (i.e., the service does not exhaust it before the SDK reads it — see Rule 11).
            var (_, _, blobMock, svc) = BuildSut();

            var payload = new byte[] { 10, 20, 30 };
            using var stream = new MemoryStream(payload);

            Stream capturedStream = null;
            blobMock
                .Setup(b => b.UploadAsync(
                    It.IsAny<Stream>(),
                    It.IsAny<BlobUploadOptions>(),
                    It.IsAny<CancellationToken>()))
                .Callback<Stream, BlobUploadOptions, CancellationToken>((s, _, _) => capturedStream = s)
                .ReturnsAsync((Response<BlobContentInfo>)null);

            await svc.UploadAsync(stream, "test.jpg", "image/jpeg");

            Assert.NotNull(capturedStream);
            // Position should be 0 (start), confirming the stream was not pre-consumed
            Assert.Equal(0, capturedStream.Position);
        }

        // ── DeleteAsync ──────────────────────────────────────────────────────────

        [Fact]
        public async Task DeleteAsync_WithValidAzureBlobUrl_CallsGetBlobClientWithExtractedBlobName()
        {
            var (_, containerMock, _, svc) = BuildSut();
            const string blobUrl = "https://testaccount.blob.core.windows.net/teaching-materials/course_5_xyz.jpg";

            await svc.DeleteAsync(blobUrl);

            containerMock.Verify(
                c => c.GetBlobClient("course_5_xyz.jpg"),
                Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_WithValidAzureBlobUrl_CallsDeleteIfExistsAsync()
        {
            var (_, _, blobMock, svc) = BuildSut();
            const string blobUrl = "https://testaccount.blob.core.windows.net/teaching-materials/course_5_xyz.jpg";

            await svc.DeleteAsync(blobUrl);

            blobMock.Verify(b => b.DeleteIfExistsAsync(
                It.IsAny<DeleteSnapshotsOption>(),
                It.IsAny<BlobRequestConditions>(),
                It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_WithNullUrl_DoesNotCallDeleteIfExistsAsync()
        {
            var (_, _, blobMock, svc) = BuildSut();

            await svc.DeleteAsync(null);

            blobMock.Verify(b => b.DeleteIfExistsAsync(
                It.IsAny<DeleteSnapshotsOption>(),
                It.IsAny<BlobRequestConditions>(),
                It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task DeleteAsync_WithEmptyUrl_DoesNotCallDeleteIfExistsAsync()
        {
            var (_, _, blobMock, svc) = BuildSut();

            await svc.DeleteAsync(string.Empty);

            blobMock.Verify(b => b.DeleteIfExistsAsync(
                It.IsAny<DeleteSnapshotsOption>(),
                It.IsAny<BlobRequestConditions>(),
                It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task DeleteAsync_WithLegacyLocalPath_DoesNotThrow()
        {
            // Legacy DB records may contain paths like ~/Uploads/TeachingMaterials/file.jpg
            // from the old local file system implementation. DeleteAsync must not throw.
            var (_, _, blobMock, svc) = BuildSut();

            await svc.DeleteAsync("~/Uploads/TeachingMaterials/course_1_old.jpg");

            blobMock.Verify(b => b.DeleteIfExistsAsync(
                It.IsAny<DeleteSnapshotsOption>(),
                It.IsAny<BlobRequestConditions>(),
                It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task DeleteAsync_WithUrlFromDifferentContainer_DoesNotDeleteBlob()
        {
            // If the stored URL refers to a different container, the service must skip deletion
            // rather than attempting to delete a blob from the wrong container.
            var (_, _, blobMock, svc) = BuildSut();
            const string foreignContainerUrl =
                "https://testaccount.blob.core.windows.net/other-container/some-blob.jpg";

            await svc.DeleteAsync(foreignContainerUrl);

            blobMock.Verify(b => b.DeleteIfExistsAsync(
                It.IsAny<DeleteSnapshotsOption>(),
                It.IsAny<BlobRequestConditions>(),
                It.IsAny<CancellationToken>()),
                Times.Never);
        }

        // ── Configuration defaults ────────────────────────────────────────────────

        [Fact]
        public void Constructor_UsesDefaultContainerName_WhenNotConfigured()
        {
            var serviceMock   = new Mock<BlobServiceClient>();
            var containerMock = new Mock<BlobContainerClient>();
            serviceMock
                .Setup(s => s.GetBlobContainerClient(It.IsAny<string>()))
                .Returns(containerMock.Object);

            var configWithNoContainerName = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>())
                .Build();

            // Should not throw — defaults to "teaching-materials"
            var svc = new AzureBlobStorageService(
                serviceMock.Object, configWithNoContainerName);

            serviceMock.Verify(
                s => s.GetBlobContainerClient("teaching-materials"),
                Times.Once);
        }
    }
}
