using System.Security.Cryptography;

namespace DonutMS.Core.Utils;

public static class PasswordHasher
{
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int Iterations = 10000;

    public static (byte[] hash, byte[] salt) CreateHash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            Iterations,
            HashAlgorithmName.SHA256,
            HashSize);

        return (hash, salt);
    }

    public static bool VerifyHash(string password, byte[]? storedHash, byte[]? storedSalt)
    {
        if (storedHash == null || storedSalt == null)
            return false;

        var computed = Rfc2898DeriveBytes.Pbkdf2(
            password,
            storedSalt,
            Iterations,
            HashAlgorithmName.SHA256,
            HashSize);

        return CryptographicOperations.FixedTimeEquals(computed, storedHash);
    }
}
