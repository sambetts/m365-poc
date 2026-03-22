using Azure.Identity;
using Bot.Services.Contract;
using Bot.Services.ServiceSetup;
using Microsoft.CognitiveServices.Speech;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;

namespace Bot.Services.Speech
{
    /// <summary>
    /// Azure Speech SDK implementation of <see cref="ITextToSpeechService"/>.
    /// Produces raw 16 kHz 16-bit mono PCM suitable for the Teams media platform.
    /// Auth: RBAC via ClientSecretCredential + SpeechResourceId + SpeechEndpoint (custom domain).
    /// </summary>
    public class TextToSpeechService : ITextToSpeechService, IDisposable
    {
        private readonly AzureSettings _cfg;
        private readonly ClientSecretCredential _credential;

        public TextToSpeechService(IOptions<AzureSettings> settings)
        {
            _cfg = settings.Value;
            _credential = new ClientSecretCredential(
                _cfg.AadTenantId,
                _cfg.AadAppId,
                _cfg.AadAppSecret);
        }

        /// <inheritdoc />
        public async Task<byte[]> SynthesizeToAudioAsync(string text, string? language = null)
        {
            var speechConfig = await CreateSpeechConfigAsync().ConfigureAwait(false);

            if (!string.IsNullOrWhiteSpace(language))
                speechConfig.SpeechSynthesisLanguage = language;

            using var synthesizer = new SpeechSynthesizer(speechConfig, null);
            var result = await synthesizer.SpeakTextAsync(text).ConfigureAwait(false);

            if (result.Reason == ResultReason.SynthesizingAudioCompleted)
            {
                return result.AudioData;
            }

            var cancellation = SpeechSynthesisCancellationDetails.FromResult(result);
            throw new InvalidOperationException(
                $"Speech synthesis failed: {cancellation.Reason} – {cancellation.ErrorDetails}");
        }

        public void Dispose()
        {
            // No unmanaged resources to clean up.
        }

        private async Task<SpeechConfig> CreateSpeechConfigAsync()
        {
            if (string.IsNullOrWhiteSpace(_cfg.SpeechResourceId))
                throw new InvalidOperationException(
                    "SpeechResourceId is required for RBAC auth. " +
                    "Set it to the full Azure resource ID of your Speech resource.");
            if (string.IsNullOrWhiteSpace(_cfg.SpeechEndpoint))
                throw new InvalidOperationException(
                    "SpeechEndpoint is required for RBAC auth. " +
                    "Set it to the custom domain endpoint (e.g. https://myresource.cognitiveservices.azure.com/).");

            Console.WriteLine($"[TTS] Using RBAC auth (tenant={_cfg.AadTenantId}, app={_cfg.AadAppId}, endpoint={_cfg.SpeechEndpoint})");
            var tokenRequestContext = new Azure.Core.TokenRequestContext(
                ["https://cognitiveservices.azure.com/.default"]);
            var accessToken = await _credential.GetTokenAsync(tokenRequestContext).ConfigureAwait(false);
            Console.WriteLine("[TTS] AAD token acquired successfully.");

            // Must use FromEndpoint with the custom domain for AAD auth.
            // The shared regional endpoint does not support AAD tokens.
            var speechConfig = SpeechConfig.FromEndpoint(new Uri(_cfg.SpeechEndpoint));
            speechConfig.AuthorizationToken = $"aad#{_cfg.SpeechResourceId}#{accessToken.Token}";

            speechConfig.SetSpeechSynthesisOutputFormat(SpeechSynthesisOutputFormat.Raw16Khz16BitMonoPcm);

            if (!string.IsNullOrWhiteSpace(_cfg.SpeechVoiceName))
            {
                speechConfig.SpeechSynthesisVoiceName = _cfg.SpeechVoiceName;
            }

            return speechConfig;
        }
    }
}
