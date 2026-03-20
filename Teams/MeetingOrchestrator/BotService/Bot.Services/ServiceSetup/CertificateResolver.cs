using System;
using System.Security.Cryptography.X509Certificates;

namespace Bot.Services.ServiceSetup
{
    /// <summary>
    /// Resolves X.509 certificates from the local machine certificate store.
    /// </summary>
    internal static class CertificateResolver
    {
        /// <summary>
        /// Searches the local machine store for a certificate matching the given thumbprint.
        /// </summary>
        /// <param name="thumbprint">The certificate thumbprint.</param>
        /// <returns>The matching <see cref="X509Certificate2"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="thumbprint"/> is null or empty.</exception>
        /// <exception cref="InvalidOperationException">Thrown when no matching certificate is found.</exception>
        public static X509Certificate2 GetFromStore(string thumbprint)
        {
            if (string.IsNullOrEmpty(thumbprint))
            {
                throw new ArgumentNullException(nameof(thumbprint), "No certificate thumbprint found");
            }

            X509Store store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadOnly);
            try
            {
                X509Certificate2Collection certs = store.Certificates.Find(X509FindType.FindByThumbprint, thumbprint, validOnly: false);

                if (certs.Count != 1)
                {
                    throw new InvalidOperationException($"No certificate with thumbprint {thumbprint} was found in the machine store.");
                }

                return certs[0];
            }
            finally
            {
                store.Close();
            }
        }
    }
}
