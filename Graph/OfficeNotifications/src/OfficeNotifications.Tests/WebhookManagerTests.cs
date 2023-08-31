using CommonUtils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OfficeNotifications.Engine;
using OfficeNotifications.Engine.Webhooks;
using System.Threading.Tasks;

namespace OfficeNotifications.Tests
{
    [TestClass]
    public class WebhookManagerTests : AbstractTest
    {
        // Won't work without a public endpoint (TestGraphNotificationEndpoint) listening for notifications. 
        [TestMethod]
        public async Task UserChatsWebhooksManagerTests()
        {
            var webhooksManager = await UserChatsWebhooksManager.LoadFromKeyvault<UserChatsWebhooksManager>("webhooks", _config!.TestUserId, _config!, _tracer!);
            await webhooksManager.DeleteWebhooks();
            var noWebHooksValidResult = await webhooksManager.HaveValidSubscription();

            Assert.IsFalse(noWebHooksValidResult);

            await webhooksManager.CreateOrUpdateSubscription();
            var webHooksCreatedValidResult = await webhooksManager.HaveValidSubscription();

            Assert.IsTrue(webHooksCreatedValidResult);
        }

        [TestMethod]
        public async Task UserEmailsWebhooksManagerTests()
        {
            var webhooksManager = await UserBaseWebhooksManager.LoadFromKeyvault<UserEmailsWebhooksManager>("webhooks", _config!.TestUserId, _config!, _tracer!);
            await webhooksManager.DeleteWebhooks();
            var noWebHooksValidResult = await webhooksManager.HaveValidSubscription();

            Assert.IsFalse(noWebHooksValidResult);

            await webhooksManager.CreateOrUpdateSubscription();
            var webHooksCreatedValidResult = await webhooksManager.HaveValidSubscription();

            Assert.IsTrue(webHooksCreatedValidResult);
        }



        [TestMethod]
        public async Task DoubleCreateWebhooksManagerTests()
        {
            var webhooksManager = await UserBaseWebhooksManager.LoadFromKeyvault<UserEmailsWebhooksManager>("webhooks", _config!.TestUserId, _config!, _tracer!);
            

            await webhooksManager.CreateOrUpdateSubscription();
            var webHooksCreatedValidResult = await webhooksManager.HaveValidSubscription();

            Assert.IsTrue(webHooksCreatedValidResult);


            await webhooksManager.CreateOrUpdateSubscription();
            webHooksCreatedValidResult = await webhooksManager.HaveValidSubscription();

            Assert.IsTrue(webHooksCreatedValidResult);
        }

    }
}
