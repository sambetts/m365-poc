using Bot.Services.Contract;
using Microsoft.Graph.Communications.Calls;

namespace Bot.Services.Bot;

/// <summary>
/// Default implementation of <see cref="ICallHandlerFactory"/>.
/// </summary>
public class DefaultCallHandlerFactory : ICallHandlerFactory
{
    private readonly IAzureSettings _settings;
    private readonly ITextToSpeechService _ttsService;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultCallHandlerFactory"/> class.
    /// </summary>
    /// <param name="settings">The Azure settings.</param>
    /// <param name="ttsService">The TTS service for speech playback.</param>
    public DefaultCallHandlerFactory(IAzureSettings settings, ITextToSpeechService ttsService)
    {
        _settings = settings;
        _ttsService = ttsService;
    }

    /// <inheritdoc />
    public CallHandler Create(ICall call) => new(call, _settings, _ttsService);
}
