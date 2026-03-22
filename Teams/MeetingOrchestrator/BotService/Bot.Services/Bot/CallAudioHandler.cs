using Bot.Services.Contract;
using Bot.Services.ServiceSetup;
using Bot.Services.Speech;
using Microsoft.Graph.Communications.Common.Telemetry;
using Microsoft.Skype.Bots.Media;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Bot.Services.Bot;

/// <summary>
/// Encapsulates all audio playback logic for a single call: TTS synthesis
/// and streaming PCM frames through <see cref="SpeechAudioPlayer"/>.
/// </summary>
public class CallAudioHandler : IDisposable
{
    private readonly AzureSettings _settings;
    private readonly ITextToSpeechService _ttsService;
    private readonly IGraphLogger _logger;
    private readonly SpeechAudioPlayer _speechPlayer;
    private bool _disposed;

    public CallAudioHandler(
        IAudioSocket audioSocket,
        IAzureSettings settings,
        ITextToSpeechService ttsService,
        IGraphLogger logger)
    {
        _settings = (AzureSettings)settings;
        _ttsService = ttsService ?? throw new ArgumentNullException(nameof(ttsService));
        _logger = logger;

        _speechPlayer = new SpeechAudioPlayer(audioSocket, logger);
    }

    /// <summary>
    /// Kicks off background TTS synthesis and enqueues the resulting audio.
    /// Call this once after the handler is constructed.
    /// </summary>
    public void StartSpeaking()
    {
        _ = Task.Run(async () =>
        {
            try { await SynthesizeAndEnqueueAsync().ConfigureAwait(false); }
            catch (Exception ex) { Console.Error.WriteLine($"[CallAudioHandler] SynthesizeAndEnqueueAsync FAILED: {ex}"); }
        });
    }

    private async Task SynthesizeAndEnqueueAsync()
    {
        Console.WriteLine($"[CallAudioHandler] SynthesizeAndEnqueueAsync — reading script from: {_settings.SpeechScriptFilePath}");

        var scriptText = await File.ReadAllTextAsync(_settings.SpeechScriptFilePath).ConfigureAwait(false);
        Console.WriteLine($"[CallAudioHandler] Script loaded ({scriptText.Length} chars). Synthesizing...");

        var pcmAudio = await _ttsService.SynthesizeToAudioAsync(scriptText).ConfigureAwait(false);
        Console.WriteLine($"[CallAudioHandler] Synthesized {pcmAudio.Length} bytes of PCM audio. Enqueueing...");

        await _speechPlayer.EnqueueAudioAsync(pcmAudio).ConfigureAwait(false);
        Console.WriteLine($"[CallAudioHandler] Audio enqueued to player.");
    }

    /// <summary>
    /// Shuts down the audio player gracefully.
    /// </summary>
    public Task ShutdownAsync() => _speechPlayer.ShutdownAsync();

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _speechPlayer.Dispose();
    }
}
