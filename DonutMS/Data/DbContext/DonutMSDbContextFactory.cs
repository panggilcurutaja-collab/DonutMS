using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace DonutMS.Data.DbContext;

public class DonutMSDbContextFactory : IDesignTimeDbContextFactory<DonutMSDbContext>
{
    public DonutMSDbContext CreateDbContext(string[] args)
    {
        var basePath = Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory) ?? Directory.GetCurrentDirectory();

        var configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production"}.json", optional: true)
            .Build();

        var connectionString = configuration.GetConnectionString("Default") ?? "Data Source=donutms.db";

        var optionsBuilder = new DbContextOptionsBuilder<DonutMSDbContext>()
            .UseSqlite(connectionString);

        return new DonutMSDbContext(optionsBuilder.Options);
    }
}
