using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using System.Security.Cryptography.X509Certificates;

namespace CommonUtils
{
    public class AuthUtils
    {
        // Ensure threadsafe
        static SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1);

        private static Dictionary<string, X509Certificate2> _cachedCerts = new ();
        public static async Task<X509Certificate2> RetrieveKeyVaultCertificate(string name, string tenantId, string clientId, string clientSecret, string keyVaultUrl)
        {
            await semaphoreSlim.WaitAsync();
            try
            {
                if (!_cachedCerts.ContainsKey(name))
                {
                    var client = new SecretClient(vaultUri: new Uri(keyVaultUrl), credential: new ClientSecretCredential(tenantId, clientId, clientSecret));

                    var secret = await client.GetSecretAsync(name);

                    _cachedCerts.Add(name, new X509Certificate2(Convert.FromBase64String(secret.Value.Value)));
                }

            }
            finally
            {
                semaphoreSlim.Release();    
            }
            
            return _cachedCerts[name];

        }
    }
}
