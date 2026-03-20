using Bot.Services.Contract;
using Bot.Services.Util;
using Microsoft.Graph.Communications.Calls.Media;
using Microsoft.Graph.Communications.Client;
using Microsoft.Graph.Communications.Common.Telemetry;
using Microsoft.Skype.Bots.Media;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Bot.Services.Bot;

/// <summary>
/// Default implementation of <see cref="IMediaSessionFactory"/> that creates local media
/// sessions with the standard audio, video, and VBSS socket configuration.
/// </summary>
public class DefaultMediaSessionFactory : IMediaSessionFactory
{
    private readonly IGraphLogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultMediaSessionFactory"/> class.
    /// </summary>
    /// <param name="logger">The graph logger.</param>
    public DefaultMediaSessionFactory(IGraphLogger logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public ILocalMediaSession Create(ICommunicationsClient client, Guid mediaSessionId = default)
    {
        try
        {
            var videoSocketSettings = new List<VideoSocketSettings>
            {
                new()
                {
                    StreamDirections = StreamDirection.Sendrecv,
                    ReceiveColorFormat = VideoColorFormat.H264,
                    SupportedSendVideoFormats = SampleConstants.SupportedSendVideoFormats,
                    MaxConcurrentSendStreams = 1,
                },
            };

            for (int i = 0; i < SampleConstants.NumberOfMultiviewSockets; i++)
            {
                videoSocketSettings.Add(new VideoSocketSettings
                {
                    StreamDirections = StreamDirection.Recvonly,
                    ReceiveColorFormat = VideoColorFormat.H264,
                });
            }

            var vbssSocketSettings = new VideoSocketSettings
            {
                StreamDirections = StreamDirection.Recvonly,
                ReceiveColorFormat = VideoColorFormat.H264,
                MediaType = MediaType.Vbss,
                SupportedSendVideoFormats = new List<VideoFormat>
                {
                    // fps 1.875 is required for h264 in vbss scenario.
                    VideoFormat.H264_1920x1080_1_875Fps,
                },
            };

            return client.CreateMediaSession(
                new AudioSocketSettings
                {
                    StreamDirections = StreamDirection.Sendrecv,
                    SupportedAudioFormat = AudioFormat.Pcm16K,
                },
                videoSocketSettings,
                vbssSocketSettings,
                mediaSessionId: mediaSessionId);
        }
        catch (Exception e)
        {
            _logger.Log(TraceLevel.Error, e.Message);
            throw;
        }
    }
}
