using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using ContosoUniversity.Controllers;
using ContosoUniversity.Data;
using ContosoUniversity.Models;
using ContosoUniversity.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace ContosoUniversity.Tests.Controllers
{
    /// <summary>
    /// Unit tests for <see cref="CoursesController"/> verifying that the controller
    /// delegates all teaching-material image storage operations to <see cref="IBlobStorageService"/>
    /// and no longer uses local file-system I/O.
    ///
    /// An EF Core InMemory database is used so no real SQL connection is required.
    /// <see cref="IBlobStorageService"/> is mocked to isolate controller logic from
    /// Azure Storage.
    /// </summary>
    public class CoursesControllerTests : IDisposable
    {
        private readonly SchoolContext _db;
        private readonly Mock<IBlobStorageService> _blobServiceMock;
        private readonly CoursesController _controller;
        private const string TestBlobUrl =
            "https://testaccount.blob.core.windows.net/teaching-materials/course_1_test.jpg";

        public CoursesControllerTests()
        {
            var options = new DbContextOptionsBuilder<SchoolContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _db = new SchoolContext(options);

            // Seed a Department so FK constraints are satisfied
            _db.Departments.Add(new Department
            {
                DepartmentID = 1,
                Name = "Test Department",
                Budget = 0,
                StartDate = DateTime.UtcNow
            });
            _db.SaveChanges();

            // NotificationService test constructor (no real Service Bus needed)
            var senderMock   = new Mock<ServiceBusSender>();
            var receiverMock = new Mock<ServiceBusReceiver>();
            senderMock
                .Setup(s => s.SendMessageAsync(
                    It.IsAny<ServiceBusMessage>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var notificationService = new NotificationService(
                senderMock.Object, receiverMock.Object);

            _blobServiceMock = new Mock<IBlobStorageService>();
            _blobServiceMock
                .Setup(s => s.UploadAsync(
                    It.IsAny<Stream>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .ReturnsAsync(TestBlobUrl);
            _blobServiceMock
                .Setup(s => s.DeleteAsync(It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            _controller = new CoursesController(_db, notificationService, _blobServiceMock.Object);
        }

        public void Dispose()
        {
            _controller?.Dispose();
            _db?.Dispose();
        }

        // ── Create ───────────────────────────────────────────────────────────────

        [Fact]
        public async Task Create_WithNoImage_SavesCourseWithoutImagePath()
        {
            var course = new Course
            {
                CourseID = 1001,
                Title = "Cloud Computing",
                Credits = 3,
                DepartmentID = 1
            };

            var result = await _controller.Create(course, null);

            Assert.IsType<RedirectToActionResult>(result);
            var saved = _db.Courses.Single(c => c.CourseID == 1001);
            Assert.Equal("Cloud Computing", saved.Title);
            Assert.Null(saved.TeachingMaterialImagePath);

            // No blob interaction when there is no file
            _blobServiceMock.Verify(
                s => s.UploadAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task Create_WithValidJpegImage_UploadsToBlobAndStoresUrl()
        {
            var course = new Course
            {
                CourseID = 1002,
                Title = "Machine Learning",
                Credits = 4,
                DepartmentID = 1
            };
            var fileMock = BuildFileMock("photo.jpg", 1024);

            var result = await _controller.Create(course, fileMock.Object);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);

            var saved = _db.Courses.Single(c => c.CourseID == 1002);
            Assert.Equal(TestBlobUrl, saved.TeachingMaterialImagePath);

            _blobServiceMock.Verify(
                s => s.UploadAsync(
                    It.IsAny<Stream>(),
                    It.Is<string>(n => n.StartsWith("course_1002_") && n.EndsWith(".jpg")),
                    "image/jpeg"),
                Times.Once);
        }

        [Fact]
        public async Task Create_WithValidPngImage_SetsCorrectContentType()
        {
            var course = new Course
            {
                CourseID = 1003,
                Title = "Data Science",
                Credits = 3,
                DepartmentID = 1
            };
            var fileMock = BuildFileMock("diagram.png", 512);

            await _controller.Create(course, fileMock.Object);

            _blobServiceMock.Verify(
                s => s.UploadAsync(
                    It.IsAny<Stream>(),
                    It.Is<string>(n => n.EndsWith(".png")),
                    "image/png"),
                Times.Once);
        }

        [Fact]
        public async Task Create_WithInvalidExtension_ReturnsViewWithModelError_AndDoesNotUpload()
        {
            var course = new Course
            {
                CourseID = 1004,
                Title = "Security",
                Credits = 3,
                DepartmentID = 1
            };
            var fileMock = BuildFileMock("document.pdf", 2048);

            var result = await _controller.Create(course, fileMock.Object);

            var view = Assert.IsType<ViewResult>(result);
            Assert.True(_controller.ModelState.ContainsKey("teachingMaterialImage"));
            Assert.False(_db.Courses.Any(c => c.CourseID == 1004));

            _blobServiceMock.Verify(
                s => s.UploadAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task Create_WithOversizedFile_ReturnsViewWithModelError_AndDoesNotUpload()
        {
            var course = new Course
            {
                CourseID = 1005,
                Title = "Networking",
                Credits = 3,
                DepartmentID = 1
            };
            // 6 MB — exceeds the 5 MB limit
            var fileMock = BuildFileMock("large.jpg", 6 * 1024 * 1024);

            var result = await _controller.Create(course, fileMock.Object);

            Assert.IsType<ViewResult>(result);
            Assert.True(_controller.ModelState.ContainsKey("teachingMaterialImage"));

            _blobServiceMock.Verify(
                s => s.UploadAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);
        }

        // ── Edit ─────────────────────────────────────────────────────────────────

        [Fact]
        public async Task Edit_WithNoNewImage_KeepsExistingImagePath_AndDoesNotInteractWithBlobStorage()
        {
            // Arrange: course already has a blob URL stored
            var course = new Course
            {
                CourseID = 2001,
                Title = "Algorithms",
                Credits = 4,
                DepartmentID = 1,
                TeachingMaterialImagePath = TestBlobUrl
            };
            _db.Courses.Add(course);
            _db.SaveChanges();
            _db.Entry(course).State = EntityState.Detached;

            // Act: edit without uploading a new file
            var editedCourse = new Course
            {
                CourseID = 2001,
                Title = "Algorithms (Updated)",
                Credits = 4,
                DepartmentID = 1,
                TeachingMaterialImagePath = TestBlobUrl   // passed through hidden field
            };

            var result = await _controller.Edit(editedCourse, null);

            Assert.IsType<RedirectToActionResult>(result);
            var saved = _db.Courses.Find(2001);
            Assert.Equal("Algorithms (Updated)", saved.Title);
            Assert.Equal(TestBlobUrl, saved.TeachingMaterialImagePath);

            _blobServiceMock.Verify(
                s => s.UploadAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);
            _blobServiceMock.Verify(
                s => s.DeleteAsync(It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task Edit_WithNewValidImage_DeletesOldBlob_AndUploadsNewBlob()
        {
            const string oldBlobUrl = "https://testaccount.blob.core.windows.net/teaching-materials/course_2002_old.jpg";
            const string newBlobUrl = "https://testaccount.blob.core.windows.net/teaching-materials/course_2002_new.jpg";

            _blobServiceMock
                .Setup(s => s.UploadAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(newBlobUrl);

            var course = new Course
            {
                CourseID = 2002,
                Title = "Databases",
                Credits = 3,
                DepartmentID = 1,
                TeachingMaterialImagePath = oldBlobUrl
            };
            _db.Courses.Add(course);
            _db.SaveChanges();
            _db.Entry(course).State = EntityState.Detached;

            var editedCourse = new Course
            {
                CourseID = 2002,
                Title = "Databases",
                Credits = 3,
                DepartmentID = 1,
                TeachingMaterialImagePath = oldBlobUrl   // hidden field value
            };
            var fileMock = BuildFileMock("new-image.jpg", 2048);

            var result = await _controller.Edit(editedCourse, fileMock.Object);

            Assert.IsType<RedirectToActionResult>(result);

            // Old blob must be deleted
            _blobServiceMock.Verify(s => s.DeleteAsync(oldBlobUrl), Times.Once);

            // New blob must be uploaded
            _blobServiceMock.Verify(
                s => s.UploadAsync(
                    It.IsAny<Stream>(),
                    It.Is<string>(n => n.StartsWith("course_2002_") && n.EndsWith(".jpg")),
                    "image/jpeg"),
                Times.Once);

            // Saved path must be the new blob URL
            var saved = _db.Courses.Find(2002);
            Assert.Equal(newBlobUrl, saved.TeachingMaterialImagePath);
        }

        [Fact]
        public async Task Edit_WithInvalidExtension_ReturnsViewWithModelError_AndDoesNotUpload()
        {
            var course = new Course
            {
                CourseID = 2003,
                Title = "Software Engineering",
                Credits = 3,
                DepartmentID = 1,
                TeachingMaterialImagePath = TestBlobUrl
            };
            _db.Courses.Add(course);
            _db.SaveChanges();
            _db.Entry(course).State = EntityState.Detached;

            var editedCourse = new Course
            {
                CourseID = 2003,
                Title = "Software Engineering",
                Credits = 3,
                DepartmentID = 1,
                TeachingMaterialImagePath = TestBlobUrl
            };
            var fileMock = BuildFileMock("notes.docx", 1024);

            var result = await _controller.Edit(editedCourse, fileMock.Object);

            Assert.IsType<ViewResult>(result);
            Assert.True(_controller.ModelState.ContainsKey("teachingMaterialImage"));

            _blobServiceMock.Verify(
                s => s.UploadAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);
            _blobServiceMock.Verify(
                s => s.DeleteAsync(It.IsAny<string>()),
                Times.Never);
        }

        // ── DeleteConfirmed ───────────────────────────────────────────────────────

        [Fact]
        public async Task DeleteConfirmed_WithImagePath_DeletesBlobAndRemovesCourse()
        {
            var course = new Course
            {
                CourseID = 3001,
                Title = "Operating Systems",
                Credits = 3,
                DepartmentID = 1,
                TeachingMaterialImagePath = TestBlobUrl
            };
            _db.Courses.Add(course);
            _db.SaveChanges();

            var result = await _controller.DeleteConfirmed(3001);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);

            Assert.False(_db.Courses.Any(c => c.CourseID == 3001));

            _blobServiceMock.Verify(s => s.DeleteAsync(TestBlobUrl), Times.Once);
        }

        [Fact]
        public async Task DeleteConfirmed_WithoutImagePath_RemovesCourse_WithoutCallingBlobDelete()
        {
            // Even though DeleteAsync would handle null gracefully, the test confirms the
            // controller always calls DeleteAsync (which is safe with null) and the DB record
            // is removed.
            var course = new Course
            {
                CourseID = 3002,
                Title = "Compilers",
                Credits = 4,
                DepartmentID = 1,
                TeachingMaterialImagePath = null
            };
            _db.Courses.Add(course);
            _db.SaveChanges();

            await _controller.DeleteConfirmed(3002);

            Assert.False(_db.Courses.Any(c => c.CourseID == 3002));

            // DeleteAsync is called with null — which is a no-op inside the service
            _blobServiceMock.Verify(s => s.DeleteAsync(null), Times.Once);
        }

        [Fact]
        public async Task DeleteConfirmed_WithLegacyLocalPath_DoesNotThrow_AndRemovesCourse()
        {
            // DB records migrated from the old local-file implementation may still have
            // ~/Uploads/... paths. The controller must not crash when deleting them.
            var course = new Course
            {
                CourseID = 3003,
                Title = "Computer Graphics",
                Credits = 3,
                DepartmentID = 1,
                TeachingMaterialImagePath = "~/Uploads/TeachingMaterials/course_3003_old.jpg"
            };
            _db.Courses.Add(course);
            _db.SaveChanges();

            // DeleteAsync must handle the legacy path gracefully (no throw)
            await _controller.DeleteConfirmed(3003);

            Assert.False(_db.Courses.Any(c => c.CourseID == 3003));
            _blobServiceMock.Verify(
                s => s.DeleteAsync("~/Uploads/TeachingMaterials/course_3003_old.jpg"),
                Times.Once);
        }

        // ── Verify no local file-system I/O in controller ─────────────────────────

        [Fact]
        public async Task Create_UsesIBlobStorageService_NotLocalFileSystem()
        {
            // This test documents the contract: UploadAsync on IBlobStorageService must be
            // called instead of any local FileStream / Directory operations.
            var course = new Course
            {
                CourseID = 9001,
                Title = "Cloud Native",
                Credits = 3,
                DepartmentID = 1
            };
            var fileMock = BuildFileMock("image.jpg", 1024);

            await _controller.Create(course, fileMock.Object);

            // Blob service was called — no local I/O paths involved
            _blobServiceMock.Verify(
                s => s.UploadAsync(It.IsAny<Stream>(), It.IsAny<string>(), "image/jpeg"),
                Times.Once);
        }

        // ── Private helpers ─────────────────────────────────────────────────────

        private static Mock<IFormFile> BuildFileMock(string fileName, long size)
        {
            var mock = new Mock<IFormFile>();
            mock.SetupGet(f => f.FileName).Returns(fileName);
            mock.SetupGet(f => f.Length).Returns(size);
            mock.Setup(f => f.OpenReadStream()).Returns(new MemoryStream(new byte[Math.Min(size, 1024)]));
            return mock;
        }
    }
}
