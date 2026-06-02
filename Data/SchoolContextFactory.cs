using Microsoft.EntityFrameworkCore;

namespace ContosoUniversity.Data
{
    /// <summary>
    /// Legacy factory - kept for reference. In ASP.NET Core the SchoolContext
    /// is registered via DI in Program.cs and injected into controllers.
    /// </summary>
    public static class SchoolContextFactory
    {
        public static SchoolContext Create(string connectionString)
        {
            var optionsBuilder = new DbContextOptionsBuilder<SchoolContext>();
            optionsBuilder.UseSqlServer(connectionString);
            return new SchoolContext(optionsBuilder.Options);
        }
    }
}
