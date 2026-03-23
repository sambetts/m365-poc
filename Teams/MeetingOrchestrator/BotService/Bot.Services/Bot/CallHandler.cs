using Bot.Services.Bot;
using Bot.Services.Contract;
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

    public CallHandler(ICall statefulCall, ITextToSpeechService ttsService, string displayName)
        : base(TimeSpan.FromMinutes(10), statefulCall?.GraphLogger!)
    {
        Call = statefulCall!;
        DisplayName = displayName ?? string.Empty;
        Call.OnUpdated += CallOnUpdated;

        Console.WriteLine($"[CallHandler] Constructor called. CallId={Call.Id}, DisplayName={DisplayName}, State={Call.Resource.State}");

        Call.GetLocalMediaSession().AudioSocket.DominantSpeakerChanged += OnDominantSpeakerChanged;
        Call.Participants.OnUpdated += ParticipantsOnUpdated;

        _audioHandler = new CallAudioHandler(
            Call.GetLocalMediaSession().AudioSocket,
            ttsService,
            GraphLogger);
    }

    public ICall Call { get; }

    /// <summary>The display name the bot used when joining this call.</summary>
    public string DisplayName { get; }

    /// <summary>
    /// Starts speaking the given script (JSON or plain text).
    /// </summary>
    public void StartScript(string scriptContent) => _audioHandler.StartSpeaking(scriptContent);

    /// <summary>Pauses the speech script between paragraphs.</summary>
    public void PauseSpeaking() => _audioHandler.Pause();

    /// <summary>Resumes the speech script after a pause.</summary>
    public void ResumeSpeaking() => _audioHandler.Resume();

    /// <summary>Gets whether speech playback is currently paused.</summary>
    public bool IsSpeakingPaused => _audioHandler.IsPaused;

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
