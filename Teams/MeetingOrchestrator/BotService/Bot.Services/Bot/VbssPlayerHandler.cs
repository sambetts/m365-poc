namespace Bot.Services.Bot
{
    using global::Bot.Services.ServiceSetup;
    using global::Bot.Services.Util;
    using Microsoft.Graph.Communications.Common.Telemetry;
    using Microsoft.Skype.Bots.Media;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// Manages the VBSS (Video-Based Screen Sharing) frame player lifecycle,
    /// including buffer creation, enqueuing, and shutdown.
    /// </summary>
    internal class VbssPlayerHandler
    {
        private readonly IVideoSocket _vbssSocket;
        private readonly IGraphLogger _logger;
        private readonly AzureSettings _settings;
        private AudioVideoFramePlayer _vbssFramePlayer;
        private List<VideoMediaBuffer> _vbssMediaBuffers = new List<VideoMediaBuffer>();
        private List<VideoFormat> _vbssKnownSupportedFormats;

        /// <summary>
        /// Initializes a new instance of the <see cref="VbssPlayerHandler"/> class
        /// and subscribes to the socket's send-status-changed event.
        /// </summary>
        /// <param name="vbssSocket">The VBSS video socket.</param>
        /// <param name="logger">The graph logger.</param>
        /// <param name="settings">The Azure settings.</param>
        public VbssPlayerHandler(IVideoSocket vbssSocket, IGraphLogger logger, AzureSettings settings)
        {
            _vbssSocket = vbssSocket ?? throw new ArgumentNullException(nameof(vbssSocket));
            _logger = logger;
            _settings = settings;
            _vbssSocket.VideoSendStatusChanged += OnSendStatusChanged;
        }

        /// <summary>
        /// Gets a value indicating whether the handler has been shut down.
        /// </summary>
        public bool IsShutdown { get; private set; }

        /// <summary>
        /// Shuts down the VBSS player, unsubscribes events, and disposes buffers.
        /// </summary>
        public async Task ShutdownAsync()
        {
            IsShutdown = true;

            if (_vbssFramePlayer != null)
            {
                _vbssFramePlayer.LowOnFrames -= OnLowOnFrames;
                await _vbssFramePlayer.ShutdownAsync().ConfigureAwait(false);
            }

            _vbssSocket.VideoSendStatusChanged -= OnSendStatusChanged;
            DisposeBuffers();
        }

        private void OnSendStatusChanged(object sender, VideoSendStatusChangedEventArgs e)
        {
            _logger.Info($"[VbssSendStatusChangedEventArgs(MediaSendStatus=<{e.MediaSendStatus}>]");

            if (e.MediaSendStatus == MediaSendStatus.Active)
            {
                _logger.Info($"[VbssSendStatusChangedEventArgs(MediaSendStatus=<{e.MediaSendStatus}>;PreferredVideoSourceFormat=<{string.Join(";", e.PreferredEncodedVideoSourceFormats.ToList())}>]");

                var previousSupportedFormats = (_vbssKnownSupportedFormats != null && _vbssKnownSupportedFormats.Any()) ? _vbssKnownSupportedFormats :
                   new List<VideoFormat>();
                _vbssKnownSupportedFormats = e.PreferredEncodedVideoSourceFormats.ToList();

                if (_vbssFramePlayer == null)
                {
                    CreateFramePlayer();
                }
                else
                {
                    _vbssFramePlayer?.ClearAsync().ForgetAndLogExceptionAsync(_logger);
                }

                _logger.Info($"[VbssSendStatusChangedEventArgs(MediaSendStatus=<{e.MediaSendStatus}> enqueuing new formats: {string.Join(";", _vbssKnownSupportedFormats)}]");

                _vbssMediaBuffers = Utilities.GetUtils(_settings).CreateVideoMediaBuffers(DateTime.Now.Ticks, _vbssKnownSupportedFormats, true, _logger);
                _vbssFramePlayer?.EnqueueBuffersAsync(new List<AudioMediaBuffer>(), _vbssMediaBuffers).ForgetAndLogExceptionAsync(_logger);
            }
            else if (e.MediaSendStatus == MediaSendStatus.Inactive)
            {
                _vbssFramePlayer?.ClearAsync().ForgetAndLogExceptionAsync(_logger);
            }
        }

        private void OnLowOnFrames(object sender, LowOnFramesEventArgs e)
        {
            if (!IsShutdown)
            {
                _logger.Info($"Low on frames event raised for the vbss player, remaining lenght is {e.RemainingMediaLengthInMS} ms");

                _vbssMediaBuffers = Utilities.GetUtils(_settings).CreateVideoMediaBuffers(DateTime.Now.Ticks, _vbssKnownSupportedFormats, true, _logger);
                _vbssFramePlayer?.EnqueueBuffersAsync(new List<AudioMediaBuffer>(), _vbssMediaBuffers).ForgetAndLogExceptionAsync(_logger);
                _logger.Info("enqueued more frames in the vbssFramePlayer");
            }
        }

        private void CreateFramePlayer()
        {
            try
            {
                _logger.Info("Creating the vbss FramePlayer");
                var framePlayerSettings =
                    new AudioVideoFramePlayerSettings(new AudioSettings(20), new VideoSettings(), 1000);
                _vbssFramePlayer = new AudioVideoFramePlayer(
                    null,
                    (VideoSocket)_vbssSocket,
                    framePlayerSettings);

                _vbssFramePlayer.LowOnFrames += OnLowOnFrames;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Failed to create the vbssFramePlayer with exception {ex}");
            }
        }

        private void DisposeBuffers()
        {
            foreach (var buffer in _vbssMediaBuffers)
            {
                buffer.Dispose();
            }

            _logger.Info($"disposed {_vbssMediaBuffers.Count} vbssMediaBuffers");
            _vbssMediaBuffers.Clear();
        }
    }
}
