using Bot.Services.Util;
using Microsoft.Graph;
using Microsoft.Graph.Communications.Calls;
using Microsoft.Graph.Communications.Calls.Media;
using Microsoft.Graph.Communications.Client;
using Microsoft.Graph.Communications.Common;
using Microsoft.Graph.Communications.Common.Telemetry;
using Microsoft.Graph.Communications.Resources;
using Microsoft.Graph.Models;
using Microsoft.Skype.Bots.Media;
using MeetingOrchestratorBot.Model.Models;
using MeetingOrchestratorBot.Services.Authentication;
using MeetingOrchestratorBot.Services.Contract;
using MeetingOrchestratorBot.Services.ServiceSetup;
using MeetingOrchestratorBot.Services.Util;
using Sample.AudioVideoPlaybackBot.FrontEnd.Bot;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace MeetingOrchestratorBot.Services.Bot;

/// <summary>
/// Manages the communications client lifecycle, call handling, and media sessions.
/// </summary>
public class BotService : IDisposable, IBotService
{
    private readonly IGraphLogger _logger;
    private readonly AzureSettings _settings;

    /// <inheritdoc />
    public ConcurrentDictionary<string, CallHandler> CallHandlers { get; } = new();

    /// <inheritdoc />
    public ICommunicationsClient Client { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="BotService"/> class.
    /// </summary>
    /// <param name="logger">The graph logger.</param>
    /// <param name="settings">The Azure settings.</param>
    public BotService(IGraphLogger logger, IAzureSettings settings)
    {
        _logger = logger;
        _settings = (AzureSettings)settings;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Client?.Dispose();
        Client = null;
    }

    /// <summary>
    /// Initialize the instance.
    /// </summary>
    public void Initialize()
    {
        var name = GetType().Assembly.GetName().Name;

        var authProvider = new AuthenticationProvider(
            name,
            _settings.AadAppId,
            _settings.AadAppSecret,
            _settings.AadTenantId,
            _logger);

        var builder = new CommunicationsClientBuilder(name, _settings.AadAppId, _logger);
        builder.SetAuthenticationProvider(authProvider);
        builder.SetNotificationUrl(_settings.CallControlBaseUrl);
        builder.SetMediaPlatformSettings(_settings.MediaPlatformSettings);
        builder.SetServiceBaseUrl(_settings.PlaceCallEndpointUrl);

        Client = builder.Build();
        Client.Calls().OnIncoming += CallsOnIncoming;
        Client.Calls().OnUpdated += CallsOnUpdated;
    }

    /// <inheritdoc />
    public async Task ChangeSharingRoleAsync(string callLegId, ScreenSharingRole role)
    {
        ArgumentException.ThrowIfNullOrEmpty(callLegId);

        await Client.Calls()[callLegId]
            .ChangeScreenSharingRoleAsync(role)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task EndCallByCallLegIdAsync(string callLegId)
    {
        try
        {
            await GetHandlerOrThrow(callLegId).Call.DeleteAsync().ConfigureAwait(false);
        }
        catch (Exception)
        {
            // Manually remove the call from SDK state.
            // This will trigger the ICallCollection.OnUpdated event with the removed resource.
            Client.Calls().TryForceRemove(callLegId, out _);
        }
    }

    /// <inheritdoc />
    public async Task<ICall> JoinCallAsync(JoinCallBody joinCallBody)
    {
        var scenarioId = Guid.NewGuid();
        var (chatInfo, meetingInfo) = JoinInfo.ParseJoinURL(joinCallBody.JoinURL);
        var tenantId = ExtractTenantId(meetingInfo);
        var mediaSession = CreateLocalMediaSession();

        var joinParams = new JoinMeetingParameters(chatInfo, meetingInfo, mediaSession)
        {
            TenantId = tenantId,
        };

        if (!string.IsNullOrWhiteSpace(joinCallBody.DisplayName))
        {
            // Teams client does not allow changing of ones own display name.
            // If display name is specified, we join as anonymous (guest) user
            // with the specified display name. This will put bot into lobby
            // unless lobby bypass is disabled.
            joinParams.GuestIdentity = new Identity
            {
                Id = Guid.NewGuid().ToString(),
                DisplayName = joinCallBody.DisplayName,
            };
        }

        var statefulCall = await Client.Calls().AddAsync(joinParams, scenarioId).ConfigureAwait(false);
        statefulCall.GraphLogger.Info($"Call creation complete: {statefulCall.Id}");
        return statefulCall;
    }

    private static string ExtractTenantId(MeetingInfo meetingInfo)
    {
        var organizer = (meetingInfo as OrganizerMeetingInfo)?.Organizer;
        return organizer?.User?.AdditionalData != null
            && organizer.User.AdditionalData.TryGetValue("tenantId", out var tid)
            ? tid?.ToString()
            : null;
    }

    private ILocalMediaSession CreateLocalMediaSession(Guid mediaSessionId = default)
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

            return Client.CreateMediaSession(
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

    private void CallsOnIncoming(ICallCollection sender, CollectionEventArgs<ICall> args)
    {
        args.AddedResources.ForEach(call =>
        {
            IMediaSession mediaSession = Guid.TryParse(call.Id, out Guid callId)
                ? CreateLocalMediaSession(callId)
                : CreateLocalMediaSession();

            call?.AnswerAsync(mediaSession).ForgetAndLogExceptionAsync(
                call.GraphLogger,
                $"Answering call {call.Id} with scenario {call.ScenarioId}.");
        });
    }

    private void CallsOnUpdated(ICallCollection sender, CollectionEventArgs<ICall> args)
    {
        foreach (var call in args.AddedResources)
        {
            CallHandlers[call.Id] = new CallHandler(call, _settings);
        }

        foreach (var call in args.RemovedResources)
        {
            if (CallHandlers.TryRemove(call.Id, out CallHandler handler))
            {
                handler.Dispose();
            }
        }
    }

    private CallHandler GetHandlerOrThrow(string callLegId)
    {
        if (!CallHandlers.TryGetValue(callLegId, out CallHandler handler))
        {
            throw new ArgumentException($"call ({callLegId}) not found");
        }

        return handler;
    }
}
