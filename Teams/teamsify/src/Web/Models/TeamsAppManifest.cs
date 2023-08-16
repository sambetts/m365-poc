using Newtonsoft.Json;
using System.Drawing;
using System.IO.Compression;

namespace Web.Models
{

    // https://docs.microsoft.com/en-us/microsoftteams/platform/resources/schema/manifest-schema
    public class TeamsAppManifest
    {

        #region Props

        [JsonProperty("$schema")]
        public string Schema => "https://developer.microsoft.com/en-us/json-schemas/teams/v1.11/MicrosoftTeams.schema.json";

        [JsonProperty("manifestVersion")]
        public string ManifestVersion => "1.11";

        [JsonProperty("version")]
        public string Version => "1";

        [JsonProperty("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [JsonProperty("packageName")]
        public string PackageName { get; set; } = String.Empty;

        [JsonProperty("developer")]
        public AppDeveloper Developer { get; set; } = new AppDeveloper();

        [JsonProperty("icons")]
        public AppIcons Icons { get; set; } = new AppIcons();

        [JsonProperty("name")]
        public AppName Name { get; set; } = new AppName();

        [JsonProperty("description")]
        public AppDescription Description { get; set; } = new AppDescription();

        [JsonProperty("accentColor")]
        public string AccentColor => "#FFFFFF";

        [JsonProperty("staticTabs")]
        public List<AppStaticTab> StaticTabs { get; set; } = new List<AppStaticTab>();

        [JsonProperty("permissions")]
        public List<string> Permissions { get; set; } = new List<string>();

        [JsonProperty("validDomains")]
        public List<string> ValidDomains { get; set; } = new List<string>();

        #endregion

        #region Classes

        public class AppDescription
        {
            [JsonProperty("short")]
            public string Short { get; set; } = String.Empty;

            [JsonProperty("full")]
            public string Full { get; set; } = String.Empty;
        }

        public class AppDeveloper
        {
            [JsonProperty("name")]
            public string Name { get; set; } = String.Empty;

            [JsonProperty("websiteUrl")]
            public string WebsiteUrl { get; set; } = String.Empty;

            [JsonProperty("privacyUrl")]
            public string PrivacyUrl { get; set; } = String.Empty;

            [JsonProperty("termsOfUseUrl")]
            public string TermsOfUseUrl { get; set; } = String.Empty;
        }

        public class AppIcons
        {
            [JsonProperty("color")]
            public string Color => "color.png";

            [JsonProperty("outline")]
            public string Outline => "outline.png";
        }

        public class AppName
        {
            [JsonProperty("short")]
            public string Short { get; set; } = String.Empty;

            [JsonProperty("full")]
            public string Full { get; set; } = String.Empty;
        }

        public class AppStaticTab
        {
            [JsonProperty("entityId")]
            public string EntityId { get; set; } = String.Empty;

            [JsonProperty("name")]
            public string Name { get; set; } = String.Empty;

            [JsonProperty("contentUrl")]
            public string ContentUrl { get; set; } = String.Empty;

            [JsonProperty("websiteUrl")]
            public string WebsiteUrl { get; set; } = String.Empty;

            [JsonProperty("scopes")]
            public List<string> Scopes => new List<string>() { "personal" };
        }
        #endregion


        /// <summary>
        /// Build a Teams app zip file
        /// </summary>
        public byte[] BuildZip(string zipFilenameMinusExtension)
        {
            // Prep temp dir
            var tempRootPath = $"{Path.GetTempPath()}Teamsify\\{DateTime.Now.Ticks}";
            var tempManifestPath = $"{tempRootPath}\\manifest";
            Directory.CreateDirectory(tempManifestPath);

            // Write app contents
            File.WriteAllText($"{tempManifestPath}\\manifest.json", JsonConvert.SerializeObject(this));
            File.WriteAllBytes($"{tempManifestPath}\\color.png", ImageToByte(Properties.Resources.globe));
            File.WriteAllBytes($"{tempManifestPath}\\outline.png", ImageToByte(Properties.Resources.globe32));

            // Build zip
            var zipFileName = $"{tempRootPath}\\{zipFilenameMinusExtension}.zip";
            ZipFile.CreateFromDirectory(tempManifestPath, zipFileName);

            var zipBytes = File.ReadAllBytes(zipFileName);

            // Try and clean
            try
            {
                Directory.Delete(tempRootPath, true);
            }
            catch (IOException)
            {
                // Ignore
            }

            return zipBytes;
        }
        static byte[] ImageToByte(Image img)
        {
            var converter = new ImageConverter();
            return (byte[])converter.ConvertTo(img, typeof(byte[]));
        }

    }
}
