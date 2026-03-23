using Bot.Model.Models;
using Bot.Services.Contract;
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
/// Supports structured scripts with per-paragraph language, pause timing,
/// and an external pause/resume gate.
/// </summary>
public class CallAudioHandler : IDisposable
{
    /// <summary>16 kHz × 16-bit × mono = 32 000 bytes per second.</summary>
    private const int BytesPerSecond = 16_000 * 2;

    /// <summary>20 ms frame = 640 bytes.</summary>
    private const int FrameSizeBytes = 640;

    private readonly ITextToSpeechService _ttsService;
    private readonly IGraphLogger _logger;
    private readonly SpeechAudioPlayer _speechPlayer;

    private readonly object _pauseLock = new();
    private TaskCompletionSource? _pauseTcs;
    private bool _disposed;

    public CallAudioHandler(
        IAudioSocket audioSocket,
        ITextToSpeechService ttsService,
        IGraphLogger logger)
    {
        _ttsService = ttsService ?? throw new ArgumentNullException(nameof(ttsService));
        _logger = logger;

        _speechPlayer = new SpeechAudioPlayer(audioSocket, logger);
    }

    /// <summary>
    /// Gets whether playback is currently paused.
    /// </summary>
    public bool IsPaused { get; private set; }

    /// <summary>
    /// Pauses script processing. The current paragraph will finish, but the
    /// handler will wait before starting the next one.
    /// </summary>
    public void Pause()
    {
        lock (_pauseLock)
        {
            IsPaused = true;
            _pauseTcs ??= new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        Console.WriteLine("[CallAudioHandler] Paused.");
    }

    /// <summary>
    /// Resumes script processing after a <see cref="Pause"/>.
    /// </summary>
    public void Resume()
    {
        lock (_pauseLock)
        {
            IsPaused = false;
            _pauseTcs?.TrySetResult();
            _pauseTcs = null;
        }

        Console.WriteLine("[CallAudioHandler] Resumed.");
    }

    /// <summary>
    /// Kicks off background TTS synthesis and enqueues the resulting audio
    /// for the given script content (JSON or plain text).
    /// </summary>
    public void StartSpeaking(string scriptContent)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scriptContent);

        _ = Task.Run(async () =>
        {
            try { await SynthesizeAndEnqueueAsync(scriptContent).ConfigureAwait(false); }
            catch (Exception ex) { Console.Error.WriteLine($"[CallAudioHandler] SynthesizeAndEnqueueAsync FAILED: {ex}"); }
        });
    }

    private async Task SynthesizeAndEnqueueAsync(string scriptContent)
    {
        var script = SpeechScript.Parse(scriptContent);

        Console.WriteLine($"[CallAudioHandler] Script loaded — {script.Paragraphs.Count} paragraph(s), defaultLanguage={script.DefaultLanguage}");

        // Build one contiguous PCM buffer (speech + silence gaps) so the
        // AudioVideoFramePlayer receives a single enqueue and loops it correctly.
        using var combined = new MemoryStream();

        for (var i = 0; i < script.Paragraphs.Count; i++)
        {
            var paragraph = script.Paragraphs[i];
            var language = paragraph.Language ?? script.DefaultLanguage;

            Console.WriteLine($"[CallAudioHandler] Paragraph {i + 1}/{script.Paragraphs.Count} (lang={language})");

            // Wait if externally paused.
            await WaitWhilePausedAsync().ConfigureAwait(false);

            // Pre-paragraph silence.
            if (paragraph.PauseBeforeSeconds > 0)
            {
                Console.WriteLine($"[CallAudioHandler]   pause-before {paragraph.PauseBeforeSeconds}s");
                WriteSilence(combined, paragraph.PauseBeforeSeconds);
            }

            // Synthesize this paragraph.
            var pcmAudio = await _ttsService.SynthesizeToAudioAsync(paragraph.Text, language).ConfigureAwait(false);
            Console.WriteLine($"[CallAudioHandler]   Synthesized {pcmAudio.Length} bytes.");
            combined.Write(pcmAudio);

            // Post-paragraph silence.
            if (paragraph.PauseAfterSeconds > 0)
            {
                Console.WriteLine($"[CallAudioHandler]   pause-after {paragraph.PauseAfterSeconds}s");
                WriteSilence(combined, paragraph.PauseAfterSeconds);
            }
        }

        var fullAudio = combined.ToArray();
        Console.WriteLine($"[CallAudioHandler] Combined audio: {fullAudio.Length} bytes. Enqueueing...");
        await _speechPlayer.EnqueueAudioAsync(fullAudio).ConfigureAwait(false);
        Console.WriteLine("[CallAudioHandler] All paragraphs enqueued.");
    }

    /// <summary>
    /// Writes frame-aligned silence (zero bytes) into <paramref name="stream"/>.
    /// </summary>
    private static void WriteSilence(MemoryStream stream, double seconds)
    {
        var byteCount = (int)(seconds * BytesPerSecond);
        // Align down to 20 ms frame boundary.
        byteCount = byteCount / FrameSizeBytes * FrameSizeBytes;
        if (byteCount > 0)
            stream.Write(new byte[byteCount]);
    }

    /// <summary>
    /// Blocks asynchronously while <see cref="IsPaused"/> is <c>true</c>.
    /// Returns immediately when not paused.
    /// </summary>
    private async Task WaitWhilePausedAsync()
    {
        while (true)
        {
            Task? waitTask;
            lock (_pauseLock)
            {
                if (!IsPaused)
                    return;
                waitTask = _pauseTcs?.Task;
            }

            if (waitTask is not null)
                await waitTask.ConfigureAwait(false);
            else
                return;
        }
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
