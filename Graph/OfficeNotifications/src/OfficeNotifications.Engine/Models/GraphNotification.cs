using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OfficeNotifications.Engine.Models
{
    public class GraphNotification
    {
        [JsonPropertyName("value")]
        public List<ChangeNotificationForUserId> Notifications { get; set; } = new List<ChangeNotificationForUserId>();

        [JsonIgnore]
        public bool IsValid => Notifications.Any() && Notifications.Where(n=> n.IsValid).Count() == Notifications.Count;
    }

    /// <summary>
    /// Base Graph implementation
    /// </summary>
    public class ChangeNotification
    {
        [JsonPropertyName("subscriptionId")]
        public Guid SubscriptionId { get; set; }

        [JsonPropertyName("clientState")]
        public string ClientState { get; set; } = string.Empty;

        [JsonPropertyName("resourceData")]
        public ResourceData? ResourceData { get; set; } = null;

        [JsonPropertyName("tenantId")]
        public Guid TenantId { get; set; } = Guid.Empty;


        [JsonPropertyName("subscriptionExpirationDateTime")]
        public DateTimeOffset SubscriptionExpirationDateTime { get; set; }

        [JsonPropertyName("changeType")]
        public string ChangeType { get; set; } = string.Empty;

        [JsonPropertyName("encryptedContent")]
        public EncryptedGraphResourceDataContent? EncryptedResourceDataContent { get; set; } = null;

        public NotificationContext? NotificationContext
        {
            get 
            {
                NotificationContext? notificationContext = null;
                if (!string.IsNullOrEmpty(ClientState))
                {
                    try
                    {
                        notificationContext = JsonSerializer.Deserialize<NotificationContext>(ClientState);
                    }
                    catch (JsonException)
                    {
                        // Ignore
                    }
                }
                return notificationContext;
            }
        }

        [JsonIgnore]
        public bool IsValid => ResourceData != null && NotificationContext != null;
    }

    /// <summary>
    /// Extracted user ID from context field
    /// </summary>
    public class ChangeNotificationForUserId : ChangeNotification
    {
        private string? _userId = null;
        public string UserId 
        {
            get 
            {
                if (_userId == null)
                {
                    if (!string.IsNullOrEmpty(this.ClientState))
                    {
                        var s = JsonSerializer.Deserialize<NotificationContext>(ClientState);
                        _userId = s?.ForUserId ?? string.Empty;
                    }
                    if (_userId == null)
                    {
                        _userId = string.Empty;
                    }
                }
                return _userId; 
            }
        }
    }
    public class ResourceData
    {
        [JsonPropertyName("@odata.type")]
        public string OdataType { get; set; } = string.Empty;
    }

    public class EncryptedGraphResourceDataContent
    {
        [JsonPropertyName("data")]
        public string Data { get; set; } = string.Empty;

        [JsonPropertyName("dataSignature")]
        public string DataSignature { get; set; } = string.Empty;

        [JsonPropertyName("dataKey")]
        public string DataKey { get; set; } = string.Empty;

        [JsonPropertyName("encryptionCertificateId")]
        public string EncryptionCertificateId { get; set; } = string.Empty;

        [JsonPropertyName("encryptionCertificateThumbprint")]
        public string EncryptionCertificateThumbprint { get; set; } = string.Empty;

        /// <summary>
        /// This process: https://docs.microsoft.com/en-us/graph/webhooks-with-resource-data#decrypt-the-symmetric-key
        /// </summary>
        /// <param name="cert">Cert the Graph subscription was created with</param>
        public string DecryptResourceDataContent(X509Certificate2 cert)
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

            // Initialize with the private key that matches the encryptionCertificateId.
            var encryptedSymmetricKey = Convert.FromBase64String(DataKey);

            // Decrypt using OAEP padding.
            var decryptedSymmetricKey = rsa.Decrypt(encryptedSymmetricKey, RSAEncryptionPadding.OaepSHA1);

            // Can now use decryptedSymmetricKey with the AES algorithm.
            var encryptedPayload = Convert.FromBase64String(Data);
            var expectedSignature = Convert.FromBase64String(DataSignature);

            using (var hmac = new HMACSHA256(decryptedSymmetricKey))
            {
                var actualSignature = hmac.ComputeHash(encryptedPayload);
                if (actualSignature.SequenceEqual(expectedSignature))
                {
                    // Continue with decryption of the encryptedPayload.
                    return DecryptPayload(decryptedSymmetricKey);
                }
                else
                {
                    // Do not attempt to decrypt encryptedPayload. Assume notification payload has been tampered with and investigate.
                    throw new InvalidDataException("Invalid payload");
                }
            }
        }

        private string DecryptPayload(byte[] decryptedSymmetricKey)
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

            var encryptedPayload = Convert.FromBase64String(Data);

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


}
