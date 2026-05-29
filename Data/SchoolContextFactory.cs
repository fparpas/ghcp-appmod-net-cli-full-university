using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace ContosoUniversity.Data
{
    public static class SchoolContextFactory
    {
        // Used by EF Core migrations tooling — reads from appsettings.json
        public static SchoolContext Create()
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false)
                .AddJsonFile("appsettings.Development.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            var connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

            var optionsBuilder = new DbContextOptionsBuilder<SchoolContext>();
            optionsBuilder.UseSqlServer(connectionString);

            return new SchoolContext(optionsBuilder.Options);
        }
    }
}

