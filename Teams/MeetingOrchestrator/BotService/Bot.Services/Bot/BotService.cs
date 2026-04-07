using Bot.Model.Models;
using Bot.Services.Authentication;
using Bot.Services.Contract;
using Bot.Services.ServiceSetup;
using Bot.Services.Util;
using Microsoft.Graph.Communications.Calls;
using Microsoft.Graph.Communications.Calls.Media;
using Microsoft.Graph.Communications.Client;
using Microsoft.Graph.Communications.Common;
using Microsoft.Graph.Communications.Common.Telemetry;
using Microsoft.Graph.Communications.Resources;
using Microsoft.Graph.Models;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Bot.Services.Bot;

/// <summary>
/// Manages the communications client lifecycle, call handling, and media sessions.
/// </summary>
public class BotService : IDisposable, IBotService
{
    private readonly IGraphLogger _logger;
    private readonly AzureSettings _settings;
    private readonly IMediaSessionFactory _mediaSessionFactory;
    private readonly ICallHandlerFactory _callHandlerFactory;

    /// <summary>
    /// Maps ScenarioId → display name so that <see cref="OnCallsUpdated"/>
    /// can pass the name to the <see cref="CallHandler"/> when the call is established.
    /// </summary>
    private readonly ConcurrentDictionary<Guid, string> _pendingDisplayNames = new();

    /// <inheritdoc />
    public ConcurrentDictionary<string, CallHandler> CallHandlers { get; } = new();

    /// <inheritdoc />
    public ICommunicationsClient Client { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="BotService"/> class.
    /// </summary>
    /// <param name="logger">The graph logger.</param>
    /// <param name="settings">The Azure settings.</param>
    /// <param name="mediaSessionFactory">Factory for creating local media sessions.</param>
    /// <param name="callHandlerFactory">Factory for creating call handlers.</param>
    public BotService(
        IGraphLogger logger,
        IAzureSettings settings,
        IMediaSessionFactory mediaSessionFactory,
        ICallHandlerFactory callHandlerFactory)
    {
        _logger = logger;
        _settings = (AzureSettings)settings;
        _mediaSessionFactory = mediaSessionFactory;
        _callHandlerFactory = callHandlerFactory;
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
        Client.Calls().OnIncoming += OnIncomingCall;
        Client.Calls().OnUpdated += OnCallsUpdated;
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
        var mediaSession = _mediaSessionFactory.Create(Client);

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

        // Stash the display name BEFORE AddAsync so that OnCallsUpdated
        // can retrieve it when the call-added event fires during AddAsync.
        _pendingDisplayNames[scenarioId] = joinCallBody.DisplayName ?? string.Empty;

        var statefulCall = await Client.Calls().AddAsync(joinParams, scenarioId).ConfigureAwait(false);
        statefulCall.GraphLogger.Info($"Call creation complete: {statefulCall.Id}");

        return statefulCall;
    }

    internal static string ExtractTenantId(MeetingInfo meetingInfo)
    {
        var organizer = (meetingInfo as OrganizerMeetingInfo)?.Organizer;
        return organizer?.User?.AdditionalData != null
            && organizer.User.AdditionalData.TryGetValue("tenantId", out var tid)
            ? tid?.ToString()
            : null;
    }

    /// <summary>
    /// Handles incoming call events. Override in tests to verify behavior
    /// without a live communications client.
    /// </summary>
    protected virtual void OnIncomingCall(ICallCollection sender, CollectionEventArgs<ICall> args)
    {
        args.AddedResources.ForEach(call =>
        {
            IMediaSession mediaSession = Guid.TryParse(call.Id, out Guid callId)
                ? _mediaSessionFactory.Create(Client, callId)
                : _mediaSessionFactory.Create(Client);

            call?.AnswerAsync(mediaSession).ForgetAndLogExceptionAsync(
                call.GraphLogger,
                $"Answering call {call.Id} with scenario {call.ScenarioId}.");
        });
    }

    /// <summary>
    /// Handles call collection update events (added/removed calls).
    /// Override in tests to verify handler registration and cleanup.
    /// </summary>
    protected virtual void OnCallsUpdated(ICallCollection sender, CollectionEventArgs<ICall> args)
    {
        foreach (var call in args.AddedResources)
        {
            _pendingDisplayNames.TryRemove(call.ScenarioId, out var displayName);
            CallHandlers[call.Id] = _callHandlerFactory.Create(call, displayName ?? string.Empty);
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
