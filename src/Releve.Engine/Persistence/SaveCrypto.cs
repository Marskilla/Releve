using System.Security.Cryptography;
using System.Text;

namespace Releve.Engine.Persistence;

/// <summary>
/// Chiffrement AES-256-GCM de la sauvegarde : nonce aléatoire de 12 octets et tag de 16 octets
/// stockés avec le contenu. Obfuscation contre l'édition manuelle, pas une protection cryptographique.
/// </summary>
public static class SaveCrypto
{
    public const int NonceSize = 12;
    public const int TagSize = 16;

    // Secret constant embarque dans l'application, dont la cle est derivee.
    private const string EmbeddedSecret = "Releve — sauvegarde locale (obfuscation, pas une protection)";

    private static readonly byte[] Key = SHA256.HashData(Encoding.UTF8.GetBytes(EmbeddedSecret));

    public static byte[] Encrypt(byte[] plain)
    {
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var cipher = new byte[plain.Length];
        var tag = new byte[TagSize];

        using var aes = new AesGcm(Key, TagSize);
        aes.Encrypt(nonce, plain, cipher, tag);

        // [nonce][tag][contenu chiffre]
        var payload = new byte[NonceSize + TagSize + cipher.Length];
        nonce.CopyTo(payload, 0);
        tag.CopyTo(payload, NonceSize);
        cipher.CopyTo(payload, NonceSize + TagSize);
        return payload;
    }

    public static byte[]? Decrypt(byte[] payload)
    {
        if (payload.Length < NonceSize + TagSize)
        {
            return null;
        }

        var nonce = payload.AsSpan(0, NonceSize);
        var tag = payload.AsSpan(NonceSize, TagSize);
        var cipher = payload.AsSpan(NonceSize + TagSize);
        var plain = new byte[cipher.Length];

        try
        {
            using var aes = new AesGcm(Key, TagSize);
            aes.Decrypt(nonce, cipher, tag, plain);
            return plain;
        }
        catch (CryptographicException)
        {
            return null; // contenu altere ou mauvaise cle
        }
    }
}
