using System.IO;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace DonutMS.Services;

public record StoredCredentials(string Username, DateTime SavedAt);

public interface ICredentialStore
{
    Task SaveAsync(string username);
    Task<StoredCredentials?> LoadAsync();
    Task ClearAsync();
}

public class CredentialStore : ICredentialStore
{
    private readonly IEncryptionService _encryptionService;
    private readonly ILogger<CredentialStore> _logger;
    private readonly string _filePath;

    public CredentialStore(IEncryptionService encryptionService, ILogger<CredentialStore> logger)
    {
        _encryptionService = encryptionService;
        _logger = logger;

        var baseFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "DonutMS");

        _filePath = Path.Combine(baseFolder, "auth.json");
    }

    public async Task SaveAsync(string username)
    {
        var payload = new StoredCredentials(
            _encryptionService.Protect(username),
            DateTime.UtcNow);

        var folder = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrWhiteSpace(folder))
            Directory.CreateDirectory(folder);

        var json = JsonConvert.SerializeObject(payload, Formatting.Indented);
        await File.WriteAllTextAsync(_filePath, json);
        _logger.LogInformation("Saved credentials for auto-login");
    }

    public async Task<StoredCredentials?> LoadAsync()
    {
        if (!File.Exists(_filePath))
            return null;

        try
        {
            var json = await File.ReadAllTextAsync(_filePath);
            var payload = JsonConvert.DeserializeObject<StoredCredentials>(json);
            if (payload == null)
                return null;

            var username = _encryptionService.Unprotect(payload.Username);
            if (string.IsNullOrWhiteSpace(username))
                return null;

            return new StoredCredentials(username, payload.SavedAt);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load stored credentials");
            return null;
        }
    }

    public Task ClearAsync()
    {
        try
        {
            if (File.Exists(_filePath))
                File.Delete(_filePath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to clear stored credentials");
        }

        return Task.CompletedTask;
    }
}
