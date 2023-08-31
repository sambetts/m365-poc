using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Security.Cryptography.X509Certificates;

namespace OfficeNotifications.Tests
{
    [TestClass]
    public class DecryptionTests : AbstractTest
    {
        /// <summary>
        /// Tests we can decrypt a Graph notification resource data content from encrypted payload.
        /// Needs a real Graph response made from a real subscription create with a real X509Certificate2 certificate.
        /// Both set in appsettings
        /// </summary>
        [TestMethod]
        public void DecryptNotificationResourceDataMessage()
        {
            var testCert = new X509Certificate2(Convert.FromBase64String(_config!.TestX509Certificate2));
            var r = _config.TestMessageNotification.ToEncryptedContent().DecryptResourceDataContent(testCert);

            Assert.IsFalse(string.IsNullOrEmpty(r));
        }

    }
}
