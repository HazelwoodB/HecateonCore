namespace Hecateon.Core.Security;

using System.Security.Cryptography;
using System.Text;

/// <summary>
/// Provides local-first, privacy-first encryption for sensitive data.
/// Uses AES-256-GCM for authenticated encryption.
/// </summary>
public class EncryptionService
{
    private readonly byte[] _masterKey;
    private const int KeySizeBytes = 32; // AES-256
    private const int NonceSizeBytes = 12; // GCM nonce
    private const int TagSizeBytes = 16; // GCM tag

    public EncryptionService(string? masterKey = null)
    {
        // In production, load from secure key store (Windows DPAPI, Azure Key Vault, etc.)
        _masterKey = masterKey != null
            ? Encoding.UTF8.GetBytes(masterKey).Take(KeySizeBytes).Concat(new byte[KeySizeBytes]).Take(KeySizeBytes).ToArray()
            : GenerateRandomKey();
    }

    /// <summary>
    /// Encrypt plaintext with authenticated encryption (AES-GCM).
    /// </summary>
    public string Encrypt(string plaintext)
    {
        using var aes = new AesGcm(_masterKey, TagSizeBytes);
        var nonce = new byte[NonceSizeBytes];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(nonce);
        }

        var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
        var ciphertextBytes = new byte[plaintextBytes.Length];
        var tag = new byte[TagSizeBytes];

        aes.Encrypt(nonce, plaintextBytes, ciphertextBytes, tag);

        // Combine nonce + ciphertext + tag
        var combined = nonce.Concat(ciphertextBytes).Concat(tag).ToArray();
        return Convert.ToBase64String(combined);
    }

    /// <summary>
    /// Decrypt with authenticated verification.
    /// </summary>
    public string? Decrypt(string encryptedData)
    {
        try
        {
            var combined = Convert.FromBase64String(encryptedData);
            if (combined.Length < NonceSizeBytes + TagSizeBytes)
                return null;

            var nonce = combined.Take(NonceSizeBytes).ToArray();
            var ciphertext = combined.Skip(NonceSizeBytes).Take(combined.Length - NonceSizeBytes - TagSizeBytes).ToArray();
            var tag = combined.Skip(combined.Length - TagSizeBytes).ToArray();

            using var aes = new AesGcm(_masterKey, TagSizeBytes);
            var plaintext = new byte[ciphertext.Length];
            aes.Decrypt(nonce, ciphertext, tag, plaintext);
            return Encoding.UTF8.GetString(plaintext);
        }
        catch
        {
            return null; // Authentication failed or corrupted data
        }
    }

    private static byte[] GenerateRandomKey()
    {
        var key = new byte[KeySizeBytes];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(key);
        }
        return key;
    }

    /// <summary>
    /// Hash a string using PBKDF2 (for passwords, recovery codes).
    /// </summary>
    public static string HashPassword(string password)
    {
        var salt = new byte[16];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(salt);
        }

        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, 10000, HashAlgorithmName.SHA256, 32);
        return $"{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    /// <summary>
    /// Verify a password against a hash.
    /// </summary>
    public static bool VerifyPassword(string password, string hash)
    {
        try
        {
            var parts = hash.Split('.');
            if (parts.Length != 2) return false;

            var salt = Convert.FromBase64String(parts[0]);
            var storedHash = Convert.FromBase64String(parts[1]);

            var computedHash = Rfc2898DeriveBytes.Pbkdf2(password, salt, 10000, HashAlgorithmName.SHA256, 32);
            return computedHash.SequenceEqual(storedHash);
        }
        catch
        {
            return false;
        }
    }
}
