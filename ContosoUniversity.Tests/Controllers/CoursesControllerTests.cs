using System;
using System.IO;
using System.Threading.Tasks;
using ContosoUniversity.Controllers;
using ContosoUniversity.Data;
using ContosoUniversity.Models;
using ContosoUniversity.Services;
using ContosoUniversity.Tests.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace ContosoUniversity.Tests.Controllers
{
    /// <summary>
    /// Unit tests for <see cref="CoursesController"/> verifying Azure Blob Storage migration.
    /// All tests use an in-memory EF Core database and a mocked <see cref="IBlobStorageService"/>.
    /// </summary>
    public class CoursesControllerTests : IDisposable
    {
        private readonly SchoolContext _context;
        private readonly Mock<IBlobStorageService> _blobServiceMock;
        private readonly CoursesController _controller;

        private const string FakeBlobUrl = "https://myaccount.blob.core.windows.net/teaching-materials/course_1_abc.jpg";

        public CoursesControllerTests()
        {
            var options = new DbContextOptionsBuilder<SchoolContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new SchoolContext(options);

            // Seed a department so SelectList does not fail in error-path tests.
            _context.Set<Department>().Add(new Department
            {
                DepartmentID = 1,
                Name = "Mathematics",
                Budget = 100000m,
                StartDate = DateTime.UtcNow
            });
            _context.SaveChanges();

            _blobServiceMock = new Mock<IBlobStorageService>();

            // Use the internal test constructor so no real Service Bus connection is needed.
            var notificationService = new NotificationService(
                new FakeServiceBusSender(),
                new FakeServiceBusProcessor(),
                NullLogger<NotificationService>.Instance);

            _controller = new CoursesController(_context, notificationService, _blobServiceMock.Object, NullLogger<BaseController>.Instance);
        }

        public void Dispose()
        {
            _controller.Dispose();
            _context.Dispose();
        }

        // ── Create (POST) ─────────────────────────────────────────────────────

        [Fact]
        public async Task Create_Post_WithValidFile_UploadsToBlobAndSavesCourse()
        {
            _blobServiceMock
                .Setup(s => s.UploadFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(FakeBlobUrl);

            var course = new Course { CourseID = 1050, Title = "Algebra", Credits = 3, DepartmentID = 1 };
            var file = BuildMockFile("material.jpg", 1024, "image/jpeg");

            var result = await _controller.Create(course, file.Object);

            // Redirects to Index on success
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);

            // Course is persisted with the blob URL
            var saved = await _context.Courses.FindAsync(1050);
            Assert.NotNull(saved);
            Assert.Equal(FakeBlobUrl, saved.TeachingMaterialImagePath);

            // Blob service was called exactly once with the correct content-type
            _blobServiceMock.Verify(
                s => s.UploadFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), "image/jpeg"),
                Times.Once);
        }

        [Fact]
        public async Task Create_Post_WithInvalidExtension_ReturnsViewWithModelError()
        {
            var course = new Course { CourseID = 1051, Title = "Biology", Credits = 3, DepartmentID = 1 };
            var file = BuildMockFile("malware.exe", 512, "application/octet-stream");

            var result = await _controller.Create(course, file.Object);

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.False(viewResult.ViewData.ModelState.IsValid);
            Assert.True(viewResult.ViewData.ModelState.ContainsKey("teachingMaterialImage"));

            // Blob service must NOT have been called
            _blobServiceMock.Verify(
                s => s.UploadFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task Create_Post_WithFileTooLarge_ReturnsViewWithModelError()
        {
            var course = new Course { CourseID = 1052, Title = "Chemistry", Credits = 4, DepartmentID = 1 };
            // 6 MB — exceeds the 5 MB limit
            var file = BuildMockFile("large.jpg", 6 * 1024 * 1024, "image/jpeg");

            var result = await _controller.Create(course, file.Object);

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.False(viewResult.ViewData.ModelState.IsValid);
            Assert.True(viewResult.ViewData.ModelState.ContainsKey("teachingMaterialImage"));

            _blobServiceMock.Verify(
                s => s.UploadFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task Create_Post_WithNoFile_CreatesCourseWithoutBlobPath()
        {
            var course = new Course { CourseID = 1053, Title = "History", Credits = 2, DepartmentID = 1 };

            var result = await _controller.Create(course, teachingMaterialImage: null);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);

            var saved = await _context.Courses.FindAsync(1053);
            Assert.NotNull(saved);
            Assert.Null(saved.TeachingMaterialImagePath);

            _blobServiceMock.Verify(
                s => s.UploadFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task Create_Post_WhenUploadThrows_ReturnsViewWithModelError()
        {
            _blobServiceMock
                .Setup(s => s.UploadFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new InvalidOperationException("Storage unavailable"));

            var course = new Course { CourseID = 1054, Title = "Physics", Credits = 4, DepartmentID = 1 };
            var file = BuildMockFile("diagram.png", 2048, "image/png");

            var result = await _controller.Create(course, file.Object);

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.False(viewResult.ViewData.ModelState.IsValid);
            Assert.True(viewResult.ViewData.ModelState.ContainsKey("teachingMaterialImage"));

            // No course should have been saved
            Assert.Null(await _context.Courses.FindAsync(1054));
        }

        // ── Edit (POST) ───────────────────────────────────────────────────────

        [Fact]
        public async Task Edit_Post_WithNewFile_DeletesOldBlobAndUploadsNewBlob()
        {
            const string oldBlobUrl = "https://myaccount.blob.core.windows.net/teaching-materials/course_2_old.jpg";
            const string newBlobUrl = "https://myaccount.blob.core.windows.net/teaching-materials/course_2_new.jpg";

            _context.Courses.Add(new Course
            {
                CourseID = 2010,
                Title = "Calculus",
                Credits = 4,
                DepartmentID = 1,
                TeachingMaterialImagePath = oldBlobUrl
            });
            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear(); // detach seeded entity so controller can track the bound model

            _blobServiceMock
                .Setup(s => s.UploadFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(newBlobUrl);
            _blobServiceMock
                .Setup(s => s.DeleteFileAsync(oldBlobUrl))
                .Returns(Task.CompletedTask);

            // The Bind model comes from the form — pass the existing old path so the controller can delete it.
            var courseToEdit = new Course
            {
                CourseID = 2010,
                Title = "Calculus II",
                Credits = 4,
                DepartmentID = 1,
                TeachingMaterialImagePath = oldBlobUrl
            };
            var file = BuildMockFile("new-material.jpg", 1024, "image/jpeg");

            var result = await _controller.Edit(courseToEdit, file.Object);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);

            // Old blob must have been deleted
            _blobServiceMock.Verify(s => s.DeleteFileAsync(oldBlobUrl), Times.Once);

            // New blob must have been uploaded
            _blobServiceMock.Verify(
                s => s.UploadFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), "image/jpeg"),
                Times.Once);

            // Persisted path is the new blob URL
            _context.Entry(courseToEdit).Reload();
            Assert.Equal(newBlobUrl, courseToEdit.TeachingMaterialImagePath);
        }

        [Fact]
        public async Task Edit_Post_WithNoFile_UpdatesCourseWithoutTouchingBlob()
        {
            const string existingBlobUrl = "https://myaccount.blob.core.windows.net/teaching-materials/course_3_existing.jpg";

            _context.Courses.Add(new Course
            {
                CourseID = 3010,
                Title = "Statistics",
                Credits = 3,
                DepartmentID = 1,
                TeachingMaterialImagePath = existingBlobUrl
            });
            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear(); // detach seeded entity so controller can track the bound model

            var courseToEdit = new Course
            {
                CourseID = 3010,
                Title = "Statistics Updated",
                Credits = 3,
                DepartmentID = 1,
                TeachingMaterialImagePath = existingBlobUrl
            };

            var result = await _controller.Edit(courseToEdit, teachingMaterialImage: null);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);

            // Blob service should not have been touched at all
            _blobServiceMock.Verify(s => s.DeleteFileAsync(It.IsAny<string>()), Times.Never);
            _blobServiceMock.Verify(
                s => s.UploadFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task Edit_Post_WithInvalidExtension_ReturnsViewWithModelError()
        {
            var course = new Course { CourseID = 4010, Title = "Geometry", Credits = 3, DepartmentID = 1 };
            _context.Courses.Add(course);
            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();

            var courseToEdit = new Course { CourseID = 4010, Title = "Geometry", Credits = 3, DepartmentID = 1 };
            var file = BuildMockFile("script.js", 512, "application/javascript");

            var result = await _controller.Edit(courseToEdit, file.Object);

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.False(viewResult.ViewData.ModelState.IsValid);
            Assert.True(viewResult.ViewData.ModelState.ContainsKey("teachingMaterialImage"));

            _blobServiceMock.Verify(
                s => s.UploadFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);
        }

        // ── DeleteConfirmed (POST) ────────────────────────────────────────────

        [Fact]
        public async Task DeleteConfirmed_WithBlobPath_DeletesBlobAndRemovesCourse()
        {
            const string blobUrl = "https://myaccount.blob.core.windows.net/teaching-materials/course_5_img.jpg";

            _context.Courses.Add(new Course
            {
                CourseID = 5010,
                Title = "Trigonometry",
                Credits = 3,
                DepartmentID = 1,
                TeachingMaterialImagePath = blobUrl
            });
            await _context.SaveChangesAsync();

            _blobServiceMock
                .Setup(s => s.DeleteFileAsync(blobUrl))
                .Returns(Task.CompletedTask);

            var result = await _controller.DeleteConfirmed(5010);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);

            // Blob must have been deleted
            _blobServiceMock.Verify(s => s.DeleteFileAsync(blobUrl), Times.Once);

            // Course must have been removed from the database
            Assert.Null(await _context.Courses.FindAsync(5010));
        }

        [Fact]
        public async Task DeleteConfirmed_WithNoBlobPath_RemovesCourseWithoutCallingBlobService()
        {
            _context.Courses.Add(new Course
            {
                CourseID = 6010,
                Title = "Linear Algebra",
                Credits = 4,
                DepartmentID = 1,
                TeachingMaterialImagePath = null
            });
            await _context.SaveChangesAsync();

            var result = await _controller.DeleteConfirmed(6010);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);

            // Blob service must NOT have been called
            _blobServiceMock.Verify(s => s.DeleteFileAsync(It.IsAny<string>()), Times.Never);

            Assert.Null(await _context.Courses.FindAsync(6010));
        }

        // ── BlobPath format ───────────────────────────────────────────────────

        [Fact]
        public async Task Create_Post_BlobUrlStoredInTeachingMaterialImagePath_NotLocalPath()
        {
            _blobServiceMock
                .Setup(s => s.UploadFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(FakeBlobUrl);

            var course = new Course { CourseID = 7010, Title = "Number Theory", Credits = 3, DepartmentID = 1 };
            var file = BuildMockFile("notes.jpg", 512, "image/jpeg");

            await _controller.Create(course, file.Object);

            var saved = await _context.Courses.FindAsync(7010);
            Assert.NotNull(saved);

            // Must be a blob URL, not a local path starting with /Uploads/
            Assert.StartsWith("https://", saved.TeachingMaterialImagePath, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("/Uploads/", saved.TeachingMaterialImagePath, StringComparison.OrdinalIgnoreCase);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        /// <summary>Builds a mock IFormFile with the specified name, length, and content-type.</summary>
        private static Mock<IFormFile> BuildMockFile(string fileName, long length, string contentType)
        {
            var mock = new Mock<IFormFile>();
            mock.Setup(f => f.FileName).Returns(fileName);
            mock.Setup(f => f.Length).Returns(length);
            mock.Setup(f => f.ContentType).Returns(contentType);
            mock.Setup(f => f.OpenReadStream()).Returns(new MemoryStream(new byte[Math.Min(length, 64)]));
            return mock;
        }
    }
}
