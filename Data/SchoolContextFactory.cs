using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace ContosoUniversity.Data
{
    /// <summary>
    /// Design-time factory for EF Core migrations against Azure SQL Database.
    /// When running migrations (dotnet ef migrations add / database update), EF Core
    /// invokes this factory so it can build a SchoolContext without a running app host.
    /// The factory reads the connection string from appsettings.json, which is expected
    /// to contain an Azure SQL Database connection using Managed Identity
    /// (Authentication=Active Directory Default).
    /// </summary>
    public class SchoolContextFactory : IDesignTimeDbContextFactory<SchoolContext>
    {
        public SchoolContext CreateDbContext(string[] args)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false)
                .AddJsonFile("appsettings.Development.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            var connectionString = config.GetConnectionString("DefaultConnection");

            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException(
                    "Connection string 'DefaultConnection' was not found in configuration. " +
                    "Ensure appsettings.json contains a valid Azure SQL Database connection string " +
                    "using Managed Identity (Authentication=Active Directory Default).");
            }

            var optionsBuilder = new DbContextOptionsBuilder<SchoolContext>();
            optionsBuilder.UseSqlServer(connectionString);

            return new SchoolContext(optionsBuilder.Options);
        }

        /// <summary>
        /// Creates a SchoolContext from an explicit connection string.
        /// Provided as a convenience for testing and programmatic usage.
        /// </summary>
        public static SchoolContext Create(string connectionString)
        {
            var optionsBuilder = new DbContextOptionsBuilder<SchoolContext>();
            optionsBuilder.UseSqlServer(connectionString);
            return new SchoolContext(optionsBuilder.Options);
        }
    }
}
