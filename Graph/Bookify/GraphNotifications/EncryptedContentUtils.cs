using Microsoft.Graph.Models;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace GraphNotifications;

public static class EncryptedContentUtils
{

    /// <summary>
    /// This process: https://docs.microsoft.com/en-us/graph/webhooks-with-resource-data#decrypt-the-symmetric-key
    /// </summary>
    /// <param name="cert">Cert the Graph subscription was created with</param>
    public static string DecryptResourceDataContent(ChangeNotificationEncryptedContent encryptedResourceDataContent, X509Certificate2 cert)
    {
        // https://www.pkisolutions.com/accessing-and-using-certificate-private-keys-in-net-framework-net-core/
        const string RSA = "1.2.840.113549.1.1.1";
        RSA rsa;
        switch (cert.PublicKey.Oid.Value)
        {
            case RSA:
                rsa = cert.GetRSAPrivateKey() ?? throw new ArgumentOutOfRangeException(nameof(cert), "No private key");
                break;

            default:
                throw new NotSupportedException("Unsupported algorithm group");
        }

        if (encryptedResourceDataContent.DataKey == null)
        {
            throw new InvalidDataException("No DataKey in encrypted content");
        }
        if (encryptedResourceDataContent.Data == null)
        {
            throw new InvalidDataException("No Data in encrypted content");
        }
        if (encryptedResourceDataContent.DataSignature == null)
        {
            throw new InvalidDataException("No DataSignature in encrypted content");
        }

        // Initialize with the private key that matches the encryptionCertificateId.
        var encryptedSymmetricKey = Convert.FromBase64String(encryptedResourceDataContent.DataKey);

        // Decrypt using OAEP padding.
        var decryptedSymmetricKey = rsa.Decrypt(encryptedSymmetricKey, RSAEncryptionPadding.OaepSHA1);

        // Can now use decryptedSymmetricKey with the AES algorithm.
        var encryptedPayload = Convert.FromBase64String(encryptedResourceDataContent.Data);
        var expectedSignature = Convert.FromBase64String(encryptedResourceDataContent.DataSignature);

        using (var hmac = new HMACSHA256(decryptedSymmetricKey))
        {
            var actualSignature = hmac.ComputeHash(encryptedPayload);
            if (actualSignature.SequenceEqual(expectedSignature))
            {
                // Continue with decryption of the encryptedPayload.
                return DecryptPayload(encryptedResourceDataContent.Data, decryptedSymmetricKey);
            }
            else
            {
                // Do not attempt to decrypt encryptedPayload. Assume notification payload has been tampered with and investigate.
                throw new InvalidDataException("Invalid payload");
            }
        }
    }

    private static string DecryptPayload(string encryptedResourceDataContentData, byte[] decryptedSymmetricKey)
    {
        var aesProvider = Aes.Create();
        aesProvider.Key = decryptedSymmetricKey;
        aesProvider.Padding = PaddingMode.PKCS7;
        aesProvider.Mode = CipherMode.CBC;

        // Obtain the intialization vector from the symmetric key itself.
        int vectorSize = 16;
        var iv = new byte[vectorSize];
        Array.Copy(decryptedSymmetricKey, iv, vectorSize);
        aesProvider.IV = iv;

        var encryptedPayload = Convert.FromBase64String(encryptedResourceDataContentData);

        // Decrypt the resource data content.
        using (var decryptor = aesProvider.CreateDecryptor())
        {
            using (var msDecrypt = new MemoryStream(encryptedPayload))
            {
                using (var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                {
                    using (var srDecrypt = new StreamReader(csDecrypt))
                    {
                        return srDecrypt.ReadToEnd();
                    }
                }
            }
        }
    }
}
