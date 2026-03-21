using Microsoft.Graph.Communications.Calls.Media;
using Microsoft.Graph.Communications.Common.Telemetry;
using Microsoft.Skype.Bots.Media;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Bot.Services.Speech
{
    /// <summary>
    /// Sends pre-synthesized PCM audio to the Teams audio socket using the SDK's
    /// <see cref="AudioVideoFramePlayer"/> (same pattern as EchoBot).
    /// Audio format: 16 kHz, 16-bit, mono (640 bytes per 20 ms frame).
    /// </summary>
    public class SpeechAudioPlayer : IDisposable
    {
        private const int FrameSizeBytes = 640;              // 16 kHz × 16-bit × 20 ms
        private const int FrameDurationTicks = 20 * 10_000;  // 20 ms in 100-ns ticks

        private readonly IAudioSocket _audioSocket;
        private readonly IGraphLogger _logger;
        private readonly TaskCompletionSource<bool> _audioSendStatusActive = new();
        private readonly TaskCompletionSource<bool> _playerCreated = new();

        private AudioVideoFramePlayer _framePlayer;
        private byte[] _pcmData;
        private int _shutdown;
        private bool _disposed;

        public SpeechAudioPlayer(IAudioSocket audioSocket, IGraphLogger logger)
        {
            _audioSocket = audioSocket ?? throw new ArgumentNullException(nameof(audioSocket));
            _logger = logger;

            _audioSocket.AudioSendStatusChanged += OnAudioSendStatusChanged;

            Console.WriteLine("[SpeechAudioPlayer] Created. Waiting for AudioSendStatus=Active to init player...");

            // Start player creation (waits for audio send to become Active, like EchoBot).
            _ = Task.Run(InitPlayerAsync);
        }

        /// <summary>
        /// Enqueue PCM audio for looped playback. Can be called after TTS synthesis completes.
        /// Waits for the player to be ready before enqueueing.
        /// </summary>
        public async Task EnqueueAudioAsync(byte[] pcmData)
        {
            _pcmData = pcmData ?? throw new ArgumentNullException(nameof(pcmData));

            Console.WriteLine($"[SpeechAudioPlayer] EnqueueAudioAsync called with {pcmData.Length} bytes. Waiting for player...");

            await _playerCreated.Task.ConfigureAwait(false);

            Console.WriteLine("[SpeechAudioPlayer] Player ready, creating and enqueueing audio buffers...");

            var buffers = CreateAudioMediaBuffers(pcmData);
            await _framePlayer.EnqueueBuffersAsync(buffers, new List<VideoMediaBuffer>()).ConfigureAwait(false);

            Console.WriteLine($"[SpeechAudioPlayer] Enqueued {buffers.Count} audio buffers.");
        }

        public async Task ShutdownAsync()
        {
            if (Interlocked.CompareExchange(ref _shutdown, 1, 0) == 1)
                return;

            _audioSocket.AudioSendStatusChanged -= OnAudioSendStatusChanged;

            await _playerCreated.Task.ConfigureAwait(false);

            if (_framePlayer != null)
                await _framePlayer.ShutdownAsync().ConfigureAwait(false);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _audioSocket.AudioSendStatusChanged -= OnAudioSendStatusChanged;
        }

        private void OnAudioSendStatusChanged(object sender, AudioSendStatusChangedEventArgs e)
        {
            Console.WriteLine($"[SpeechAudioPlayer] AudioSendStatus changed to {e.MediaSendStatus}");

            if (e.MediaSendStatus == MediaSendStatus.Active)
            {
                _audioSendStatusActive.TrySetResult(true);
            }
        }

        private async Task InitPlayerAsync()
        {
            try
            {
                Console.WriteLine("[SpeechAudioPlayer] InitPlayerAsync: awaiting AudioSendStatusActive...");
                await _audioSendStatusActive.Task.ConfigureAwait(false);

                Console.WriteLine("[SpeechAudioPlayer] Audio send active. Creating AudioVideoFramePlayer...");

                var settings = new AudioVideoFramePlayerSettings(
                    new AudioSettings(20), new VideoSettings(), 1000);

                _framePlayer = new AudioVideoFramePlayer(
                    (AudioSocket)_audioSocket,
                    null,
                    settings);

                _framePlayer.LowOnFrames += OnLowOnFrames;

                Console.WriteLine("[SpeechAudioPlayer] AudioVideoFramePlayer created.");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[SpeechAudioPlayer] InitPlayerAsync FAILED: {ex}");
            }
            finally
            {
                _playerCreated.TrySetResult(true);
            }
        }

        private void OnLowOnFrames(object sender, LowOnFramesEventArgs e)
        {
            Console.WriteLine($"[SpeechAudioPlayer] LowOnFrames (remaining={e.RemainingMediaLengthInMS}ms). Re-enqueueing...");

            if (_pcmData == null || _shutdown == 1) return;

            try
            {
                var buffers = CreateAudioMediaBuffers(_pcmData);
                _ = _framePlayer.EnqueueBuffersAsync(buffers, new List<VideoMediaBuffer>());
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[SpeechAudioPlayer] Re-enqueue failed: {ex}");
            }
        }

        private static List<AudioMediaBuffer> CreateAudioMediaBuffers(byte[] pcmData)
        {
            var buffers = new List<AudioMediaBuffer>();
            var referenceTime = DateTime.Now.Ticks;

            int offset = 0;
            while (offset + FrameSizeBytes <= pcmData.Length)
            {
                IntPtr unmanagedBuffer = Marshal.AllocHGlobal(FrameSizeBytes);
                Marshal.Copy(pcmData, offset, unmanagedBuffer, FrameSizeBytes);

                var audioBuffer = new AudioSendBuffer(
                    unmanagedBuffer, FrameSizeBytes, AudioFormat.Pcm16K, referenceTime);

                buffers.Add(audioBuffer);
                referenceTime += FrameDurationTicks;
                offset += FrameSizeBytes;
            }

            return buffers;
        }
    }
}
