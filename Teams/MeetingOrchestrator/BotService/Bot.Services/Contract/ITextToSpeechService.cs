using System.Threading.Tasks;

namespace Bot.Services.Contract
{
    /// <summary>
    /// Synthesizes text into raw PCM audio data using Azure Speech Services.
    /// </summary>
    public interface ITextToSpeechService
    {
        /// <summary>
        /// Synthesizes the given text into raw 16 kHz 16-bit mono PCM audio.
        /// </summary>
        /// <param name="text">The text to synthesize.</param>
        /// <param name="language">
        /// Optional BCP-47 language tag (e.g. "en-US", "fr-FR").
        /// When <c>null</c>, the service's default language/voice is used.
        /// </param>
        /// <returns>Raw PCM audio bytes.</returns>
        Task<byte[]> SynthesizeToAudioAsync(string text, string? language = null);
    }
}
