using Microsoft.VisualStudio.TestTools.UnitTesting;
using SPOAzBlob.Engine;
using System.Threading.Tasks;

namespace SPOAzBlob.Tests
{
    [TestClass]
    public class GraphTests : AbstractTest
    {
        // Won't work without a public endpoint (TestGraphNotificationEndpoint) listening for notifications. 
        [TestMethod]
        public async Task WebhooksManagerTests()
        {
            var webhooksManager = new WebhooksManager(_config!, _tracer, _config!.WebhookUrlOverride);
            await webhooksManager.DeleteWebhooks();
            var noWebHooksValidResult = await webhooksManager.HaveValidSubscription();

            Assert.IsFalse(noWebHooksValidResult);

            await webhooksManager.CreateOrUpdateSubscription();
            var webHooksCreatedValidResult = await webhooksManager.HaveValidSubscription();

            Assert.IsTrue(webHooksCreatedValidResult);
        }

        [TestMethod]
        public async Task UserManagerTests()
        {
            var userManager = new GraphUserManager(_config!, _tracer);
            var user = await userManager.GetUserByEmail(_config!.TestEmailAddress);

            Assert.IsNotNull(user);
        }
    }
}
