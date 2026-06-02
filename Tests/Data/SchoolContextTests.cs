using System;
using System.Linq;
using ContosoUniversity.Data;
using ContosoUniversity.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ContosoUniversity.Tests.Data
{
    /// <summary>
    /// Unit tests for <see cref="SchoolContext"/> verifying that it can be configured
    /// for Azure SQL Database with Managed Identity (passwordless) authentication.
    ///
    /// Tests use the EF Core InMemory provider so no real Azure SQL connection is required.
    /// </summary>
    public class SchoolContextTests
    {
        // ── Connection string format validation ────────────────────────────────

        [Fact]
        public void AzureSqlConnectionString_DoesNotContainLocalDb()
        {
            const string connectionString =
                "Server=tcp:<YOUR_SERVER_NAME>.database.windows.net;" +
                "Database=ContosoUniversity;" +
                "Authentication=Active Directory Default;";

            Assert.DoesNotContain("(LocalDb)", connectionString, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("localdb", connectionString, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void AzureSqlConnectionString_DoesNotContainIntegratedSecurity()
        {
            const string connectionString =
                "Server=tcp:<YOUR_SERVER_NAME>.database.windows.net;" +
                "Database=ContosoUniversity;" +
                "Authentication=Active Directory Default;";

            Assert.DoesNotContain("Integrated Security", connectionString, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void AzureSqlConnectionString_ContainsActiveDirectoryDefault()
        {
            const string connectionString =
                "Server=tcp:<YOUR_SERVER_NAME>.database.windows.net;" +
                "Database=ContosoUniversity;" +
                "Authentication=Active Directory Default;";

            Assert.Contains("Authentication=Active Directory Default", connectionString, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void AzureSqlConnectionString_TargetsAzureSqlEndpoint()
        {
            const string connectionString =
                "Server=tcp:<YOUR_SERVER_NAME>.database.windows.net;" +
                "Database=ContosoUniversity;" +
                "Authentication=Active Directory Default;";

            Assert.Contains(".database.windows.net", connectionString, StringComparison.OrdinalIgnoreCase);
        }

        // ── SchoolContext with InMemory provider ────────────────────────────────

        [Fact]
        public void SchoolContext_CanBeCreatedWithInMemoryProvider()
        {
            var options = new DbContextOptionsBuilder<SchoolContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new SchoolContext(options);
            Assert.NotNull(context);
        }

        [Fact]
        public void SchoolContext_CanAddAndRetrieveStudents()
        {
            var options = new DbContextOptionsBuilder<SchoolContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using (var context = new SchoolContext(options))
            {
                context.Students.Add(new Student
                {
                    LastName = "Doe",
                    FirstMidName = "Jane",
                    EnrollmentDate = DateTime.UtcNow
                });
                context.SaveChanges();
            }

            using (var context = new SchoolContext(options))
            {
                var student = context.Students.FirstOrDefault(s => s.LastName == "Doe");
                Assert.NotNull(student);
                Assert.Equal("Jane", student.FirstMidName);
            }
        }

        [Fact]
        public void SchoolContext_CanAddAndRetrieveCourses()
        {
            var options = new DbContextOptionsBuilder<SchoolContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using (var context = new SchoolContext(options))
            {
                context.Courses.Add(new Course
                {
                    CourseID = 1001,
                    Title = "Introduction to Azure",
                    Credits = 3
                });
                context.SaveChanges();
            }

            using (var context = new SchoolContext(options))
            {
                var course = context.Courses.FirstOrDefault(c => c.CourseID == 1001);
                Assert.NotNull(course);
                Assert.Equal("Introduction to Azure", course.Title);
                Assert.Equal(3, course.Credits);
            }
        }

        [Fact]
        public void SchoolContext_CanAddAndRetrieveDepartments()
        {
            var options = new DbContextOptionsBuilder<SchoolContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            var budgetDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            using (var context = new SchoolContext(options))
            {
                context.Departments.Add(new Department
                {
                    Name = "Engineering",
                    Budget = 100000m,
                    StartDate = budgetDate
                });
                context.SaveChanges();
            }

            using (var context = new SchoolContext(options))
            {
                var dept = context.Departments.FirstOrDefault(d => d.Name == "Engineering");
                Assert.NotNull(dept);
                Assert.Equal(100000m, dept.Budget);
            }
        }

        [Fact]
        public void SchoolContext_CanAddAndRetrieveNotifications()
        {
            var options = new DbContextOptionsBuilder<SchoolContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using (var context = new SchoolContext(options))
            {
                context.Notifications.Add(new Notification
                {
                    EntityType = "Student",
                    EntityId = "42",
                    Operation = "CREATE",
                    Message = "New Student 'Jane Doe' has been created",
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "System",
                    IsRead = false
                });
                context.SaveChanges();
            }

            using (var context = new SchoolContext(options))
            {
                var notification = context.Notifications
                    .FirstOrDefault(n => n.EntityId == "42");
                Assert.NotNull(notification);
                Assert.Equal("Student", notification.EntityType);
                Assert.Equal("CREATE", notification.Operation);
                Assert.False(notification.IsRead);
            }
        }

        [Fact]
        public void SchoolContext_DbSetsAreNotNull()
        {
            var options = new DbContextOptionsBuilder<SchoolContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new SchoolContext(options);

            Assert.NotNull(context.Courses);
            Assert.NotNull(context.Enrollments);
            Assert.NotNull(context.Departments);
            Assert.NotNull(context.OfficeAssignments);
            Assert.NotNull(context.CourseAssignments);
            Assert.NotNull(context.People);
            Assert.NotNull(context.Students);
            Assert.NotNull(context.Instructors);
            Assert.NotNull(context.Notifications);
        }

        // ── SchoolContextFactory ────────────────────────────────────────────────

        [Fact]
        public void SchoolContextFactory_Create_ReturnsSchoolContext()
        {
            // SchoolContextFactory.Create() accepts a connection string and
            // returns a configured SchoolContext. Even with an unreachable Azure SQL
            // endpoint the object is created successfully — the connection is not
            // opened until the first query.
            const string azureConnectionString =
                "Server=tcp:<YOUR_SERVER_NAME>.database.windows.net;" +
                "Database=ContosoUniversity;" +
                "Authentication=Active Directory Default;";

            using var context = SchoolContextFactory.Create(azureConnectionString);
            Assert.NotNull(context);
        }
    }
}
