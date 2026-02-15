using System.Security.Cryptography;
using System.Text;

namespace DonutMS.Services;

public interface IEncryptionService
{
    string Protect(string? plainText);
    string Unprotect(string? protectedText);
}

public class EncryptionService : IEncryptionService
{
    private const string Prefix = "enc:";

    public string Protect(string? plainText)
    {
        if (string.IsNullOrWhiteSpace(plainText))
            return string.Empty;

        if (plainText.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
            return plainText;

        var bytes = Encoding.UTF8.GetBytes(plainText);
        var protectedBytes = ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser);
        return Prefix + Convert.ToBase64String(protectedBytes);
    }

    public string Unprotect(string? protectedText)
    {
        if (string.IsNullOrWhiteSpace(protectedText))
            return string.Empty;

        if (!protectedText.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
            return protectedText;

        var payload = protectedText.Substring(Prefix.Length);
        var protectedBytes = Convert.FromBase64String(payload);
        var bytes = ProtectedData.Unprotect(protectedBytes, null, DataProtectionScope.CurrentUser);
        return Encoding.UTF8.GetString(bytes);
    }
}
