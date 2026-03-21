
using Bot.Model.Constants;
using Bot.Services.Contract;
using Bot.Services.Util;
using Microsoft.Skype.Bots.Media;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;

namespace Bot.Services.ServiceSetup
{
    /// <summary>
    /// Class AzureSettings.
    /// Implements the <see cref="IAzureSettings" />
    /// </summary>
    /// <seealso cref="IAzureSettings" />
    public class AzureSettings : IAzureSettings
    {
        /// <summary>
        /// Gets or sets the name of the bot.
        /// </summary>
        /// <value>The name of the bot.</value>
        public string BotName { get; set; }

        /// <summary>
        /// Gets or sets the name of the service DNS.
        /// </summary>
        /// <value>The name of the service DNS.</value>
        public string ServiceDnsName { get; set; }

        /// <summary>
        /// Gets or sets the service cname.
        /// </summary>
        /// <value>The service cname.</value>
        public string ServiceCname { get; set; }

        /// <summary>
        /// Gets or sets the certificate thumbprint.
        /// </summary>
        /// <value>The certificate thumbprint.</value>
        public string CertificateThumbprint { get; set; }

        /// <summary>
        /// Gets or sets the call control listening urls.
        /// </summary>
        /// <value>The call control listening urls.</value>
        public IEnumerable<string> CallControlListeningUrls { get; set; }

        /// <summary>
        /// Gets or sets the call control base URL.
        /// </summary>
        /// <value>The call control base URL.</value>
        public Uri CallControlBaseUrl { get; set; }

        /// <summary>
        /// Gets or sets the place call endpoint URL.
        /// </summary>
        /// <value>The place call endpoint URL.</value>
        public Uri PlaceCallEndpointUrl { get; set; }

        /// <summary>
        /// Gets the media platform settings.
        /// </summary>
        /// <value>The media platform settings.</value>
        public MediaPlatformSettings MediaPlatformSettings { get; private set; }

        /// <summary>
        /// Gets or sets the aad application identifier.
        /// </summary>
        /// <value>The aad application identifier.</value>
        public string AadAppId { get; set; }

        public string AadTenantId { get; set; }


        /// <summary>
        /// Gets or sets the aad application secret.
        /// </summary>
        /// <value>The aad application secret.</value>
        public string AadAppSecret { get; set; }

        /// <summary>
        /// Gets or sets the instance public port.
        /// </summary>
        /// <value>The instance public port.</value>
        public int InstancePublicPort { get; set; }

        /// <summary>
        /// Gets or sets the instance internal port.
        /// </summary>
        /// <value>The instance internal port.</value>
        public int InstanceInternalPort { get; set; }

        /// <summary>
        /// Gets or sets the call signaling port.
        /// </summary>
        /// <value>The call signaling port.</value>
        public int CallSignalingPort { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [capture events].
        /// </summary>
        /// <value><c>true</c> if [capture events]; otherwise, <c>false</c>.</value>
        public bool CaptureEvents { get; set; } = false;

        /// <summary>
        /// Gets or sets the name of the pod.
        /// </summary>
        /// <value>The name of the pod.</value>
        public string PodName { get; set; }

        /// <summary>
        /// Gets or sets the media folder.
        /// </summary>
        /// <value>The media folder.</value>
        public string MediaFolder { get; set; }

        /// <summary>
        /// Gets or sets the events folder.
        /// </summary>
        /// <value>The events folder.</value>
        public string EventsFolder { get; set; }

        public string ApplicationInsightsKey { get; set; }

        /// <summary>
        /// Gets or sets the audio settings.
        /// </summary>
        /// <value>The audio settings.</value>
        public AudioSettings AudioSettings { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is stereo.
        /// </summary>
        /// <value><c>true</c> if this instance is stereo; otherwise, <c>false</c>.</value>
        public bool IsStereo { get; set; }

        /// <summary>
        /// Gets or sets the wav quality.
        /// </summary>
        /// <value>The wav quality.</value>
        public int WAVQuality { get; set; }

        public string BaseContentDir { get; set; }

        /// <summary>
        /// Gets the h264 1280 x 720 file location.
        /// </summary>
        public string H2641280x720x30FpsFile { get; set; }

        /// <summary>
        /// Gets the h264 640 x 360 file location.
        /// </summary>
        public string H264640x360x30xFpsFile { get; set; }

        /// <summary>
        /// Gets the h264 320 x 180 file location.
        /// </summary>
        public string H264320x180x15FpsFile { get; set; }

        /// <summary>
        /// Backing WAV file for video
        /// </summary>
        public string WavFile { get; set; }

        public string AudioFileLocation { get; set; }

        /// <summary>
        /// Gets or sets the Azure Speech Services region (e.g. "eastus").
        /// </summary>
        public string SpeechRegion { get; set; }

        /// <summary>
        /// Gets or sets the full Azure resource ID of the Speech resource.
        /// Required for RBAC auth. Format: /subscriptions/{sub}/resourceGroups/{rg}/providers/Microsoft.CognitiveServices/accounts/{name}
        /// </summary>
        public string SpeechResourceId { get; set; }

        /// <summary>
        /// Gets or sets the custom domain endpoint of the Speech resource (e.g. "https://myresource.cognitiveservices.azure.com/").
        /// Required for RBAC auth. The Speech resource must have a custom subdomain enabled.
        /// </summary>
        public string SpeechEndpoint { get; set; }

        /// <summary>
        /// Gets or sets the TTS voice name. Defaults to "en-US-AvaMultilingualNeural" when empty.
        /// </summary>
        public string SpeechVoiceName { get; set; } = "en-US-AvaMultilingualNeural";

        /// <summary>
        /// Gets or sets the relative path to the TTS script text file.
        /// When set, the bot speaks this script on loop instead of playing video/audio files.
        /// </summary>
        public string SpeechScriptFile { get; set; }

        /// <summary>
        /// Gets the resolved absolute path to the speech script file.
        /// </summary>
        public string SpeechScriptFilePath { get; private set; }

        /// <summary>
        /// Gets a value indicating whether TTS mode is enabled.
        /// </summary>
        public bool IsTtsMode => !string.IsNullOrWhiteSpace(SpeechScriptFile);

        /// <summary>
        /// Initializes this instance.
        /// </summary>
        public void Initialize()
        {
            if (string.IsNullOrWhiteSpace(ServiceCname))
            {
                ServiceCname = ServiceDnsName;
            }

            var defaultCertificate = CertificateResolver.GetFromStore(CertificateThumbprint);
            var controlListenUris = new List<string>();

            var baseDomain = "+";
            int podNumber = 0;
            if (!string.IsNullOrEmpty(this.PodName))
            {
                int.TryParse(Regex.Match(this.PodName, @"\d+$").Value, out podNumber);
            }



            // Create structured config objects for service.
            this.CallControlBaseUrl = new Uri($"https://{this.ServiceCname}/{podNumber}/{HttpRouteConstants.CallSignalingRoutePrefix}/{HttpRouteConstants.OnNotificationRequestRoute}");

            controlListenUris.Add($"https://{baseDomain}:{CallSignalingPort}/");
            controlListenUris.Add($"https://{baseDomain}:{CallSignalingPort}/{podNumber}/");
            controlListenUris.Add($"http://{baseDomain}:{CallSignalingPort + 1}/"); // required for AKS pod graceful termination

            this.CallControlListeningUrls = controlListenUris;

            this.MediaPlatformSettings = new MediaPlatformSettings()
            {
                MediaPlatformInstanceSettings = new MediaPlatformInstanceSettings()
                {
                    CertificateThumbprint = defaultCertificate.Thumbprint,
                    InstanceInternalPort = InstanceInternalPort,
                    InstancePublicIPAddress = IPAddress.Any,
                    InstancePublicPort = InstancePublicPort + podNumber,
                    ServiceFqdn = this.ServiceCname
                },
                ApplicationId = this.AadAppId,
            };

            Console.WriteLine($"{nameof(MediaPlatformSettings)}:");
            Console.WriteLine($"{nameof(MediaPlatformSettings.MediaPlatformInstanceSettings.CertificateThumbprint)}: {MediaPlatformSettings.MediaPlatformInstanceSettings.CertificateThumbprint}.");
            Console.WriteLine($"{nameof(MediaPlatformSettings.MediaPlatformInstanceSettings.InstanceInternalPort)}: {MediaPlatformSettings.MediaPlatformInstanceSettings.InstanceInternalPort}.");
            Console.WriteLine($"{nameof(MediaPlatformSettings.MediaPlatformInstanceSettings.InstancePublicPort)}: {MediaPlatformSettings.MediaPlatformInstanceSettings.InstancePublicPort}.");
            Console.WriteLine($"{nameof(MediaPlatformSettings.MediaPlatformInstanceSettings.ServiceFqdn)}: {MediaPlatformSettings.MediaPlatformInstanceSettings.ServiceFqdn}.");
            Console.WriteLine($"{nameof(MediaPlatformSettings.ApplicationId)}: {MediaPlatformSettings.ApplicationId}.");
            Console.WriteLine();

            // Resolve speech script file path
            if (!string.IsNullOrWhiteSpace(SpeechScriptFile))
            {
                SpeechScriptFilePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, SpeechScriptFile);
                Console.WriteLine($"TTS mode enabled. Script file: {SpeechScriptFilePath}");
            }

        }

            }
        }
