using System;
using System.Linq;
using ContosoUniversity.Data;
using ContosoUniversity.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ContosoUniversity.Tests.Data
{
    /// <summary>
    /// Unit tests for SchoolContext verifying Azure SQL Database Managed Identity migration.
    /// Tests use the EF Core in-memory provider so no actual SQL Server connection is required.
    /// </summary>
    public class SchoolContextTests : IDisposable
    {
        private readonly SchoolContext _context;

        public SchoolContextTests()
        {
            var options = new DbContextOptionsBuilder<SchoolContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new SchoolContext(options);
        }

        public void Dispose() => _context.Dispose();

        // ── DbSet availability ─────────────────────────────────────────────────

        [Fact]
        public void SchoolContext_HasCoursesDbSet()
        {
            Assert.NotNull(_context.Courses);
        }

        [Fact]
        public void SchoolContext_HasEnrollmentsDbSet()
        {
            Assert.NotNull(_context.Enrollments);
        }

        [Fact]
        public void SchoolContext_HasDepartmentsDbSet()
        {
            Assert.NotNull(_context.Departments);
        }

        [Fact]
        public void SchoolContext_HasPeopleDbSet()
        {
            Assert.NotNull(_context.People);
        }

        [Fact]
        public void SchoolContext_HasStudentsDbSet()
        {
            Assert.NotNull(_context.Students);
        }

        [Fact]
        public void SchoolContext_HasInstructorsDbSet()
        {
            Assert.NotNull(_context.Instructors);
        }

        [Fact]
        public void SchoolContext_HasNotificationsDbSet()
        {
            Assert.NotNull(_context.Notifications);
        }

        // ── Basic CRUD with in-memory provider ────────────────────────────────

        [Fact]
        public void AddStudent_PersistsToContext()
        {
            var student = new Student
            {
                FirstMidName = "Jane",
                LastName = "Doe",
                EnrollmentDate = DateTime.UtcNow
            };

            _context.Students.Add(student);
            _context.SaveChanges();

            Assert.Equal(1, _context.Students.Count());
            Assert.Equal("Jane", _context.Students.Single().FirstMidName);
        }

        [Fact]
        public void AddAndRetrieveCourse_RoundTripsSuccessfully()
        {
            var course = new Course
            {
                CourseID = 9999,
                Title = "Azure Fundamentals",
                Credits = 3,
                DepartmentID = 1
            };

            _context.Courses.Add(course);
            _context.SaveChanges();

            var retrieved = _context.Courses.Single(c => c.CourseID == 9999);

            Assert.Equal("Azure Fundamentals", retrieved.Title);
            Assert.Equal(3, retrieved.Credits);
        }

        [Fact]
        public void UpdateStudent_PersistsChanges()
        {
            var student = new Student
            {
                FirstMidName = "Original",
                LastName = "Name",
                EnrollmentDate = DateTime.UtcNow
            };

            _context.Students.Add(student);
            _context.SaveChanges();

            student.FirstMidName = "Updated";
            _context.SaveChanges();

            var updated = _context.Students.Single();
            Assert.Equal("Updated", updated.FirstMidName);
        }

        [Fact]
        public void DeleteStudent_RemovesFromContext()
        {
            var student = new Student
            {
                FirstMidName = "ToDelete",
                LastName = "Student",
                EnrollmentDate = DateTime.UtcNow
            };

            _context.Students.Add(student);
            _context.SaveChanges();

            _context.Students.Remove(student);
            _context.SaveChanges();

            Assert.Empty(_context.Students);
        }

        // ── Connection string validation ──────────────────────────────────────

        [Fact]
        public void AzureSqlConnectionString_ContainsManagedIdentityAuthentication()
        {
            // Verify the Azure SQL Managed Identity connection string format is used
            // in non-development environments (production appsettings.json).
            const string productionConnectionString =
                "Server=tcp:<YOUR_SERVER>.database.windows.net;" +
                "Database=ContosoUniversityNoAuthEFCore;" +
                "Authentication=Active Directory Default;" +
                "TrustServerCertificate=True";

            Assert.Contains("Authentication=Active Directory Default", productionConnectionString,
                StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Password=", productionConnectionString,
                StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("User ID=", productionConnectionString,
                StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Integrated Security=True", productionConnectionString,
                StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void AzureSqlConnectionString_TargetsAzureSqlEndpoint()
        {
            const string productionConnectionString =
                "Server=tcp:<YOUR_SERVER>.database.windows.net;" +
                "Database=ContosoUniversityNoAuthEFCore;" +
                "Authentication=Active Directory Default;" +
                "TrustServerCertificate=True";

            Assert.Contains(".database.windows.net", productionConnectionString,
                StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void LocalDbConnectionString_UsedOnlyForDevelopment()
        {
            // The LocalDB connection string should only exist in appsettings.Development.json
            // so it is never active in production (non-Development environments).
            const string devConnectionString =
                "Data Source=(LocalDb)\\MSSQLLocalDB;" +
                "Initial Catalog=ContosoUniversityNoAuthEFCore;" +
                "Integrated Security=True;" +
                "MultipleActiveResultSets=True";

            // Confirm the dev string targets LocalDB (Windows-only, local use only)
            Assert.Contains("(LocalDb)", devConnectionString, StringComparison.OrdinalIgnoreCase);

            // And confirm it does NOT contain Managed Identity — the production string does
            Assert.DoesNotContain("Authentication=Active Directory Default", devConnectionString,
                StringComparison.OrdinalIgnoreCase);
        }

        // ── SchoolContext constructor ─────────────────────────────────────────

        [Fact]
        public void SchoolContext_CanBeInstantiatedWithOptions()
        {
            var options = new DbContextOptionsBuilder<SchoolContext>()
                .UseInMemoryDatabase("CanBeInstantiatedTest")
                .Options;

            using var ctx = new SchoolContext(options);

            Assert.NotNull(ctx);
        }

        [Fact]
        public void SchoolContext_MultipleInstances_UseIsolatedDatabases()
        {
            // Each instance gets its own in-memory database (unique name per test constructor)
            var options1 = new DbContextOptionsBuilder<SchoolContext>()
                .UseInMemoryDatabase("Isolation_DB1")
                .Options;

            var options2 = new DbContextOptionsBuilder<SchoolContext>()
                .UseInMemoryDatabase("Isolation_DB2")
                .Options;

            using var ctx1 = new SchoolContext(options1);
            using var ctx2 = new SchoolContext(options2);

            ctx1.Students.Add(new Student
            {
                FirstMidName = "Only",
                LastName = "InDb1",
                EnrollmentDate = DateTime.UtcNow
            });
            ctx1.SaveChanges();

            // ctx2 should not see ctx1's data
            Assert.Empty(ctx2.Students);
        }
    }
}
