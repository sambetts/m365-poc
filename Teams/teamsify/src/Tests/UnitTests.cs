using Microsoft.Extensions.Configuration;
using Web;
using Web.Config;
using Web.Models;

namespace Tests
{
    public class UnitTests
    {
        private WebAppConfig _config;

        [SetUp]
        public void GetConfig()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(System.IO.Directory.GetCurrentDirectory())
                .AddEnvironmentVariables()
                .AddJsonFile("appsettings.json", true);


            var config = builder.Build();
            _config = new WebAppConfig(config);

        }

        const string URL = "https://contoso.com";

        [Test]
        public void AppDetailsIsValid()
        {
            Assert.IsFalse(new AppDetails().IsValid);
            Assert.IsTrue(GetAppDetails().IsValid);
        }

        [Test]
        public void ToTeamsAppManifestTests()
        {
            var manifest = GetAppDetails().ToTeamsAppManifest(URL);
            Assert.IsTrue(manifest.PackageName.Length > 0 && manifest.PackageName.IndexOf(" ") == -1);
            Assert.IsTrue(manifest.StaticTabs.Count == 1);
        }

        [Test]
        public async Task UserSessionTests()
        {
            var connectionString = _config.ConnectionStrings.Storage;
            var client = new UserSessionTableClient(connectionString);
            var newSession = await UserSession.AddNewSessionToAzTable(client);

            Assert.IsNotNull(newSession.RowKey);

            newSession.SavedManifestUrl = URL;

            await newSession.UpdateTableRecord(client);
            var newSession2 = await UserSession.GetSessionFromAzTable(newSession.RowKey, client);

            // Make sure we get null for a random ID
            var nullSession = await UserSession.GetSessionFromAzTable(Guid.NewGuid().ToString(), client);
            Assert.IsNull(nullSession);

            Assert.IsTrue(newSession2.SavedManifestUrl == URL);
        }

        [Test]
        public void GenerateZipBytes()
        {
            var a = GetAppDetails();
            var bytes = a.ToTeamsAppManifest(URL).BuildZip("bob.zip");

            Assert.IsNotNull(bytes);
        }

        AppDetails GetAppDetails()
        {
            return new AppDetails()
            {
                CompanyName = "Testing",
                CompanyWebsite = URL,
                LongDescription = "Testing long",
                ShortDescription = "Test",
                ShortName = "Test",
                LongName = "Test"
            };
        }
    }
}