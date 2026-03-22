using Bot.Services.Bot;
using Bot.Services.Contract;
using Bot.Services.ServiceSetup;
using Microsoft.Graph.Communications.Calls;
using Microsoft.Graph.Communications.Calls.Media;
using Microsoft.Graph.Communications.Common.Telemetry;
using Microsoft.Graph.Communications.Resources;
using Microsoft.Graph.Models;
using Microsoft.Skype.Bots.Media;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;

namespace Bot.Services.Bot;

/// <summary>
/// Manages the lifecycle of a single call: dominant speaker tracking and TTS audio playback.
/// </summary>
public class CallHandler : HeartbeatHandler
{
    public const uint DominantSpeakerNone = DominantSpeakerChangedEventArgs.None;

    private readonly CallAudioHandler _audioHandler;

    public CallHandler(ICall statefulCall, IAzureSettings settings, ITextToSpeechService ttsService)
        : base(TimeSpan.FromMinutes(10), statefulCall?.GraphLogger!)
    {
        Call = statefulCall!;
        Call.OnUpdated += CallOnUpdated;

        Console.WriteLine($"[CallHandler] Constructor called. CallId={Call.Id}, State={Call.Resource.State}");

        Call.GetLocalMediaSession().AudioSocket.DominantSpeakerChanged += OnDominantSpeakerChanged;
        Call.Participants.OnUpdated += ParticipantsOnUpdated;

        _audioHandler = new CallAudioHandler(
            Call.GetLocalMediaSession().AudioSocket,
            settings,
            ttsService,
            GraphLogger);

        _audioHandler.StartSpeaking();
    }

    public ICall Call { get; }

    protected override Task HeartbeatAsync(ElapsedEventArgs args) => Call.KeepAliveAsync();

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        Call.GetLocalMediaSession().AudioSocket.DominantSpeakerChanged -= OnDominantSpeakerChanged;
        Call.OnUpdated -= CallOnUpdated;
        Call.Participants.OnUpdated -= ParticipantsOnUpdated;

        foreach (var participant in Call.Participants)
            participant.OnUpdated -= OnParticipantUpdated;

        _audioHandler?.ShutdownAsync().ForgetAndLogExceptionAsync(GraphLogger);
        _audioHandler?.Dispose();
    }

    private void CallOnUpdated(ICall sender, ResourceEventArgs<Call> e)
    {
        Console.WriteLine($"[CallHandler] CallOnUpdated: {e.OldResource.State} -> {e.NewResource.State}");
    }

    private void ParticipantsOnUpdated(IParticipantCollection sender, CollectionEventArgs<IParticipant> args)
    {
        foreach (var participant in args.AddedResources)
        {
            if (participant.Resource.Info.Identity.User is not null)
                participant.OnUpdated += OnParticipantUpdated;
        }

        foreach (var participant in args.RemovedResources)
        {
            if (participant.Resource.Info.Identity.User is not null)
                participant.OnUpdated -= OnParticipantUpdated;
        }
    }

    private void OnParticipantUpdated(IParticipant sender, ResourceEventArgs<Participant> args)
    {
    }

    private void OnDominantSpeakerChanged(object sender, DominantSpeakerChangedEventArgs e)
    {
        Console.WriteLine($"[CallHandler] OnDominantSpeakerChanged: {e.CurrentDominantSpeaker}");
    }
}
