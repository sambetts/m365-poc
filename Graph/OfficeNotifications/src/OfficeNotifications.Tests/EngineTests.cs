using Microsoft.VisualStudio.TestTools.UnitTesting;
using OfficeNotifications.Engine;
using System.Threading.Tasks;
using Microsoft.Graph;
using System.Text.Json;
using System.Linq;

namespace OfficeNotifications.Tests
{
    [TestClass]
    public class EngineTests : AbstractTest
    {
        [TestMethod]
        public async Task NotificationCountManagerTests()
        {
            var manager = await UserNotificationsManager.GetNotificationManager("webhooks", _config!, _tracer);

            await manager.ClearNotifications(_config!.TestUserId);
            Assert.IsTrue((await manager.GetNotifications(_config!.TestUserId)).Count() == 0);

            // Chat message test
            var c = new ChatMessage
            {
                From = new ChatMessageFromIdentitySet { User = new Identity { Id = _config!.TestUserId  } }
            };
            await manager.ProcessWebhookMessage(new Engine.Models.ChangeNotificationForUserId
            {
                ClientState = "{\"ForUserId\":\"" + _config!.TestUserId + "\"}",
                ResourceData = new Engine.Models.ResourceData { OdataType = "#Microsoft.Graph.chatMessage" }
            }, JsonSerializer.Serialize(c));

            Assert.IsTrue((await manager.GetNotifications(_config!.TestUserId)).Count() == 1);

            // Email test
            var m = new Message
            {
                From = new Recipient { EmailAddress = new EmailAddress { Address = "testing@unittesting.local", Name = "Testing User" } }
            };
            await manager.ProcessWebhookMessage(new Engine.Models.ChangeNotificationForUserId
            {
                ClientState = "{\"ForUserId\":\"" + _config!.TestUserId + "\"}",
                ResourceData = new Engine.Models.ResourceData { OdataType = "#microsoft.graph.message" }
            }, JsonSerializer.Serialize(m));
            Assert.IsTrue((await manager.GetNotifications(_config!.TestUserId)).Count() == 2);

            // Double-check clear works
            await manager.ClearNotifications(_config!.TestUserId);
            Assert.IsTrue((await manager.GetNotifications(_config!.TestUserId)).Count() == 0);
        }
    }
}
