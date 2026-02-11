using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DonutMS.Services;

public interface IBackupService
{
    Task<string> CreateBackupAsync(string? targetFolder = null);
    Task<string?> EnsureDailyBackupAsync(string? targetFolder = null);
}

public class BackupService : IBackupService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<BackupService> _logger;

    public BackupService(IConfiguration configuration, ILogger<BackupService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<string> CreateBackupAsync(string? targetFolder = null)
    {
        var dbPath = ResolveDatabasePath();

        if (!File.Exists(dbPath))
            throw new FileNotFoundException("Database file not found", dbPath);

        var backupFolder = ResolveBackupFolder(targetFolder);

        var fileName = $"donutms_backup_{DateTime.Now:yyyyMMdd_HHmmss}.db";
        var backupPath = Path.Combine(backupFolder, fileName);

        await Task.Run(() => File.Copy(dbPath, backupPath, true));
        _logger.LogInformation("Backup created: {Path}", backupPath);
        return backupPath;
    }

    public async Task<string?> EnsureDailyBackupAsync(string? targetFolder = null)
    {
        var backupFolder = ResolveBackupFolder(targetFolder);

        var latestBackup = Directory.EnumerateFiles(backupFolder, "donutms_backup_*.db")
            .Select(path => new FileInfo(path))
            .OrderByDescending(info => info.CreationTime)
            .FirstOrDefault();

        if (latestBackup != null && latestBackup.CreationTime.Date == DateTime.Today)
        {
            _logger.LogInformation("Daily backup already exists ({Backup}). Skipping auto backup.", latestBackup.FullName);
            return null;
        }

        return await CreateBackupAsync(backupFolder);
    }

    private string ResolveDatabasePath()
    {
        var connectionString = _configuration.GetConnectionString("Default")
            ?? _configuration.GetSection("Database")["ConnectionString"]
            ?? "Data Source=donutms.db";

        var dbPath = ExtractSqlitePath(connectionString);
        if (!Path.IsPathRooted(dbPath))
        {
            dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, dbPath);
        }

        return dbPath;
    }

    private static string ResolveBackupFolder(string? targetFolder)
    {
        var backupFolder = targetFolder ??
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "DonutMS", "Backups");
        Directory.CreateDirectory(backupFolder);
        return backupFolder;
    }

    private static string ExtractSqlitePath(string connectionString)
    {
        var marker = "Data Source=";
        var idx = connectionString.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (idx < 0)
            return connectionString;

        var pathPart = connectionString.Substring(idx + marker.Length).Trim();
        var semicolonIdx = pathPart.IndexOf(';');
        if (semicolonIdx > -1)
            pathPart = pathPart.Substring(0, semicolonIdx);

        return pathPart.Trim().Trim('"');
    }
}
