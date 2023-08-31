using Microsoft.VisualStudio.TestTools.UnitTesting;
using OfficeNotifications.Engine.Webhooks;
using OfficeNotifications.Engine;
using System.Threading.Tasks;

namespace OfficeNotifications.Tests
{
    [TestClass]
    public class GraphTests : AbstractTest
    {
        [TestMethod]
        public async Task UserManagerTests()
        {
            var userManager = new GraphUserManager(_config!, _tracer!);
            var user = await userManager.GetUserByEmail(_config!.TestEmailAddress);

            Assert.IsNotNull(user);
        }

    }
}
