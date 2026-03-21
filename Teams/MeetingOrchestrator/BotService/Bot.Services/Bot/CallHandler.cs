using Bot.Services.Bot;
using Bot.Services.Contract;
using Bot.Services.ServiceSetup;
using Bot.Services.Speech;
using Microsoft.Graph.Communications.Calls;
using Microsoft.Graph.Communications.Calls.Media;
using Microsoft.Graph.Communications.Common.Telemetry;
using Microsoft.Graph.Communications.Resources;
using Microsoft.Graph.Models;
using Microsoft.Skype.Bots.Media;
using System;
using System.IO;
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

    private readonly AzureSettings _settings;
    private readonly ITextToSpeechService _ttsService;

    private SpeechAudioPlayer _speechPlayer;

    public CallHandler(ICall statefulCall, IAzureSettings settings, ITextToSpeechService ttsService)
        : base(TimeSpan.FromMinutes(10), statefulCall?.GraphLogger!)
    {
        Call = statefulCall!;
        Call.OnUpdated += CallOnUpdated;
        _settings = (AzureSettings)settings;
        _ttsService = ttsService ?? throw new ArgumentNullException(nameof(ttsService));

        Console.WriteLine($"[CallHandler] Constructor called. CallId={Call.Id}, State={Call.Resource.State}");

        Call.GetLocalMediaSession().AudioSocket.DominantSpeakerChanged += OnDominantSpeakerChanged;
        Call.Participants.OnUpdated += ParticipantsOnUpdated;

        // Create the player immediately (like EchoBot) — it subscribes to
        // AudioSendStatusChanged and initialises AudioVideoFramePlayer once Active.
        _speechPlayer = new SpeechAudioPlayer(
            Call.GetLocalMediaSession().AudioSocket,
            GraphLogger);

        // Kick off TTS synthesis in the background. Once the PCM bytes are ready
        // they are enqueued into the player (which waits for AudioSendStatus=Active).
        _ = Task.Run(async () =>
        {
            try { await SynthesizeAndEnqueueAsync().ConfigureAwait(false); }
            catch (Exception ex) { Console.Error.WriteLine($"[CallHandler] SynthesizeAndEnqueueAsync FAILED: {ex}"); }
        });
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

        _speechPlayer?.ShutdownAsync().ForgetAndLogExceptionAsync(GraphLogger);
        _speechPlayer?.Dispose();
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

    private async Task SynthesizeAndEnqueueAsync()
    {
        Console.WriteLine($"[CallHandler] SynthesizeAndEnqueueAsync — reading script from: {_settings.SpeechScriptFilePath}");

        var scriptText = await File.ReadAllTextAsync(_settings.SpeechScriptFilePath).ConfigureAwait(false);
        Console.WriteLine($"[CallHandler] Script loaded ({scriptText.Length} chars). Synthesizing...");

        var pcmAudio = await _ttsService.SynthesizeToAudioAsync(scriptText).ConfigureAwait(false);
        Console.WriteLine($"[CallHandler] Synthesized {pcmAudio.Length} bytes of PCM audio. Enqueueing...");

        await _speechPlayer.EnqueueAudioAsync(pcmAudio).ConfigureAwait(false);
        Console.WriteLine($"[CallHandler] Audio enqueued to player.");
    }

    private void OnDominantSpeakerChanged(object sender, DominantSpeakerChangedEventArgs e)
    {
        Console.WriteLine($"[CallHandler] OnDominantSpeakerChanged: {e.CurrentDominantSpeaker}");
    }

    private IParticipant? GetParticipantFromMSI(uint msi)
        => Call.Participants.SingleOrDefault(x =>
            x.Resource.IsInLobby == false &&
            x.Resource.MediaStreams.Any(y => y.SourceId == msi.ToString()));
}
