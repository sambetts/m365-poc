using Bot.Services.ServiceSetup;
using Bot.Services.Speech;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Xunit;

namespace Bot.Tests.Speech;

/// <summary>
/// Integration tests for <see cref="TextToSpeechService"/>.
/// These tests call the real Azure Speech service and require credentials.
///
/// Configure RBAC credentials via User Secrets (secret ID defined in Bot.Tests.csproj):
///   dotnet user-secrets set "AzureSettings:SpeechRegion" "eastus" --project Bot.Tests
///   dotnet user-secrets set "AzureSettings:SpeechResourceId" "/subscriptions/.../providers/Microsoft.CognitiveServices/accounts/..." --project Bot.Tests
///   dotnet user-secrets set "AzureSettings:AadTenantId" "..." --project Bot.Tests
///   dotnet user-secrets set "AzureSettings:AadAppId" "..." --project Bot.Tests
///   dotnet user-secrets set "AzureSettings:AadAppSecret" "..." --project Bot.Tests
///
/// The AAD app must have the "Cognitive Services Speech User" role on the Speech resource.
/// The Speech resource must have a custom subdomain enabled for AAD auth.
/// Tests are skipped when the required values are not set.
/// </summary>
public class TextToSpeechServiceIntegrationTests : IDisposable
{
    private const int Pcm16KHz16BitFrameBytes = 640; // 20 ms frame
    private const int MinExpectedBytes = Pcm16KHz16BitFrameBytes; // at least one frame

    private readonly TextToSpeechService? _sut;
    private readonly bool _canRun;

    public TextToSpeechServiceIntegrationTests()
    {
        var config = new ConfigurationBuilder()
            .AddUserSecrets<TextToSpeechServiceIntegrationTests>()
            .Build();

        var section = config.GetSection("AzureSettings");

        var speechRegion = section["SpeechRegion"];
        var speechResourceId = section["SpeechResourceId"] ?? string.Empty;
        var speechEndpoint = section["SpeechEndpoint"] ?? string.Empty;
        var speechVoice = section["SpeechVoiceName"];
        var tenantId = section["AadTenantId"] ?? string.Empty;
        var appId = section["AadAppId"] ?? string.Empty;
        var appSecret = section["AadAppSecret"] ?? string.Empty;

        if (string.IsNullOrWhiteSpace(speechRegion))
            speechRegion = "eastus";
        if (string.IsNullOrWhiteSpace(speechVoice))
            speechVoice = "en-US-AvaMultilingualNeural";

        _canRun = !string.IsNullOrWhiteSpace(tenantId)
               && !string.IsNullOrWhiteSpace(appId)
               && !string.IsNullOrWhiteSpace(appSecret)
               && !string.IsNullOrWhiteSpace(speechResourceId)
               && !string.IsNullOrWhiteSpace(speechEndpoint);
        if (!_canRun)
            return;

        var settings = new AzureSettings
        {
            SpeechRegion = speechRegion,
            SpeechResourceId = speechResourceId,
            SpeechEndpoint = speechEndpoint,
            SpeechVoiceName = speechVoice,
            AadTenantId = tenantId,
            AadAppId = appId,
            AadAppSecret = appSecret,
        };

        _sut = new TextToSpeechService(Options.Create(settings));
    }

    public void Dispose()
    {
        _sut?.Dispose();
    }

    private void SkipIfNoCredentials()
    {
        Skip.If(!_canRun, "No Azure Speech RBAC credentials configured. " +
            "Set AzureSettings:AadTenantId, AzureSettings:AadAppId, and AzureSettings:AadAppSecret " +
            "via 'dotnet user-secrets' for the Bot.Tests project.");
    }

    [SkippableFact]
    public async Task SynthesizeToAudioAsync_ShortPhrase_ReturnsPcmAudio()
    {
        SkipIfNoCredentials();

        var pcm = await _sut!.SynthesizeToAudioAsync("Hello");

        Assert.NotNull(pcm);
        Assert.True(pcm.Length >= MinExpectedBytes,
            $"Expected at least {MinExpectedBytes} bytes of PCM audio, got {pcm.Length}.");
    }

    [SkippableFact]
    public async Task SynthesizeToAudioAsync_ReturnsSampleAlignedPcm()
    {
        SkipIfNoCredentials();

        var pcm = await _sut!.SynthesizeToAudioAsync("Testing sample alignment.");

        // PCM 16kHz 16-bit mono = 2 bytes per sample.
        // The raw SDK output is sample-aligned but not necessarily 20 ms frame-aligned.
        // Frame alignment (640 bytes / 20 ms) is handled by SpeechAudioPlayer.
        Assert.Equal(0, pcm.Length % 2);
    }

    [SkippableFact]
    public async Task SynthesizeToAudioAsync_LongerText_ProducesMoreAudio()
    {
        SkipIfNoCredentials();

        var shortPcm = await _sut!.SynthesizeToAudioAsync("Hi.");
        var longPcm = await _sut!.SynthesizeToAudioAsync(
            "This is a much longer sentence that should produce significantly more audio data than a short greeting.");

        Assert.True(longPcm.Length > shortPcm.Length,
            $"Longer text ({longPcm.Length} bytes) should produce more audio than short text ({shortPcm.Length} bytes).");
    }

    [SkippableFact]
    public async Task SynthesizeToAudioAsync_EmptyText_ReturnsEmptyOrMinimalAudio()
    {
        SkipIfNoCredentials();

        // The Speech SDK may return a small header or empty data for empty input.
        var pcm = await _sut!.SynthesizeToAudioAsync(string.Empty);

        Assert.NotNull(pcm);
    }

    [SkippableFact]
    public async Task SynthesizeToAudioAsync_MultipleCalls_AllSucceed()
    {
        SkipIfNoCredentials();

        var results = await Task.WhenAll(
            _sut!.SynthesizeToAudioAsync("First call."),
            _sut!.SynthesizeToAudioAsync("Second call."),
            _sut!.SynthesizeToAudioAsync("Third call."));

        foreach (var pcm in results)
        {
            Assert.NotNull(pcm);
            Assert.True(pcm.Length >= MinExpectedBytes);
        }
    }
}
